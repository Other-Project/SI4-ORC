using System.Text.Json.Serialization;
using Cache.OpenRouteService;

namespace ORC.Models;

public class Step
{
    [JsonPropertyName("distance")] public double Distance { get; set; }
    [JsonPropertyName("duration")] public double Duration { get; set; }
    [JsonPropertyName("type")] public InstructionType Type { get; set; }
    [JsonPropertyName("instruction")] public string Instruction { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("way_points")] public int[] Waypoints { get; set; } = new int[2];
}
