using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;
using NetworkCityCore.Models;
using System.Net;
using System.Collections.Concurrent;

namespace NetworkCityCore.Services
{
    public class PacketCaptureService
    {
        private ILiveDevice? _captureDevice;
        private bool _isCapturing = false;
        private readonly ConcurrentDictionary<string, DeviceNode> _devices = new();
        private readonly ConcurrentDictionary<string, Connection> _connections = new();
        private readonly ConcurrentDictionary<string, int> _protocols = new();
        private readonly ConcurrentDictionary<string, int> _trafficVolumes = new();
        private int _totalPacketsCaptured = 0;
        private bool _isFilterActive = false;
        private HashSet<string> _filteredProtocols = new();

        public event EventHandler<NetworkSnapshot>? SnapshotCaptured;
        private readonly Timer _snapshotTimer;
        
        public PacketCaptureService()
        {
            // Create a timer that generates snapshots every second
            _snapshotTimer = new Timer(GenerateSnapshot, null, Timeout.Infinite, 1000);
        }

        public List<string> GetAvailableDevices()
        {
            var devices = new List<string>();
            
            try
            {
                foreach (var device in CaptureDeviceList.Instance)
                {
                    devices.Add($"{device.Name}: {device.Description}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing devices: {ex.Message}");
            }
            
            return devices;
        }

        public bool StartCapture(int deviceIndex)
        {
            try
            {
                if (_isCapturing)
                    return false;
                
                // Get available devices
                var devices = CaptureDeviceList.Instance;
                
                if (deviceIndex < 0 || deviceIndex >= devices.Count)
                    return false;
                
                _captureDevice = devices[deviceIndex];
                _captureDevice.Open(DeviceModes.Promiscuous);
                _captureDevice.Filter = "ip"; // Capture only IP packets
                
                // Register packet arrival event handler
                _captureDevice.OnPacketArrival += Device_OnPacketArrival;
                
                // Start capturing packets
                _captureDevice.StartCapture();
                _isCapturing = true;
                _totalPacketsCaptured = 0;
                
                // Clear previous data
                _devices.Clear();
                _connections.Clear();
                _protocols.Clear();
                _trafficVolumes.Clear();
                
                // Start generating snapshots
                _snapshotTimer.Change(1000, 1000);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting capture: {ex.Message}");
                return false;
            }
        }

        public void StopCapture()
        {
            if (!_isCapturing || _captureDevice == null)
                return;
            
            try
            {
                _snapshotTimer.Change(Timeout.Infinite, Timeout.Infinite);
                
                _captureDevice.StopCapture();
                _captureDevice.Close();
                _isCapturing = false;
                
                // Generate one final snapshot
                GenerateSnapshot(null);
                
                Console.WriteLine($"Capture finished. Total packets processed: {_totalPacketsCaptured}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping capture: {ex.Message}");
            }
        }

        public void SetProtocolFilter(string[] protocols, bool enable)
        {
            _filteredProtocols = new HashSet<string>(protocols, StringComparer.OrdinalIgnoreCase);
            _isFilterActive = enable && _filteredProtocols.Count > 0;
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                var rawPacket = e.GetPacket();
                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                
                // Try to extract IP packet
                var ipPacket = packet.Extract<IPPacket>();
                if (ipPacket == null) return;
                
                var sourceIp = ipPacket.SourceAddress.ToString();
                var destIp = ipPacket.DestinationAddress.ToString();
                var protocol = ipPacket.Protocol.ToString();
                var length = rawPacket.Data.Length;
                
                // Apply protocol filter if active
                if (_isFilterActive && !_filteredProtocols.Contains(protocol))
                    return;
                
                _totalPacketsCaptured++;
                
                // Update device information
                UpdateDevice(sourceIp, length);
                UpdateDevice(destIp, length);
                
                // Update connection information
                UpdateConnection(sourceIp, destIp, protocol, length);
                
                // Update protocol statistics
                _protocols.AddOrUpdate(protocol, 1, (_, count) => count + 1);
                
                // Update traffic volumes - group by minute for more meaningful data
                var timeKey = DateTime.Now.ToString("HH:mm");
                _trafficVolumes.AddOrUpdate(timeKey, length, (_, vol) => vol + length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing packet: {ex.Message}");
            }
        }

        private void UpdateDevice(string ip, int trafficSize)
        {
            _devices.AddOrUpdate(ip, 
                _ => new DeviceNode 
                { 
                    Id = ip, 
                    Name = GetHostName(ip),
                    DeviceType = DetermineDeviceType(ip),
                    TotalTraffic = trafficSize
                },
                (_, device) => 
                {
                    device.TotalTraffic += trafficSize;
                    return device;
                });
        }

        private string DetermineDeviceType(string ip)
        {
            // Simple heuristic for device type identification
            if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172."))
            {
                return "Local";
            }
            else if (ip == "127.0.0.1" || ip == "::1")
            {
                return "Localhost";
            }
            else
            {
                return "External";
            }
        }

        private void UpdateConnection(string sourceIp, string destIp, string protocol, int size)
        {
            var connectionKey = $"{sourceIp}-{destIp}-{protocol}";
            
            _connections.AddOrUpdate(connectionKey,
                _ => new Connection
                {
                    SourceId = sourceIp,
                    DestinationId = destIp,
                    Protocol = protocol,
                    Volume = size,
                    IsTwoWay = CheckIfTwoWayConnection(sourceIp, destIp, protocol),
                    LastActivity = DateTime.Now
                },
                (_, connection) =>
                {
                    connection.Volume += size;
                    connection.LastActivity = DateTime.Now;
                    return connection;
                });
            
            // Update connection count for source device
            if (_devices.TryGetValue(sourceIp, out var sourceDevice))
            {
                sourceDevice.ConnectionsCount.TryGetValue(destIp, out int count);
                sourceDevice.ConnectionsCount[destIp] = count + 1;
            }
        }

        private bool CheckIfTwoWayConnection(string sourceIp, string destIp, string protocol)
        {
            var reverseKey = $"{destIp}-{sourceIp}-{protocol}";
            return _connections.ContainsKey(reverseKey);
        }

        private string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
                return hostEntry.HostName;
            }
            catch
            {
                return ipAddress;
            }
        }

