using System.Text.Json.Serialization;

namespace RoutingService.JCDecaux;

public class Stands
{
    [JsonPropertyName("availabilities")] public Availabilities? Availabilities { get; set; }
    [JsonPropertyName("capacity")] public int Capacity { get; set; }
}