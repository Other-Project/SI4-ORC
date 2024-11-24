using System.Text.Json.Serialization;
using PolylineEncoder.Net.Models;

namespace RoutingService.OpenRouteService;

public class RouteSegment
{
    [JsonPropertyName("RoadName")] public string RoadName { get; set; } = "";
    [JsonPropertyName("Vehicle")] public OrsClient.Vehicle Vehicle { get; set; }
    [JsonPropertyName("InstructionText")] public string InstructionText { get; set; } = "";
    [JsonPropertyName("InstructionType")] public Step.InstructionType InstructionType { get; set; }
    [JsonPropertyName("Distance")] public double Distance { get; set; }
    [JsonPropertyName("Duration")] public double Duration { get; set; }
    [JsonPropertyName("Points")] public IGeoCoordinate[] Points { get; set; } = [];
}