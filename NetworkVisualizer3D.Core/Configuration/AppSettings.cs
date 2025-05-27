using System.ComponentModel.DataAnnotations;

namespace NetworkVisualizer3D.Core.Configuration
{
    /// <summary>
    /// Main application settings container
    /// </summary>
    public class AppSettings
    {
        public NetworkCaptureSettings NetworkCapture { get; set; } = new();
        public VisualizationSettings Visualization { get; set; } = new();
        public ApiServerSettings ApiServer { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public AnalyticsSettings Analytics { get; set; } = new();
    }

    /// <summary>
    /// Network packet capture configuration
    /// </summary>
    public class NetworkCaptureSettings
    {
        [Required]
        public string DefaultInterface { get; set; } = string.Empty;
        
        public int CaptureTimeoutMs { get; set; } = 30000; // 30 seconds
        public int SnapshotIntervalMs { get; set; } = 5000; // 5 seconds
        public int MaxPacketsPerSnapshot { get; set; } = 10000;
        public bool EnableRealTimeCapture { get; set; } = true;
        public bool SaveCaptureFiles { get; set; } = true;
        public string CaptureDirectory { get; set; } = "Captures";
        public string[] FilteredProtocols { get; set; } = { "TCP", "UDP", "ICMP" };
        public bool EnableDeepPacketInspection { get; set; } = false;
        public int BufferSizeKB { get; set; } = 1024; // 1MB buffer
    }

    /// <summary>
    /// 3D visualization configuration
    /// </summary>
    public class VisualizationSettings
    {
        public string OutputDirectory { get; set; } = "Visualizations";
        public bool EnableRealTimeVisualization { get; set; } = true;
        public int MaxDevicesDisplayed { get; set; } = 100;
        public int MaxConnectionsDisplayed { get; set; } = 500;
        public bool EnableAnimations { get; set; } = true;
        public float AnimationSpeedMultiplier { get; set; } = 1.0f;
        public bool EnableTrafficFlow { get; set; } = true;
        public string DefaultColorScheme { get; set; } = "Protocol"; // Protocol, DeviceType, Traffic
        public bool EnableTooltips { get; set; } = true;
        public int RefreshIntervalMs { get; set; } = 1000;
    }

    /// <summary>
    /// API server configuration
    /// </summary>
    public class ApiServerSettings
    {
        [Required]
        public string BaseUrl { get; set; } = "http://localhost:9000/";
        
        public int Port { get; set; } = 9000;
        public bool EnableCors { get; set; } = true;
        public string[] AllowedOrigins { get; set; } = { "*" };
        public bool EnableAuthentication { get; set; } = false;
        public string ApiKey { get; set; } = string.Empty;
        public int RequestTimeoutMs { get; set; } = 30000;
        public bool EnableRateLimiting { get; set; } = false;
        public int MaxRequestsPerMinute { get; set; } = 100;
        public bool EnableSwagger { get; set; } = true;
    }

    /// <summary>
    /// Database configuration
    /// </summary>
    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = "Data Source=networkvisualizer.db";
        public string DatabaseType { get; set; } = "SQLite"; // SQLite, PostgreSQL, MySQL
        public bool EnableMigrations { get; set; } = true;
        public int CommandTimeoutSeconds { get; set; } = 30;
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public bool LogQueries { get; set; } = false;
        public int DataRetentionDays { get; set; } = 30;
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information"; // Trace, Debug, Information, Warning, Error, Critical
        public string LogDirectory { get; set; } = "Logs";
        public string LogFilePattern { get; set; } = "networkvisualizer-{Date}.log";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableStructuredLogging { get; set; } = true;
        public int MaxLogFileSizeMB { get; set; } = 100;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnablePerformanceLogging { get; set; } = false;
    }

    /// <summary>
    /// Security configuration
    /// </summary>
    public class SecuritySettings
    {
        public bool RequireAdminPrivileges { get; set; } = true;
        public bool EnableEncryption { get; set; } = false;
        public string EncryptionKey { get; set; } = string.Empty;
        public bool EnableAuditLogging { get; set; } = true;
        public string[] TrustedNetworks { get; set; } = { "192.168.0.0/16", "10.0.0.0/8", "172.16.0.0/12" };
        public bool EnableThreatDetection { get; set; } = false;
        public int MaxFailedAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
    }

    /// <summary>
    /// Analytics and monitoring configuration
    /// </summary>
    public class AnalyticsSettings
    {
        public bool EnableAnalytics { get; set; } = true;
        public bool EnableAnomalyDetection { get; set; } = false;
        public double AnomalyThreshold { get; set; } = 0.8;
        public int AnalysisWindowMinutes { get; set; } = 15;
        public bool EnableTrafficProfiling { get; set; } = true;
        public bool EnableDeviceFingerprinting { get; set; } = false;
        public bool EnableGeoLocation { get; set; } = false;
        public string GeoLocationApiKey { get; set; } = string.Empty;
        public bool EnableReporting { get; set; } = true;
        public string ReportDirectory { get; set; } = "Reports";
        public int ReportGenerationIntervalHours { get; set; } = 24;
    }
} 