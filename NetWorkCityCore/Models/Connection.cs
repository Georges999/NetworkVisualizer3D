namespace NetworkCityCore.Models
{
    public class Connection
    {
        public string SourceId { get; set; } = string.Empty;
        public string DestinationId { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public int Volume { get; set; }
        public bool IsTwoWay { get; set; }
        
        // Additional property for Unity visualization
        public DateTime LastActivity { get; set; }
    }
}