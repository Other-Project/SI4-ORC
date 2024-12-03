using Cache.OpenRouteService;

namespace ORC.Models;

public class RouteSegment
{
    public string RoadName { get; set; } = "";
    public Vehicle Vehicle { get; set; }
    public string InstructionText { get; set; } = "";
    public InstructionType InstructionType { get; set; }
    public double Distance { get; set; }
    public double Duration { get; set; }
    public Position[] Points { get; set; } = [];
}
