using System.Text.Json.Serialization;

namespace Models.JCDecaux;

public class Position
{
    [JsonPropertyName("latitude")] public double Latitude { get; set; }
    [JsonPropertyName("longitude")] public double Longitude { get; set; }
}
