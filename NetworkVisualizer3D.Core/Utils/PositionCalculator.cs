using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NetworkVisualizer3D.Core.Models;

namespace NetworkVisualizer3D.Core.Utils
{
    /// <summary>
    /// Utility class for calculating 3D positions for network devices in visualization
    /// </summary>
    public class PositionCalculator
    {
        private readonly Random _random;
        private const float GRID_SIZE = 100.0f;
        private const float MIN_DISTANCE = 5.0f;
        private const float SUBNET_SPACING = 50.0f;

        public PositionCalculator()
        {
            _random = new Random();
        }

        /// <summary>
        /// Calculates 3D position for a new device based on its IP address and existing devices
        /// </summary>
        /// <param name="ipAddress">IP address of the device</param>
        /// <param name="existingDevices">List of existing devices with positions</param>
        /// <returns>3D position for the device</returns>
        public Vector3D CalculatePosition(string ipAddress, List<NetworkDevice> existingDevices)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                return GenerateRandomPosition(existingDevices);
            }

            var bytes = ip.GetAddressBytes();
            if (bytes.Length != 4) // Only handle IPv4 for now
            {
                return GenerateRandomPosition(existingDevices);
            }

            // Calculate base position based on subnet
            var basePosition = CalculateSubnetPosition(bytes);
            
