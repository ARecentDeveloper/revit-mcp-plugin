using Newtonsoft.Json;

namespace SampleCommandSet.Models
{
    /// <summary>
    /// Wall information structure used to return details of the created wall
    /// </summary>
    public class WallInfo
    {
        [JsonProperty("elementId")]
        public int ElementId { get; set; }
        [JsonProperty("startPoint")]
        public Point StartPoint { get; set; } = new Point();
        [JsonProperty("endPoint")]
        public Point EndPoint { get; set; } = new Point();
        [JsonProperty("height")]
        public double Height { get; set; }
        [JsonProperty("thickness")]
        public double Thickness { get; set; }
    }
}
