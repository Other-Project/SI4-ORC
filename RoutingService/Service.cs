using RoutingService.JCDecaux;
using RoutingService.OpenRouteService;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class Service : IService
{
    private static readonly HttpClient Client = new();
    private readonly JcDecauxClient jcDecauxClient = new(Client);
    private readonly OrsClient orsClient = new(Client);


    public async Task<RouteSegment[]?> CalculateRoute(string from, string to)
    {
        try
        {
            await jcDecauxClient.RetrieveContractsAsync();
            var contract = jcDecauxClient.Contracts[11];
            await jcDecauxClient.RetrieveStationsAsync(contract.Name);
            var station = jcDecauxClient.Stations[0];
            var nearestStation = jcDecauxClient.FindNearestStation(station.Position);
            if (nearestStation is null) return null;
            var route = await orsClient.GetRoute(station.Position, nearestStation.Position);

            return route;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return null;
        }
    }
}