        private void GenerateSnapshot(object? state)
        {
            if (_devices.Count == 0)
                return;
                
            var snapshot = new NetworkSnapshot
            {
                TimeStamp = DateTime.Now,
                Devices = _devices.Values.ToList(),
                Connections = _connections.Values.ToList(),
                ProtocolDistribution = new Dictionary<string, int>(_protocols),
                TrafficVolumes = new Dictionary<string, int>(_trafficVolumes)
            };
            
            // Add layout positions for devices (for Unity visualization)
            AssignLayoutPositions(snapshot.Devices);
            
            // Raise event with the snapshot
            SnapshotCaptured?.Invoke(this, snapshot);
        }

        private void AssignLayoutPositions(List<DeviceNode> devices)
        {
            // Simple circular layout algorithm
            int totalDevices = devices.Count;
            double radius = Math.Max(5, totalDevices * 0.5);
            
            for (int i = 0; i < totalDevices; i++)
            {
                double angle = 2 * Math.PI * i / totalDevices;
                float x = (float)(radius * Math.Cos(angle));
                float z = (float)(radius * Math.Sin(angle));
                
                // Set higher Y position for devices with more traffic
                float trafficScale = Math.Min(5.0f, (float)Math.Log10(devices[i].TotalTraffic + 1) * 0.5f);
                
                devices[i].Position = new float[] { x, trafficScale, z };
            }
        }

        public NetworkSnapshot? GetLatestSnapshot()
        {
            if (_devices.Count == 0)
                return null;
                
            var snapshot = new NetworkSnapshot
            {
                TimeStamp = DateTime.Now,
                Devices = _devices.Values.ToList(),
                Connections = _connections.Values.ToList(),
                ProtocolDistribution = new Dictionary<string, int>(_protocols),
                TrafficVolumes = new Dictionary<string, int>(_trafficVolumes)
            };
            
            return snapshot;
        }
    }
}