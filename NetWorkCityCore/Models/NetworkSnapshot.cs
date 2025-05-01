using Newtonsoft.Json;

namespace NetworkCityCore.Models
{
    public class NetworkSnapshot
    {
        public DateTime TimeStamp { get; set; }
        public List<DeviceNode> Devices { get; set; }
        public List<Connection> Connections { get; set; }
        public Dictionary<string, int> TrafficVolumes { get; set; }
        public Dictionary<string, int> ProtocolDistribution { get; set; }

        public NetworkSnapshot()
        {
            TimeStamp = DateTime.Now;
            Devices = new List<DeviceNode>();
            Connections = new List<Connection>();
            TrafficVolumes = new Dictionary<string, int>();
            ProtocolDistribution = new Dictionary<string, int>();
        }

        // Method to serialize to JSON for Unity consumption
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}