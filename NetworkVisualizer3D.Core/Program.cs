using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NetworkVisualizer3D.Core.Configuration;
using NetworkVisualizer3D.Core.Services;
using NetworkVisualizer3D.Core.Interfaces;

namespace NetworkVisualizer3D.Core
{
    class Program
    {
        private static AppSettings? _appSettings;
        private static INetworkCaptureService? _captureService;
        private static IApiService? _apiService;
        private static ILogger? _logger;
        private static IConfigurationRoot? _configuration;
        private static Task? _captureTask;
        private static Task? _apiTask;

        static async Task Main(string[] args)
        {
            Console.Title = "NetworkVisualizer3D - Network Traffic Analyzer";
            
            // Initialize logger first
            _logger = new ConsoleLogger();
            
            // Load configuration
            LoadConfiguration();

            // Exit if configuration failed critically
            if (_appSettings == null)
            {
                PrintError("Critical configuration missing. Please check appsettings.json.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Initialize services
            InitializeServices();

            // Check for command line arguments
            if (args.Length > 0)
            {
                await HandleCommandLineArgs(args);
                return;
            }

            // Main application loop
            await RunDashboardLoop();

            // Cleanup
            await CleanupServices();

            Console.WriteLine("\nExiting NetworkVisualizer3D. Goodbye!");
        }

        static void LoadConfiguration()
        {
            Console.WriteLine("Loading configuration from appsettings.json...");
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                _configuration = builder.Build();

                _appSettings = new AppSettings();
                _configuration.Bind("AppSettings", _appSettings);

                Console.WriteLine("Configuration loaded successfully.");
            }
            catch (FileNotFoundException)
            {
                PrintError("Error: appsettings.json not found in the application directory.");
                _appSettings = null;
            }
            catch (Exception ex)
            {
                PrintError($"Error loading configuration: {ex.Message}");
                _appSettings = null;
            }
        }

        static void InitializeServices()
        {
            try
            {
                if (_appSettings != null && _logger != null)
                {
                    _captureService = new NetworkCaptureService(_appSettings.NetworkCapture, _logger);
                    _logger.LogInformation("Network capture service initialized");
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error initializing services: {ex.Message}");
            }
        }

        static async Task HandleCommandLineArgs(string[] args)
        {
            var command = args[0].ToLower();
            
            switch (command)
            {
                case "demo":
                    await RunDemoMode();
                    break;
                case "capture":
                    await RunCaptureMode(args);
                    break;
                case "interfaces":
                    await ListNetworkInterfaces();
                    break;
                case "help":
                case "--help":
                case "-h":
                    ShowHelp();
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowHelp();
                    break;
            }
        }

        static async Task RunDemoMode()
        {
            Console.WriteLine("Starting NetworkVisualizer3D in demo mode...");
            Console.WriteLine("Demo mode provides simulated network data for visualization.");
            Console.WriteLine("Press Ctrl+C to stop the demo.");
            
            // TODO: Implement demo mode with simulated data
            await Task.Delay(1000);
            Console.WriteLine("Demo mode would run here with simulated network data.");
        }

        static async Task RunCaptureMode(string[] args)
        {
            if (_captureService == null)
            {
                PrintError("Capture service not initialized");
                return;
            }

            string? interfaceName = null;
            if (args.Length > 1)
            {
                interfaceName = args[1];
            }
            else
            {
                // List interfaces and let user choose
                var interfaces = await _captureService.GetAvailableInterfacesAsync();
                if (interfaces.Count == 0)
                {
                    PrintError("No network interfaces found");
                    return;
                }

                Console.WriteLine("Available network interfaces:");
                for (int i = 0; i < interfaces.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {interfaces[i]}");
                }

                Console.Write("Select interface (number): ");
                if (int.TryParse(Console.ReadLine(), out int selection) && 
                    selection > 0 && selection <= interfaces.Count)
                {
                    interfaceName = interfaces[selection - 1];
                }
                else
                {
                    PrintError("Invalid selection");
                    return;
                }
            }

            if (!string.IsNullOrEmpty(interfaceName))
            {
                Console.WriteLine($"Starting packet capture on {interfaceName}...");
                var success = await _captureService.StartCaptureAsync(interfaceName);
                if (success)
                {
                    Console.WriteLine("Packet capture started. Press any key to stop...");
                    Console.ReadKey();
                    await _captureService.StopCaptureAsync();
                }
                else
                {
                    PrintError("Failed to start packet capture");
                }
            }
        }

        static async Task ListNetworkInterfaces()
        {
            if (_captureService == null)
            {
                PrintError("Capture service not initialized");
                return;
            }

            Console.WriteLine("Available network interfaces:");
            var interfaces = await _captureService.GetAvailableInterfacesAsync();
            
            if (interfaces.Count == 0)
            {
                Console.WriteLine("No network interfaces found.");
                Console.WriteLine("Make sure you have administrator privileges and Npcap installed.");
            }
            else
            {
                for (int i = 0; i < interfaces.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {interfaces[i]}");
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("NetworkVisualizer3D - Network Traffic Analyzer");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  NetworkVisualizer3D.exe [command] [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  demo                    Run in demo mode with simulated data");
            Console.WriteLine("  capture [interface]     Start packet capture on specified interface");
            Console.WriteLine("  interfaces              List available network interfaces");
            Console.WriteLine("  help                    Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  NetworkVisualizer3D.exe demo");
            Console.WriteLine("  NetworkVisualizer3D.exe capture \"Wi-Fi\"");
            Console.WriteLine("  NetworkVisualizer3D.exe interfaces");
            Console.WriteLine();
            Console.WriteLine("If no command is specified, the interactive dashboard will start.");
        }

        static async Task RunDashboardLoop()
        {
            bool keepRunning = true;
            while (keepRunning)
            {
                PrintDashboardMenu();
                string? choice = Console.ReadLine();

                switch (choice?.ToLower())
                {
                    case "1":
                        await StartNetworkCapture();
                        break;
                        
                    case "2":
                        await StopNetworkCapture();
                        break;
                        
                    case "3":
                        await ShowCaptureStatistics();
                        break;
                        
                    case "4":
                        await ListAvailableInterfaces();
                        break;
                        
                    case "5":
                        await StartApiServer();
                        break;
                        
                    case "6":
                        await StopApiServer();
                        break;
                        
                    case "7":
                        await ShowCurrentSnapshot();
                        break;
                        
                    case "8":
                        ShowConfiguration();
                        break;
                        
                    case "0":
                    case "q":
                    case "exit":
                        keepRunning = false;
                        break;
                        
                    default:
                        PrintWarning("Invalid choice. Please try again.");
                        break;
                }

                if (keepRunning)
                {
                    Console.WriteLine("\nPress Enter to return to the dashboard...");
                    Console.ReadLine();
                }
            }
        }

        static void PrintDashboardMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("   NetworkVisualizer3D Dashboard");
            Console.WriteLine("========================================");
            Console.ResetColor();
            
            Console.WriteLine("--- Network Capture ---");
            Console.WriteLine($" 1. Start Network Capture [Status: {(_captureService?.IsCapturing == true ? "Running" : "Stopped")}]");
            Console.WriteLine(" 2. Stop Network Capture");
            Console.WriteLine(" 3. Show Capture Statistics");
            Console.WriteLine(" 4. List Network Interfaces");
            
            Console.WriteLine("--- API Server ---");
            Console.WriteLine($" 5. Start API Server [Status: {(_apiService?.IsRunning == true ? "Running" : "Stopped")}]");
            Console.WriteLine(" 6. Stop API Server");
            
            Console.WriteLine("--- Data Analysis ---");
            Console.WriteLine(" 7. Show Current Network Snapshot");
            Console.WriteLine(" 8. Show Configuration");
            
            Console.WriteLine("--- System ---");
            Console.WriteLine(" 0. Exit");
            Console.WriteLine("========================================");
            Console.Write("Enter your choice: ");
        }

        static async Task StartNetworkCapture()
        {
            if (_captureService == null)
            {
                PrintError("Capture service not initialized");
                return;
            }

            if (_captureService.IsCapturing)
            {
                PrintWarning("Network capture is already running");
                return;
            }

            try
            {
                var interfaces = await _captureService.GetAvailableInterfacesAsync();
                if (interfaces.Count == 0)
                {
                    PrintError("No network interfaces found. Make sure you have administrator privileges and Npcap installed.");
                    return;
                }

                Console.WriteLine("Available network interfaces:");
                for (int i = 0; i < interfaces.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {interfaces[i]}");
                }

                Console.Write("Select interface (number): ");
                if (int.TryParse(Console.ReadLine(), out int selection) && 
                    selection > 0 && selection <= interfaces.Count)
                {
                    var interfaceName = interfaces[selection - 1];
                    Console.WriteLine($"Starting packet capture on {interfaceName}...");
                    
                    var success = await _captureService.StartCaptureAsync(interfaceName);
                    if (success)
                    {
                        Console.WriteLine("Packet capture started successfully!");
                    }
                    else
                    {
                        PrintError("Failed to start packet capture");
                    }
                }
                else
                {
                    PrintError("Invalid selection");
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error starting capture: {ex.Message}");
            }
        }

        static async Task StopNetworkCapture()
        {
            if (_captureService == null)
            {
                PrintError("Capture service not initialized");
                return;
            }

            if (!_captureService.IsCapturing)
            {
                PrintWarning("Network capture is not running");
                return;
            }

            try
            {
                Console.WriteLine("Stopping packet capture...");
                await _captureService.StopCaptureAsync();
                Console.WriteLine("Packet capture stopped successfully!");
            }
            catch (Exception ex)
            {
                PrintError($"Error stopping capture: {ex.Message}");
            }
        }

        static async Task ShowCaptureStatistics()
        {
            if (_captureService == null)
            {
                PrintError("Capture service not initialized");
                return;
            }

            try
            {
                var stats = await _captureService.GetStatisticsAsync();
                
                Console.WriteLine("=== Capture Statistics ===");
                Console.WriteLine($"Total Packets Captured: {stats.TotalPacketsCaptured:N0}");
                Console.WriteLine($"Total Bytes Transferred: {stats.TotalBytesTransferred:N0}");
                Console.WriteLine($"Devices Discovered: {stats.DevicesDiscovered}");
                Console.WriteLine($"Active Connections: {stats.ActiveConnections}");
                Console.WriteLine($"Capture Time: {stats.CaptureTime}");
                Console.WriteLine($"Packets/Second: {stats.PacketsPerSecond:F2}");
                Console.WriteLine($"Bytes/Second: {stats.BytesPerSecond:F2}");
                
                if (stats.ProtocolDistribution.Count > 0)
                {
                    Console.WriteLine("\nProtocol Distribution:");
                    foreach (var protocol in stats.ProtocolDistribution.OrderByDescending(p => p.Value))
                    {
                        Console.WriteLine($"  {protocol.Key}: {protocol.Value:N0} packets");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error getting statistics: {ex.Message}");
            }
        }

        static async Task ListAvailableInterfaces()
        {
            if (_captureService == null)
            {
                PrintError("Capture service not initialized");
                return;
            }

            try
            {
                var interfaces = await _captureService.GetAvailableInterfacesAsync();
                
                Console.WriteLine("=== Available Network Interfaces ===");
                if (interfaces.Count == 0)
                {
                    Console.WriteLine("No network interfaces found.");
                    Console.WriteLine("Make sure you have administrator privileges and Npcap installed.");
                }
                else
                {
                    for (int i = 0; i < interfaces.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {interfaces[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error listing interfaces: {ex.Message}");
            }
        }

        static async Task StartApiServer()
        {
            // TODO: Implement API server startup
            Console.WriteLine("API Server functionality will be implemented next.");
            await Task.CompletedTask;
        }

        static async Task StopApiServer()
        {
            // TODO: Implement API server shutdown
            Console.WriteLine("API Server functionality will be implemented next.");
            await Task.CompletedTask;
        }

        static async Task ShowCurrentSnapshot()
        {
            if (_captureService == null)
            {
                PrintError("Capture service not initialized");
                return;
            }

            try
            {
                var snapshot = await _captureService.GetCurrentSnapshotAsync();
                
                Console.WriteLine("=== Current Network Snapshot ===");
                Console.WriteLine($"Timestamp: {snapshot.Timestamp}");
                Console.WriteLine($"Devices: {snapshot.Devices.Count}");
                Console.WriteLine($"Connections: {snapshot.Connections.Count}");
                Console.WriteLine($"Recent Packets: {snapshot.RecentPackets.Count}");
                Console.WriteLine($"Total Bytes: {snapshot.TotalBytesTransferred:N0}");
                
                if (snapshot.Devices.Count > 0)
                {
                    Console.WriteLine("\nTop Devices by Traffic:");
                    var topDevices = snapshot.Devices
                        .OrderByDescending(d => d.TotalTraffic)
                        .Take(5);
                    
                    foreach (var device in topDevices)
                    {
                        Console.WriteLine($"  {device.IpAddress} ({device.Name}): {device.TotalTraffic:N0} bytes");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error getting snapshot: {ex.Message}");
            }
        }

        static void ShowConfiguration()
        {
            if (_appSettings == null)
            {
                PrintError("Configuration not loaded");
                return;
            }

            Console.WriteLine("=== Current Configuration ===");
            Console.WriteLine($"Capture Timeout: {_appSettings.NetworkCapture.CaptureTimeoutMs}ms");
            Console.WriteLine($"Snapshot Interval: {_appSettings.NetworkCapture.SnapshotIntervalMs}ms");
            Console.WriteLine($"Max Packets Per Snapshot: {_appSettings.NetworkCapture.MaxPacketsPerSnapshot}");
            Console.WriteLine($"API Server Port: {_appSettings.ApiServer.Port}");
            Console.WriteLine($"Enable Real-time Capture: {_appSettings.NetworkCapture.EnableRealTimeCapture}");
            Console.WriteLine($"Save Capture Files: {_appSettings.NetworkCapture.SaveCaptureFiles}");
            Console.WriteLine($"Capture Directory: {_appSettings.NetworkCapture.CaptureDirectory}");
        }

        static async Task CleanupServices()
        {
            try
            {
                if (_captureService?.IsCapturing == true)
                {
                    await _captureService.StopCaptureAsync();
                }
                
                _captureService?.Dispose();
                _apiService?.Dispose();
            }
            catch (Exception ex)
            {
                PrintError($"Error during cleanup: {ex.Message}");
            }
        }

        // Helper methods for colored console output
        static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {message}");
            Console.ResetColor();
        }
        
        static void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"WARNING: {message}");
            Console.ResetColor();
        }
        
        static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"SUCCESS: {message}");
            Console.ResetColor();
        }
    }
}
