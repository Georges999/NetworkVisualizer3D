using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using NetworkVisualizer3D.Core.Configuration;
using NetworkVisualizer3D.Core.Interfaces;
using NetworkVisualizer3D.Core.Models;
using NetworkVisualizer3D.Core.Utils;

namespace NetworkVisualizer3D.Core.Services
{
    /// <summary>
    /// Network packet capture service implementation
    /// </summary>
    public class NetworkCaptureService : INetworkCaptureService
    {
        private readonly NetworkCaptureSettings _settings;
        private readonly ILogger _logger;
        private readonly DeviceTypeDetector _deviceDetector;
        private readonly PositionCalculator _positionCalculator;
        private readonly HttpAnalyzer _httpAnalyzer;
        
        private ICaptureDevice? _captureDevice;
        private readonly ConcurrentDictionary<string, NetworkDevice> _devices = new();
        private readonly ConcurrentDictionary<string, NetworkConnection> _connections = new();
        private readonly ConcurrentQueue<NetworkPacket> _recentPackets = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        private DateTime _captureStartTime;
        private long _totalPacketsCaptured;
        private long _totalBytesTransferred;
        private readonly Dictionary<string, int> _protocolDistribution = new();
        
        public event EventHandler<NetworkSnapshot>? SnapshotCaptured;
        public event EventHandler<NetworkPacket>? PacketCaptured;
        public event EventHandler<NetworkDevice>? DeviceDiscovered;
        public event EventHandler<NetworkConnection>? ConnectionEstablished;
        
        public bool IsCapturing { get; private set; }

        public NetworkCaptureService(NetworkCaptureSettings settings, ILogger logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceDetector = new DeviceTypeDetector();
            _positionCalculator = new PositionCalculator();
            _httpAnalyzer = new HttpAnalyzer();
        }

        public async Task<List<string>> GetAvailableInterfacesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var devices = CaptureDeviceList.Instance;
                    var interfaces = new List<string>();
                    
                    foreach (var device in devices)
                    {
                        if (device is LibPcapLiveDevice liveDevice)
                        {
                            var name = !string.IsNullOrEmpty(liveDevice.Interface.FriendlyName) 
                                ? liveDevice.Interface.FriendlyName 
                                : liveDevice.Interface.Name;
                            interfaces.Add(name);
                        }
                    }
                    