            // Adjust position to avoid collisions
            return AdjustForCollisions(basePosition, existingDevices);
        }

        /// <summary>
        /// Calculates positions for multiple devices using force-directed layout
        /// </summary>
        /// <param name="devices">List of devices to position</param>
        /// <param name="connections">List of connections between devices</param>
        /// <returns>Dictionary mapping device IDs to positions</returns>
        public Dictionary<string, Vector3D> CalculateForceDirectedLayout(
            List<NetworkDevice> devices, 
            List<NetworkConnection> connections)
        {
            var positions = new Dictionary<string, Vector3D>();
            
            // Initialize random positions
            foreach (var device in devices)
            {
                positions[device.Id] = new Vector3D(
                    (float)(_random.NextDouble() - 0.5) * GRID_SIZE,
                    (float)(_random.NextDouble() - 0.5) * GRID_SIZE,
                    (float)(_random.NextDouble() - 0.5) * GRID_SIZE
                );
            }

            // Apply force-directed algorithm
            const int iterations = 100;
            const float repulsionStrength = 1000.0f;
            const float attractionStrength = 0.1f;
            const float damping = 0.9f;

            var velocities = devices.ToDictionary(d => d.Id, d => new Vector3D(0, 0, 0));

            for (int iter = 0; iter < iterations; iter++)
            {
                var forces = devices.ToDictionary(d => d.Id, d => new Vector3D(0, 0, 0));

                // Calculate repulsion forces between all devices
                for (int i = 0; i < devices.Count; i++)
                {
                    for (int j = i + 1; j < devices.Count; j++)
                    {
                        var device1 = devices[i];
                        var device2 = devices[j];
                        var pos1 = positions[device1.Id];
                        var pos2 = positions[device2.Id];

                        var distance = pos1.Distance(pos2);
                        if (distance < 0.1f) distance = 0.1f; // Avoid division by zero

                        var direction = new Vector3D(
                            pos1.X - pos2.X,
                            pos1.Y - pos2.Y,
                            pos1.Z - pos2.Z
                        );

                        var magnitude = repulsionStrength / (distance * distance);
                        var force = new Vector3D(
                            direction.X / distance * magnitude,
                            direction.Y / distance * magnitude,
                            direction.Z / distance * magnitude
                        );

                        forces[device1.Id] = AddVectors(forces[device1.Id], force);
                        forces[device2.Id] = AddVectors(forces[device2.Id], new Vector3D(-force.X, -force.Y, -force.Z));
                    }
                }

                // Calculate attraction forces for connected devices
                foreach (var connection in connections)
                {
                    var device1 = devices.FirstOrDefault(d => d.IpAddress == connection.SourceIp);
                    var device2 = devices.FirstOrDefault(d => d.IpAddress == connection.DestinationIp);

                    if (device1 != null && device2 != null)
                    {
                        var pos1 = positions[device1.Id];
                        var pos2 = positions[device2.Id];

                        var distance = pos1.Distance(pos2);
                        var direction = new Vector3D(
                            pos2.X - pos1.X,
                            pos2.Y - pos1.Y,
                            pos2.Z - pos1.Z
                        );

                        var magnitude = attractionStrength * distance;
                        var force = new Vector3D(
                            direction.X / distance * magnitude,
                            direction.Y / distance * magnitude,
                            direction.Z / distance * magnitude
                        );

                        forces[device1.Id] = AddVectors(forces[device1.Id], force);
                        forces[device2.Id] = AddVectors(forces[device2.Id], new Vector3D(-force.X, -force.Y, -force.Z));
                    }
                }

                // Update velocities and positions
                foreach (var device in devices)
                {
                    velocities[device.Id] = AddVectors(
                        MultiplyVector(velocities[device.Id], damping),
                        forces[device.Id]
                    );

                    positions[device.Id] = AddVectors(positions[device.Id], velocities[device.Id]);

                    // Keep within bounds
                    var pos = positions[device.Id];
                    pos.X = Math.Max(-GRID_SIZE, Math.Min(GRID_SIZE, pos.X));
                    pos.Y = Math.Max(-GRID_SIZE, Math.Min(GRID_SIZE, pos.Y));
                    pos.Z = Math.Max(-GRID_SIZE, Math.Min(GRID_SIZE, pos.Z));
                    positions[device.Id] = pos;
                }
            }

            return positions;
        }

        /// <summary>
        /// Calculates hierarchical layout based on device types and network topology
        /// </summary>
        /// <param name="devices">List of devices to position</param>
        /// <returns>Dictionary mapping device IDs to positions</returns>
        public Dictionary<string, Vector3D> CalculateHierarchicalLayout(List<NetworkDevice> devices)
        {
            var positions = new Dictionary<string, Vector3D>();
            
            // Group devices by type
            var deviceGroups = devices.GroupBy(d => d.DeviceType).ToList();
            
            // Define hierarchy levels
            var hierarchy = new Dictionary<DeviceType, int>
            {
                { DeviceType.Router, 0 },
                { DeviceType.Switch, 1 },
                { DeviceType.AccessPoint, 1 },
                { DeviceType.Firewall, 1 },
                { DeviceType.Server, 2 },
                { DeviceType.Computer, 3 },
                { DeviceType.Printer, 3 },
                { DeviceType.MobilePhone, 4 },
                { DeviceType.Tablet, 4 },
                { DeviceType.IoTDevice, 4 },
                { DeviceType.Camera, 4 },
                { DeviceType.SmartTV, 4 },
                { DeviceType.GameConsole, 4 },
                { DeviceType.Unknown, 5 }
            };

            foreach (var group in deviceGroups)
            {
                var level = hierarchy.ContainsKey(group.Key) ? hierarchy[group.Key] : 5;
                var y = level * 20.0f; // Vertical spacing between levels
                
                var devicesInGroup = group.ToList();
                var angleStep = 2 * Math.PI / Math.Max(devicesInGroup.Count, 1);
                var radius = Math.Max(10.0f, devicesInGroup.Count * 2.0f);

                for (int i = 0; i < devicesInGroup.Count; i++)
                {
                    var angle = i * angleStep;
                    var x = (float)(radius * Math.Cos(angle));
                    var z = (float)(radius * Math.Sin(angle));
                    
                    positions[devicesInGroup[i].Id] = new Vector3D(x, y, z);
                }
            }

            return positions;
        }

        private Vector3D CalculateSubnetPosition(byte[] ipBytes)
        {
            // Use IP address to determine base position
            var x = (ipBytes[2] - 128) * SUBNET_SPACING / 128.0f;
            var z = (ipBytes[3] - 128) * SUBNET_SPACING / 128.0f;
            var y = 0.0f; // Keep devices on the same plane initially

            return new Vector3D(x, y, z);
        }

        private Vector3D AdjustForCollisions(Vector3D basePosition, List<NetworkDevice> existingDevices)
        {
            var position = new Vector3D(basePosition.X, basePosition.Y, basePosition.Z);
            var attempts = 0;
            const int maxAttempts = 50;

            while (attempts < maxAttempts)
            {
                bool collision = false;
                
                foreach (var device in existingDevices)
                {
                    if (device.Position.Distance(position) < MIN_DISTANCE)
                    {
                        collision = true;
                        break;
                    }
                }

                if (!collision)
                    break;

                // Adjust position
                var angle = _random.NextDouble() * 2 * Math.PI;
                var distance = MIN_DISTANCE + _random.NextDouble() * MIN_DISTANCE;
                
                position.X += (float)(Math.Cos(angle) * distance);
                position.Z += (float)(Math.Sin(angle) * distance);
                
                attempts++;
            }

            return position;
        }

        private Vector3D GenerateRandomPosition(List<NetworkDevice> existingDevices)
        {
            var position = new Vector3D(
                (float)(_random.NextDouble() - 0.5) * GRID_SIZE,
                (float)(_random.NextDouble() - 0.5) * 20.0f, // Smaller Y range
                (float)(_random.NextDouble() - 0.5) * GRID_SIZE
            );

            return AdjustForCollisions(position, existingDevices);
        }

        private Vector3D AddVectors(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        private Vector3D MultiplyVector(Vector3D vector, float scalar)
        {
            return new Vector3D(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        }
    }
} 