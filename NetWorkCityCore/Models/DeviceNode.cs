namespace NetworkCityCore.Models
{
    public class DeviceNode
    {
        public string Id { get; set; } = string.Empty; // IP or MAC address
        public string Name { get; set; } = string.Empty; // Hostname if available
        public string DeviceType { get; set; } = string.Empty; // Computer, phone, router, etc.
        public int TotalTraffic { get; set; }
        public Dictionary<string, int> ConnectionsCount { get; set; }
        public float[] Position { get; set; } // For stable positioning in 3D space [x,y,z]

        public DeviceNode()
        {
            ConnectionsCount = new Dictionary<string, int>();
            Position = new float[3] { 0, 0, 0 };
        }
    }
}