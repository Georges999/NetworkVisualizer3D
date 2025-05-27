using NetworkVisualizer3D.Core.Models;

namespace NetworkVisualizer3D.Core.Interfaces
{
    /// <summary>
    /// Interface for network packet capture services
    /// </summary>
    public interface INetworkCaptureService : IDisposable
    {
        /// <summary>
        /// Event fired when a new network snapshot is captured
        /// </summary>
        event EventHandler<NetworkSnapshot>? SnapshotCaptured;
        
        /// <summary>
        /// Event fired when a new packet is captured
        /// </summary>
        event EventHandler<NetworkPacket>? PacketCaptured;
        
        /// <summary>
        /// Event fired when a new device is discovered
        /// </summary>
        event EventHandler<NetworkDevice>? DeviceDiscovered;
        
        /// <summary>
        /// Event fired when a new connection is established
        /// </summary>
        event EventHandler<NetworkConnection>? ConnectionEstablished;
        
        /// <summary>
        /// Gets whether the capture service is currently running
        /// </summary>
        bool IsCapturing { get; }
        
        /// <summary>
        /// Gets the list of available network interfaces
        /// </summary>
        /// <returns>List of interface names</returns>
        Task<List<string>> GetAvailableInterfacesAsync();
        
        /// <summary>
        /// Starts packet capture on the specified interface
        /// </summary>
        /// <param name="interfaceName">Name of the network interface</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if capture started successfully</returns>
        Task<bool> StartCaptureAsync(string interfaceName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops packet capture
        /// </summary>
        /// <returns>Task representing the stop operation</returns>
        Task StopCaptureAsync();
        
        /// <summary>
        /// Gets the current network snapshot
        /// </summary>
        /// <returns>Current network snapshot</returns>
        Task<NetworkSnapshot> GetCurrentSnapshotAsync();
        
        /// <summary>
        /// Gets captured devices
        /// </summary>
        /// <returns>List of discovered devices</returns>
        Task<List<NetworkDevice>> GetDevicesAsync();
        
        /// <summary>
        /// Gets active connections
        /// </summary>
        /// <returns>List of active connections</returns>
        Task<List<NetworkConnection>> GetConnectionsAsync();
        
        /// <summary>
        /// Sets protocol filter for packet capture
        /// </summary>
        /// <param name="protocols">Protocols to capture</param>
        Task SetProtocolFilterAsync(string[] protocols);
        
        /// <summary>
        /// Gets capture statistics
        /// </summary>
        /// <returns>Capture statistics</returns>
        Task<CaptureStatistics> GetStatisticsAsync();
    }
    

} 