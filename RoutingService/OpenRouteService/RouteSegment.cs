using PolylineEncoder.Net.Models;

namespace RoutingService.OpenRouteService;

public class RouteSegment
{
    public string RoadName { get; set; } = "";
    public OrsClient.Vehicle Vehicle { get; set; }
    public string InstructionText { get; set; } = "";
    public Step.InstructionType InstructionType { get; set; }
    public double Distance { get; set; }
    public double Duration { get; set; }
    public IGeoCoordinate[] Points { get; set; } = [];
}
