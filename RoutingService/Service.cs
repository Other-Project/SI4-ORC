using GeoCoordinatePortable;
using RoutingService.JCDecaux;
using RoutingService.OpenRouteService;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class Service : IService
{
    private static readonly HttpClient Client = new();
    private readonly JcDecauxClient jcDecauxClient = new(Client);
    private readonly OrsClient orsClient = new(Client);


    public async Task<IEnumerable<RouteSegment>?> CalculateRoute(double startLon, double startLat, double endLon, double endLat)
    {
        try
        {
            var start = new GeoCoordinate(startLat, startLon);
            var end = new GeoCoordinate(endLat, endLon);

            await jcDecauxClient.RetrieveContractsAsync();
            await jcDecauxClient.RetrieveStationsAsync();
            var startStation = jcDecauxClient.FindNearestStation(start);
            if (startStation is null) return null;
            var endStation = jcDecauxClient.FindNearestStation(end);
            if (endStation is null) return null;

            var route = await orsClient.GetRoute(start, startStation.Position, OrsClient.Vehicle.FootWalking);
            route.AddRange(await orsClient.GetRoute(startStation.Position, endStation.Position));
            route.AddRange(await orsClient.GetRoute(endStation.Position, end, OrsClient.Vehicle.FootWalking));
            return route;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return null;
        }
    }
}
