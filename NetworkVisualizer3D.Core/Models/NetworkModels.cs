using System;
using System.Collections.Generic;

namespace NetworkVisualizer3D.Core.Models
{
    /// <summary>
    /// Represents a network device discovered during packet capture
    /// </summary>
    public class NetworkDevice
    {
        public string Id { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public long TotalTraffic { get; set; }
        public int PacketsSent { get; set; }
        public int PacketsReceived { get; set; }
        public Vector3D Position { get; set; } = new();
        public string Vendor { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public List<string> OpenPorts { get; set; } = new();
        public SecurityAnalysis SecurityAnalysis { get; set; } = new();
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// Represents a network connection between two devices
    /// </summary>
    public class NetworkConnection
    {
        public string Id { get; set; } = string.Empty;
        public string SourceIp { get; set; } = string.Empty;
        public string DestinationIp { get; set; } = string.Empty;
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        public ProtocolType Protocol { get; set; }
        public ConnectionState State { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivity { get; set; }
        public long TotalBytes { get; set; }
        public int PacketCount { get; set; }
        public string ApplicationProtocol { get; set; } = string.Empty;
        public List<SecurityAlert> SecurityAlerts { get; set; } = new();
        public TrafficMetrics TrafficMetrics { get; set; } = new();
    }

    /// <summary>
    /// Represents a captured network packet
    /// </summary>
    public class NetworkPacket
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string SourceIp { get; set; } = string.Empty;
        public string DestinationIp { get; set; } = string.Empty;
        public string SourceMac { get; set; } = string.Empty;
        public string DestinationMac { get; set; } = string.Empty;
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        public ProtocolType Protocol { get; set; }
        public int Size { get; set; }
        public string TcpFlags { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string HttpUrl { get; set; } = string.Empty;
        public string HttpUserAgent { get; set; } = string.Empty;
        public string DnsQuery { get; set; } = string.Empty;
        public List<SecurityAlert> SecurityAlerts { get; set; } = new();
        public byte[] PayloadData { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Represents a complete network snapshot at a point in time
    /// </summary>
    public class NetworkSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<NetworkDevice> Devices { get; set; } = new();
        public List<NetworkConnection> Connections { get; set; } = new();
        public List<NetworkPacket> RecentPackets { get; set; } = new();
        public long TotalBytesTransferred { get; set; }
        public DateTime CaptureStartTime { get; set; }
        public CaptureStatistics Statistics { get; set; } = new();
        public List<SecurityAlert> SecurityAlerts { get; set; } = new();
        public NetworkTopology Topology { get; set; } = new();
    }

    /// <summary>
    /// Represents a security alert or threat detection
    /// </summary>
    public class SecurityAlert
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public AlertType Type { get; set; }
        public ThreatLevel Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourceIp { get; set; } = string.Empty;
        public string DestinationIp { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 3D position vector for device positioning
    /// </summary>
    public class Vector3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3D() { }

        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Distance(Vector3D other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }

    /// <summary>
    /// Security analysis results for a device
    /// </summary>
    public class SecurityAnalysis
    {
        public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.Low;
        public List<string> Vulnerabilities { get; set; } = new();
        public List<string> SuspiciousActivities { get; set; } = new();
        public bool IsCompromised { get; set; }
        public DateTime LastAnalysis { get; set; }
        public Dictionary<string, object> AnalysisData { get; set; } = new();
    }

    /// <summary>
    /// Traffic metrics for connections
    /// </summary>
    public class TrafficMetrics
    {
        public double AveragePacketSize { get; set; }
        public double PacketsPerSecond { get; set; }
        public double BytesPerSecond { get; set; }
        public TimeSpan AverageLatency { get; set; }
        public int RetransmissionCount { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Network topology information
    /// </summary>
    public class NetworkTopology
    {
        public List<NetworkSubnet> Subnets { get; set; } = new();
        public List<NetworkGateway> Gateways { get; set; } = new();
        public string TopologyType { get; set; } = "Unknown"; // Star, Mesh, Bus, Ring, etc.
        public Dictionary<string, object> TopologyData { get; set; } = new();
    }

    /// <summary>
    /// Network subnet information
    /// </summary>
    public class NetworkSubnet
    {
        public string Id { get; set; } = string.Empty;
        public string NetworkAddress { get; set; } = string.Empty;
        public string SubnetMask { get; set; } = string.Empty;
        public List<string> DeviceIds { get; set; } = new();
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Network gateway information
    /// </summary>
    public class NetworkGateway
    {
        public string Id { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsDefaultGateway { get; set; }
    }

    /// <summary>
    /// HTTP analysis result
    /// </summary>
    public class HttpAnalysisResult
    {
        public string Method { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;
        public List<SecurityAlert> SecurityAlerts { get; set; } = new();
        public bool ContainsSensitiveData { get; set; }
    }

    /// <summary>
    /// Capture statistics
    /// </summary>
    public class CaptureStatistics
    {
        public int TotalPacketsCaptured { get; set; }
        public long TotalBytesTransferred { get; set; }
        public int DevicesDiscovered { get; set; }
        public int ActiveConnections { get; set; }
        public TimeSpan CaptureTime { get; set; }
        public double PacketsPerSecond { get; set; }
        public double BytesPerSecond { get; set; }
        public Dictionary<string, int> ProtocolDistribution { get; set; } = new();
    }

    // Enums
    public enum DeviceType
    {
        Unknown,
        Computer,
        Server,
        Router,
        Switch,
        AccessPoint,
        Firewall,
        LoadBalancer,
        Printer,
        MobilePhone,
        Tablet,
        IoTDevice,
        Camera,
        SmartTV,
        GameConsole,
        NetworkStorage
    }

    public enum ProtocolType
    {
        TCP,
        UDP,
        ICMP,
        ICMPv6,
        ARP,
        HTTP,
        HTTPS,
        DNS,
        DHCP,
        FTP,
        SSH,
        SMTP,
        POP3,
        IMAP,
        Other
    }

    public enum ConnectionState
    {
        Unknown,
        Established,
        SynSent,
        SynReceived,
        FinWait1,
        FinWait2,
        TimeWait,
        Closed,
        CloseWait,
        LastAck,
        Listen,
        Closing
    }

    public enum ThreatLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AlertType
    {
        SuspiciousTraffic,
        UnauthorizedAccess,
        MalwareDetection,
        DataExfiltration,
        DenialOfService,
        PortScan,
        BruteForceAttack,
        SqlInjection,
        CrossSiteScripting,
        UnencryptedSensitiveData,
        AnomalousTraffic,
        PolicyViolation,
        Other
    }
} 