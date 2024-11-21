using System.Text.Json.Serialization;

namespace RoutingService.OpenRouteService;

public class Step
{
    [JsonPropertyName("distance")] public double Distance { get; set; }
    [JsonPropertyName("duration")] public double Duration { get; set; }
    [JsonPropertyName("type")] public InstructionType Type { get; set; }
    [JsonPropertyName("instruction")] public string Instruction { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("way_points")] public int[] Waypoints { get; set; } = new int[2];

    public enum InstructionType
    {
        Left = 0,
        Right = 1,
        SharpLeft = 2,
        SharpRight = 3,
        SlightLeft = 4,
        SlightRight = 5,
        Straight = 6,
        EnterRoundabout = 7,
        ExitRoundabout = 8,
        UTurn = 9,
        Goal = 10,
        Depart = 11,
        KeepLeft = 12,
        KeepRight = 13,
    }
}
