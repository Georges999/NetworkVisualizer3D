using NetworkVisualizer3D.Core.Models;

namespace NetworkVisualizer3D.Core.Interfaces
{
    /// <summary>
    /// Interface for API server services
    /// </summary>
    public interface IApiService : IDisposable
    {
        /// <summary>
        /// Gets whether the API server is running
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// Gets the base URL of the API server
        /// </summary>
        string BaseUrl { get; }
        
        /// <summary>
        /// Starts the API server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the start operation</returns>
        Task StartAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops the API server
        /// </summary>
        /// <returns>Task representing the stop operation</returns>
        Task StopAsync();
        
        /// <summary>
        /// Gets the latest network snapshot
        /// </summary>
        /// <returns>Latest network snapshot</returns>
        Task<NetworkSnapshot?> GetLatestSnapshotAsync();
        
        /// <summary>
        /// Gets a specific snapshot by ID
        /// </summary>
        /// <param name="snapshotId">Snapshot ID</param>
        /// <returns>Network snapshot or null if not found</returns>
        Task<NetworkSnapshot?> GetSnapshotAsync(string snapshotId);
        
        /// <summary>
        /// Gets a list of available snapshots
        /// </summary>
        /// <returns>List of snapshot metadata</returns>
        Task<List<SnapshotMetadata>> GetSnapshotListAsync();
        
        /// <summary>
        /// Gets demo data for visualization
        /// </summary>
        /// <returns>Demo network snapshot</returns>
        Task<NetworkSnapshot> GetDemoDataAsync();
        
        /// <summary>
        /// Gets current capture statistics
        /// </summary>
        /// <returns>Capture statistics</returns>
        Task<CaptureStatistics> GetStatisticsAsync();
        
        /// <summary>
        /// Gets security alerts
        /// </summary>
        /// <returns>List of security alerts</returns>
        Task<List<SecurityAlert>> GetSecurityAlertsAsync();
    }
    
    /// <summary>
    /// Snapshot metadata for listing
    /// </summary>
    public class SnapshotMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int DeviceCount { get; set; }
        public int ConnectionCount { get; set; }
        public long TotalBytes { get; set; }
        public string Description { get; set; } = string.Empty;
    }
} 