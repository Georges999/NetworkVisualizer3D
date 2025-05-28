# 🌐 NetworkVisualizer3D - Web-Based Network Visualization

[![Node.js](https://img.shields.io/badge/Node.js-18+-green.svg)](https://nodejs.org/)
[![React](https://img.shields.io/badge/React-18+-blue.svg)](https://reactjs.org/)
[![Three.js](https://img.shields.io/badge/Three.js-Latest-orange.svg)](https://threejs.org/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Cross--Platform-lightgrey.svg)](https://nodejs.org/)

> A modern web-based 3D network visualization tool that discovers and displays network devices and their connections in real-time using Three.js and React.

## ✨ Overview

NetworkVisualizer3D is a comprehensive network analysis platform that combines advanced packet capture capabilities with intelligent device detection, security threat analysis, and 3D network visualization. Built with .NET 9.0 and leveraging industry-standard libraries like SharpPcap and PacketDotNet, it provides both real-time monitoring and historical analysis capabilities.

### 🎯 Key Highlights

- **🔍 Real-time Packet Capture** - Advanced packet interception with multi-interface support
- **🤖 Intelligent Device Detection** - Automatic classification using MAC OUI database and traffic patterns
- **🛡️ Security Analysis** - Built-in threat detection for SQL injection, XSS, and suspicious traffic
- **📊 3D Network Visualization** - Interactive topology mapping with force-directed layouts
- **⚡ High Performance** - Optimized for high-throughput network environments
- **🎛️ Interactive Dashboard** - Professional console interface with real-time statistics

## 🚀 Quick Start

### Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| **Windows** | 10/11 | Administrator privileges required |
| **.NET** | 9.0+ | [Download here](https://dotnet.microsoft.com/download) |
| **Npcap** | Latest | [Download here](https://npcap.com/) - WinPcap successor |

### Installation

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/NetworkVisualizer3D.git
cd NetworkVisualizer3D

# 2. Build the project
cd NetworkVisualizer3D.Core
dotnet restore
dotnet build

# 3. Run the application
dotnet run
```

### First Run

1. **Install Npcap** - Download from [npcap.com](https://npcap.com/) and install with "WinPcap API-compatible Mode" enabled
2. **Run as Administrator** - Required for packet capture functionality
3. **Select Network Interface** - Choose from available interfaces in the dashboard

## 🎮 Usage

### Interactive Dashboard

Launch the application to access the professional dashboard interface:

```bash
dotnet run
```

```
========================================
   NetworkVisualizer3D Dashboard
========================================
--- Network Capture ---
 1. Start Network Capture [Status: Stopped]
 2. Stop Network Capture
 3. Show Capture Statistics
 4. List Network Interfaces
--- API Server ---
 5. Start API Server [Status: Stopped]
 6. Stop API Server
--- Data Analysis ---
 7. Show Current Network Snapshot
 8. Show Configuration
--- System ---
 0. Exit
========================================
```

### Command Line Interface

```bash
# List available network interfaces
dotnet run interfaces

# Start capture on specific interface
dotnet run capture "Wi-Fi"

# Show help
dotnet run help

# Demo mode (coming soon)
dotnet run demo
```

## 🏗️ Architecture

### Project Structure

```
NetworkVisualizer3D.Core/
├── 📁 Configuration/           # Application settings
│   └── AppSettings.cs         # Comprehensive configuration model
├── 📁 Interfaces/             # Service contracts
│   ├── INetworkCaptureService.cs
│   ├── IApiService.cs
│   └── ILogger.cs
├── 📁 Models/                 # Data models
│   └── NetworkModels.cs       # Network entities & enums
├── 📁 Services/               # Core services
│   ├── NetworkCaptureService.cs  # Packet capture engine
│   └── ConsoleLogger.cs          # Logging implementation
├── 📁 Utils/                  # Utility classes
│   ├── DeviceTypeDetector.cs     # Device classification
│   ├── PositionCalculator.cs     # 3D positioning algorithms
│   └── HttpAnalyzer.cs           # HTTP security analysis
├── Program.cs                 # Application entry point
├── appsettings.json          # Configuration file
└── NetworkVisualizer3D.Core.csproj
```

### Core Components

#### 🔧 NetworkCaptureService
- **Real-time packet capture** using SharpPcap
- **Protocol analysis** for TCP, UDP, ICMP, HTTP, DNS
- **Device discovery** and connection tracking
- **Security threat detection** with configurable rules

#### 🎯 DeviceTypeDetector
- **MAC OUI database** for vendor identification
- **Traffic pattern analysis** for device classification
- **IP range detection** for network topology mapping

#### 🛡️ HttpAnalyzer
- **Security threat detection** (SQL injection, XSS)
- **Sensitive data exposure** monitoring
- **Suspicious user agent** detection
- **HTTP traffic analysis** with payload inspection

#### 📐 PositionCalculator
- **Force-directed layout** algorithms
- **Hierarchical positioning** based on device types
- **3D coordinate calculation** for network visualization
- **Collision avoidance** and optimal spacing

## ⚙️ Configuration

### Network Capture Settings

```json
{
  "NetworkCapture": {
    "DefaultInterface": "",
    "CaptureTimeoutMs": 30000,
    "SnapshotIntervalMs": 5000,
    "MaxPacketsPerSnapshot": 10000,
    "EnableRealTimeCapture": true,
    "SaveCaptureFiles": true,
    "CaptureDirectory": "Captures",
    "FilteredProtocols": ["TCP", "UDP", "ICMP"],
    "EnableDeepPacketInspection": false,
    "BufferSizeKB": 1024
  }
}
```

### Visualization Settings

```json
{
  "Visualization": {
    "OutputDirectory": "Visualizations",
    "EnableRealTimeVisualization": true,
    "MaxDevicesDisplayed": 100,
    "MaxConnectionsDisplayed": 500,
    "EnableAnimations": true,
    "AnimationSpeedMultiplier": 1.0,
    "EnableTrafficFlow": true,
    "DefaultColorScheme": "Protocol",
    "RefreshIntervalMs": 1000
  }
}
```

### Security Settings

```json
{
  "Security": {
    "RequireAdminPrivileges": true,
    "EnableEncryption": false,
    "EnableAuditLogging": true,
    "TrustedNetworks": [
      "192.168.0.0/16",
      "10.0.0.0/8",
      "172.16.0.0/12"
    ],
    "EnableThreatDetection": false,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 15
  }
}
```

## 🔍 Features

### Device Detection & Classification

Automatically identifies and classifies network devices:

| Device Type | Detection Method | Examples |
|-------------|------------------|----------|
| **Computers** | MAC OUI + Traffic patterns | Desktops, Laptops |
| **Servers** | IP ranges + Port analysis | Web servers, Database servers |
| **Network Equipment** | Vendor identification | Routers, Switches, Access Points |
| **IoT Devices** | Traffic behavior | Smart home devices, Sensors |
| **Mobile Devices** | MAC patterns + User agents | Phones, Tablets |
| **Printers** | Protocol analysis | Network printers, Scanners |

### Security Analysis

Built-in threat detection capabilities:

- **🚨 SQL Injection Detection** - Pattern matching for malicious SQL queries
- **🔒 XSS Attack Prevention** - Cross-site scripting attempt identification
- **📊 Traffic Anomaly Detection** - Unusual traffic pattern analysis
- **🕵️ Suspicious User Agents** - Known attack tool identification
- **💳 Sensitive Data Exposure** - Credit cards, SSNs, API keys detection
- **🌐 Unencrypted Communications** - Plain text sensitive data monitoring

### 3D Visualization Algorithms

Advanced positioning and layout algorithms:

- **Force-Directed Layout** - Physics-based node positioning
- **Hierarchical Positioning** - Device type-based layering
- **Subnet-Based Clustering** - IP range grouping
- **Collision Avoidance** - Optimal spacing algorithms
- **Real-time Updates** - Dynamic position recalculation

## 📊 Statistics & Monitoring

### Real-time Metrics

- **Packets per second** - Live capture rate monitoring
- **Bytes transferred** - Network throughput analysis
- **Protocol distribution** - Traffic composition breakdown
- **Device count** - Active device tracking
- **Connection analysis** - Active connection monitoring
- **Security alerts** - Threat detection summary

### Performance Optimization

- **Concurrent processing** - Multi-threaded packet analysis
- **Memory management** - Efficient buffer handling
- **Configurable limits** - Adjustable performance parameters
- **Background processing** - Non-blocking capture operations

## 🛠️ Development

### Building from Source

```bash
# Clone and build
git clone https://github.com/yourusername/NetworkVisualizer3D.git
cd NetworkVisualizer3D/NetworkVisualizer3D.Core

# Restore dependencies
dotnet restore

# Build project
dotnet build --configuration Release

# Run tests (when available)
dotnet test
```

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **SharpPcap** | Latest | Network packet capture |
| **PacketDotNet** | Latest | Packet parsing and analysis |
| **Newtonsoft.Json** | Latest | JSON serialization |
| **Microsoft.Extensions.Configuration** | Latest | Configuration management |
| **Microsoft.Extensions.Configuration.Binder** | Latest | Configuration binding |

## 🚧 Roadmap

### Phase 1: Core Functionality ✅
- [x] Packet capture engine
- [x] Device detection
- [x] Security analysis
- [x] Console dashboard
- [x] Configuration system

### Phase 2: Web Interface 🚧
- [ ] REST API implementation
- [ ] Web-based dashboard
- [ ] Real-time WebSocket updates
- [ ] 3D visualization frontend

### Phase 3: Advanced Features 📋
- [ ] Machine learning threat detection
- [ ] Database persistence
- [ ] Report generation
- [ ] Network topology mapping
- [ ] Performance analytics

### Phase 4: Enterprise Features 📋
- [ ] Multi-user support
- [ ] Role-based access control
- [ ] Integration APIs
- [ ] Scalability improvements
- [ ] Cloud deployment options

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests (when test framework is available)
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **SharpPcap Team** - Excellent packet capture library
- **PacketDotNet Contributors** - Comprehensive packet parsing
- **Npcap Project** - Modern packet capture driver
- **Microsoft .NET Team** - Powerful development platform

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/NetworkVisualizer3D/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/NetworkVisualizer3D/discussions)
- **Documentation**: [Wiki](https://github.com/yourusername/NetworkVisualizer3D/wiki)

---

<div align="center">

**Made with ❤️ for network security professionals and developers**

[⭐ Star this project](https://github.com/yourusername/NetworkVisualizer3D) | [🐛 Report Bug](https://github.com/yourusername/NetworkVisualizer3D/issues) | [💡 Request Feature](https://github.com/yourusername/NetworkVisualizer3D/issues)

</div>
