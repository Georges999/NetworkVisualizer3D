using NetworkCityCore.Services;
using NetworkCityCore.Models;
using System.IO;
using Newtonsoft.Json;

namespace NetworkCityCore
{
    public class Program
    {
        private static string _snapshotsDirectory = "Snapshots";
        private static int _capturedSnapshotsCount = 0;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Network City Core - Packet Capture Demo");
            Console.WriteLine("=======================================");
            Console.ResetColor();

            while (true)
            {
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1. Start new capture");
                Console.WriteLine("2. View existing snapshots");
                Console.WriteLine("3. Exit");
                
                Console.Write("\nOption: ");
                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        StartCapture();
                        break;
                    case "2":
                        ViewSnapshots();
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        break;
                }
            }
        }

        private static void StartCapture()
        {
            _capturedSnapshotsCount = 0;
            
            // Create packet capture service
            var captureService = new PacketCaptureService();
            
            // Subscribe to snapshot event
            captureService.SnapshotCaptured += OnSnapshotCaptured;
            
            // Get available devices
            var devices = captureService.GetAvailableDevices();
            
            // Display devices
            Console.WriteLine("\nAvailable network devices:");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"[{i}] {devices[i]}");
            }
            
            // Ask user to select a device
            Console.Write("\nSelect a device number to capture: ");
            if (!int.TryParse(Console.ReadLine(), out int deviceIndex) || 
                deviceIndex < 0 || deviceIndex >= devices.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid device selection. Returning to menu.");
                Console.ResetColor();
                return;
            }

            Console.Write("Capture duration in seconds (or press Enter for manual stop): ");
            string? durationInput = Console.ReadLine();
            bool hasTimer = int.TryParse(durationInput, out int duration) && duration > 0;
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Starting capture...");
            Console.ResetColor();
            
            // Start capturing
            if (captureService.StartCapture(deviceIndex))
            {
                if (hasTimer)
                {
                    Console.WriteLine($"Capturing for {duration} seconds...");
                    Thread.Sleep(duration * 1000);
                    captureService.StopCapture();
                }
                else
                {
                    Console.WriteLine("Capture started. Press any key to stop...");
                    Console.ReadKey(true);
                    captureService.StopCapture();
                }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Capture stopped. {_capturedSnapshotsCount} snapshots saved to {_snapshotsDirectory}/");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to start capturing.");
                Console.ResetColor();
            }
        }

        private static void ViewSnapshots()
        {
            if (!Directory.Exists(_snapshotsDirectory))
            {
                Console.WriteLine("No snapshots found. Capture some network data first.");
                return;
            }

            var files = Directory.GetFiles(_snapshotsDirectory, "*.json").OrderByDescending(f => f).ToArray();
            
            if (files.Length == 0)
            {
                Console.WriteLine("No snapshots found in the directory.");
                return;
            }

            Console.WriteLine($"\nFound {files.Length} snapshots. Latest 10:");
            
            for (int i = 0; i < Math.Min(10, files.Length); i++)
            {
                var fileInfo = new FileInfo(files[i]);
                Console.WriteLine($"{i+1}. {Path.GetFileName(files[i])} - {fileInfo.Length / 1024} KB");
            }

            Console.Write("\nEnter snapshot number to view details (or 0 to return): ");
            
            if (int.TryParse(Console.ReadLine(), out int fileIndex) && fileIndex > 0 && fileIndex <= Math.Min(10, files.Length))
            {
                DisplaySnapshotDetails(files[fileIndex - 1]);
            }
        }

        private static void DisplaySnapshotDetails(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var snapshot = JsonConvert.DeserializeObject<NetworkSnapshot>(json);
                
                if (snapshot == null)
                {
                    Console.WriteLine("Failed to parse snapshot file.");
                    return;
                }

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Snapshot Details: {Path.GetFileName(filePath)}");
                Console.WriteLine("=======================================");
                Console.ResetColor();
                
                Console.WriteLine($"Timestamp: {snapshot.TimeStamp}");
                Console.WriteLine($"Devices: {snapshot.Devices.Count}");
                Console.WriteLine($"Connections: {snapshot.Connections.Count}");
                
                // Devices section
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nTop 5 Devices by Traffic:");
                Console.ResetColor();
                
                foreach (var device in snapshot.Devices.OrderByDescending(d => d.TotalTraffic).Take(5))
                {
                    Console.WriteLine($"  {device.Id} ({device.Name}): {device.TotalTraffic} bytes");
                }
                
                // Protocols section
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nProtocol Distribution:");
                Console.ResetColor();
                
                foreach (var proto in snapshot.ProtocolDistribution)
                {
                    Console.WriteLine($"  {proto.Key}: {proto.Value} packets");
                }
                
                Console.WriteLine("\nPress any key to return...");
                Console.ReadKey();
                Console.Clear();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error reading snapshot: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void OnSnapshotCaptured(object? sender, NetworkSnapshot snapshot)
        {
            _capturedSnapshotsCount++;
            
            Console.WriteLine($"Snapshot {_capturedSnapshotsCount} at {snapshot.TimeStamp}:");
            Console.WriteLine($"  Devices: {snapshot.Devices.Count}");
            Console.WriteLine($"  Connections: {snapshot.Connections.Count}");
            
            // Save snapshot to file for Unity to consume
            string json = snapshot.ToJson();
            try
            {
                Directory.CreateDirectory(_snapshotsDirectory);
                File.WriteAllText($"{_snapshotsDirectory}/snapshot_{snapshot.TimeStamp:yyyyMMdd_HHmmss}.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving snapshot: {ex.Message}");
            }
        }
    }
}