                    _logger.LogInformation($"Found {interfaces.Count} network interfaces");
                    return interfaces;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error getting network interfaces: {ex.Message}");
                    return new List<string>();
                }
            });
        }

        public async Task<bool> StartCaptureAsync(string interfaceName, CancellationToken cancellationToken = default)
        {
            if (IsCapturing)
            {
                _logger.LogWarning("Capture is already running");
                return false;
            }

            try
            {
                var devices = CaptureDeviceList.Instance;
                _captureDevice = devices.FirstOrDefault(d => 
                    (d as LibPcapLiveDevice)?.Interface.FriendlyName == interfaceName ||
                    (d as LibPcapLiveDevice)?.Interface.Name == interfaceName);

                if (_captureDevice == null)
                {
                    _logger.LogError($"Network interface '{interfaceName}' not found");
                    return false;
                }

                // Configure capture device
                _captureDevice.OnPacketArrival += OnPacketArrival;
                _captureDevice.Open(DeviceModes.Promiscuous, _settings.CaptureTimeoutMs);

                // Set capture filter if specified
                if (_settings.FilteredProtocols.Length > 0)
                {
                    var filter = string.Join(" or ", _settings.FilteredProtocols.Select(p => p.ToLower()));
                    _captureDevice.Filter = filter;
                }

                // Start capture
                _captureStartTime = DateTime.UtcNow;
                _totalPacketsCaptured = 0;
                _totalBytesTransferred = 0;
                _protocolDistribution.Clear();
                
                IsCapturing = true;
                _captureDevice.StartCapture();
                
                _logger.LogInformation($"Started packet capture on {interfaceName}");

                // Start snapshot generation task
                _ = Task.Run(async () => await GenerateSnapshotsAsync(_cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting capture: {ex.Message}");
                IsCapturing = false;
                return false;
            }
        }

        public async Task StopCaptureAsync()
        {
            if (!IsCapturing)
            {
                _logger.LogWarning("Capture is not running");
                return;
            }

            try
            {
                IsCapturing = false;
                _cancellationTokenSource.Cancel();
                
                if (_captureDevice != null)
                {
                    _captureDevice.StopCapture();
                    _captureDevice.Close();
                    _captureDevice.OnPacketArrival -= OnPacketArrival;
                    _captureDevice = null;
                }

                _logger.LogInformation("Packet capture stopped");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error stopping capture: {ex.Message}");
            }
        }

        private void OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                var rawPacket = e.GetPacket();
                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                
                Interlocked.Increment(ref _totalPacketsCaptured);
                Interlocked.Add(ref _totalBytesTransferred, rawPacket.Data.Length);

                var networkPacket = ProcessPacket(packet, rawPacket.Timeval.Date);
                if (networkPacket != null)
                {
                    // Add to recent packets queue
                    _recentPackets.Enqueue(networkPacket);
                    while (_recentPackets.Count > _settings.MaxPacketsPerSnapshot)
                    {
                        _recentPackets.TryDequeue(out _);
                    }

                    // Update protocol distribution
                    lock (_protocolDistribution)
                    {
                        if (_protocolDistribution.ContainsKey(networkPacket.Protocol.ToString()))
                            _protocolDistribution[networkPacket.Protocol.ToString()]++;
                        else
                            _protocolDistribution[networkPacket.Protocol.ToString()] = 1;
                    }

                    // Process devices and connections
                    ProcessDevicesAndConnections(networkPacket);

                    // Fire packet captured event
                    PacketCaptured?.Invoke(this, networkPacket);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing packet: {ex.Message}");
            }
        }

        private NetworkPacket? ProcessPacket(Packet packet, DateTime timestamp)
        {
            try
            {
                var ethernetPacket = packet.Extract<EthernetPacket>();
                if (ethernetPacket == null) return null;

                var ipPacket = ethernetPacket.Extract<IPPacket>();
                if (ipPacket == null) return null;

                var networkPacket = new NetworkPacket
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = timestamp,
                    SourceMac = ethernetPacket.SourceHardwareAddress.ToString(),
                    DestinationMac = ethernetPacket.DestinationHardwareAddress.ToString(),
                    SourceIp = ipPacket.SourceAddress.ToString(),
                    DestinationIp = ipPacket.DestinationAddress.ToString(),
                    Size = packet.Bytes.Length,
                    Protocol = DetermineProtocol(packet)
                };

                // Extract port information for TCP/UDP
                var tcpPacket = packet.Extract<TcpPacket>();
                var udpPacket = packet.Extract<UdpPacket>();
                
                if (tcpPacket != null)
                {
                    networkPacket.SourcePort = tcpPacket.SourcePort;
                    networkPacket.DestinationPort = tcpPacket.DestinationPort;
                    networkPacket.TcpFlags = tcpPacket.Flags.ToString();
                }
                else if (udpPacket != null)
                {
                    networkPacket.SourcePort = udpPacket.SourcePort;
                    networkPacket.DestinationPort = udpPacket.DestinationPort;
                }

                // Analyze HTTP traffic if enabled
                if (_settings.EnableDeepPacketInspection && tcpPacket != null)
                {
                    var httpAnalysis = _httpAnalyzer.AnalyzePacket(tcpPacket);
                    if (httpAnalysis != null)
                    {
                        networkPacket.HttpMethod = httpAnalysis.Method;
                        networkPacket.HttpUrl = httpAnalysis.Url;
                        networkPacket.HttpUserAgent = httpAnalysis.UserAgent;
                        networkPacket.SecurityAlerts.AddRange(httpAnalysis.SecurityAlerts);
                    }
                }

                return networkPacket;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing packet: {ex.Message}");
                return null;
            }
        }

        private Models.ProtocolType DetermineProtocol(Packet packet)
        {
            if (packet.Extract<TcpPacket>() != null) return Models.ProtocolType.TCP;
            if (packet.Extract<UdpPacket>() != null) return Models.ProtocolType.UDP;
            if (packet.Extract<IcmpV4Packet>() != null) return Models.ProtocolType.ICMP;
            if (packet.Extract<IcmpV6Packet>() != null) return Models.ProtocolType.ICMPv6;
            if (packet.Extract<ArpPacket>() != null) return Models.ProtocolType.ARP;
            return Models.ProtocolType.Other;
        }

        private void ProcessDevicesAndConnections(NetworkPacket packet)
        {
            // Process source device
            ProcessDevice(packet.SourceIp, packet.SourceMac, packet.Size, true);
            
            // Process destination device
            ProcessDevice(packet.DestinationIp, packet.DestinationMac, packet.Size, false);
            
            // Process connection
            ProcessConnection(packet);
        }

        private void ProcessDevice(string ipAddress, string macAddress, int trafficBytes, bool isSource)
        {
            var deviceKey = $"{ipAddress}_{macAddress}";
            
            if (_devices.TryGetValue(deviceKey, out var existingDevice))
            {
                // Update existing device
                existingDevice.LastSeen = DateTime.UtcNow;
                existingDevice.TotalTraffic += trafficBytes;
                if (isSource) existingDevice.PacketsSent++;
                else existingDevice.PacketsReceived++;
            }
            else
            {
                // Create new device
                var device = new NetworkDevice
                {
                    Id = Guid.NewGuid().ToString(),
                    IpAddress = ipAddress,
                    MacAddress = macAddress,
                    Name = GetDeviceName(ipAddress),
                    DeviceType = _deviceDetector.DetectDeviceType(macAddress, ipAddress),
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    TotalTraffic = trafficBytes,
                    PacketsSent = isSource ? 1 : 0,
                    PacketsReceived = isSource ? 0 : 1,
                    Position = _positionCalculator.CalculatePosition(ipAddress, _devices.Values.ToList())
                };

                if (_devices.TryAdd(deviceKey, device))
                {
                    DeviceDiscovered?.Invoke(this, device);
                }
            }
        }

        private void ProcessConnection(NetworkPacket packet)
        {
            var connectionKey = $"{packet.SourceIp}:{packet.SourcePort}_{packet.DestinationIp}:{packet.DestinationPort}";
            var reverseKey = $"{packet.DestinationIp}:{packet.DestinationPort}_{packet.SourceIp}:{packet.SourcePort}";
            
            // Check if connection exists (either direction)
            if (!_connections.TryGetValue(connectionKey, out var connection) && 
                !_connections.TryGetValue(reverseKey, out connection))
            {
                // Create new connection
                connection = new NetworkConnection
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceIp = packet.SourceIp,
                    DestinationIp = packet.DestinationIp,
                    SourcePort = packet.SourcePort,
                    DestinationPort = packet.DestinationPort,
                    Protocol = packet.Protocol,
                    State = ConnectionState.Established,
                    StartTime = packet.Timestamp,
                    LastActivity = packet.Timestamp,
                    TotalBytes = packet.Size,
                    PacketCount = 1
                };

                if (_connections.TryAdd(connectionKey, connection))
                {
                    ConnectionEstablished?.Invoke(this, connection);
                }
            }
            else
            {
                // Update existing connection
                connection.LastActivity = packet.Timestamp;
                connection.TotalBytes += packet.Size;
                connection.PacketCount++;
            }
        }

        private string GetDeviceName(string ipAddress)
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(ipAddress);
                return hostEntry.HostName;
            }
            catch
            {
                return ipAddress; // Fallback to IP if hostname resolution fails
            }
        }

        private async Task GenerateSnapshotsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsCapturing)
            {
                try
                {
                    await Task.Delay(_settings.SnapshotIntervalMs, cancellationToken);
                    
                    var snapshot = await GetCurrentSnapshotAsync();
                    SnapshotCaptured?.Invoke(this, snapshot);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error generating snapshot: {ex.Message}");
                }
            }
        }

        public async Task<NetworkSnapshot> GetCurrentSnapshotAsync()
        {
            return await Task.FromResult(new NetworkSnapshot
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Devices = _devices.Values.ToList(),
                Connections = _connections.Values.ToList(),
                RecentPackets = _recentPackets.ToList(),
                TotalBytesTransferred = _totalBytesTransferred,
                CaptureStartTime = _captureStartTime,
                Statistics = await GetStatisticsAsync()
            });
        }

        public async Task<List<NetworkDevice>> GetDevicesAsync()
        {
            return await Task.FromResult(_devices.Values.ToList());
        }

        public async Task<List<NetworkConnection>> GetConnectionsAsync()
        {
            return await Task.FromResult(_connections.Values.ToList());
        }

        public async Task SetProtocolFilterAsync(string[] protocols)
        {
            if (_captureDevice != null && protocols.Length > 0)
            {
                var filter = string.Join(" or ", protocols.Select(p => p.ToLower()));
                _captureDevice.Filter = filter;
                _logger.LogInformation($"Set capture filter: {filter}");
            }
            await Task.CompletedTask;
        }

        public async Task<CaptureStatistics> GetStatisticsAsync()
        {
            var captureTime = IsCapturing ? DateTime.UtcNow - _captureStartTime : TimeSpan.Zero;
            var totalSeconds = captureTime.TotalSeconds;
            
            return await Task.FromResult(new CaptureStatistics
            {
                TotalPacketsCaptured = (int)_totalPacketsCaptured,
                TotalBytesTransferred = _totalBytesTransferred,
                DevicesDiscovered = _devices.Count,
                ActiveConnections = _connections.Count,
                CaptureTime = captureTime,
                PacketsPerSecond = totalSeconds > 0 ? _totalPacketsCaptured / totalSeconds : 0,
                BytesPerSecond = totalSeconds > 0 ? _totalBytesTransferred / totalSeconds : 0,
                ProtocolDistribution = new Dictionary<string, int>(_protocolDistribution)
            });
        }

        public void Dispose()
        {
            try
            {
                if (IsCapturing)
                {
                    StopCaptureAsync().Wait(5000);
                }
                
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _captureDevice?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error disposing NetworkCaptureService: {ex.Message}");
            }
        }
    }
} 