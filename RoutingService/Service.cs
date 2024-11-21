using GeoCoordinatePortable;
using RoutingService.JCDecaux;
using RoutingService.OpenRouteService;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class Service : IService
{
    private static readonly HttpClient Client = new();
    private readonly JcDecauxClient _jcDecauxClient = new(Client);
    private readonly OrsClient _orsClient = new(Client);


    public async Task<IEnumerable<RouteSegment>?> CalculateRoute(double startLon, double startLat, double endLon,
        double endLat)
    {
        try
        {
            var start = new GeoCoordinate(startLat, startLon);
            var end = new GeoCoordinate(endLat, endLon);

            await _jcDecauxClient.RetrieveContractsAsync();
            await _jcDecauxClient.RetrieveStationsAsync();
            var startStation = _jcDecauxClient.FindNearestStation(start);
            if (startStation is null) return null;
            var endStation = _jcDecauxClient.FindNearestStation(end);
            if (endStation is null) return null;

            if (start.GetDistanceTo(end) <= start.GetDistanceTo(startStation.Position))
                return await _orsClient.GetRoute(start, end, OrsClient.Vehicle.FootWalking);

            var route = await _orsClient.GetRoute(start, startStation.Position, OrsClient.Vehicle.FootWalking);
            route.AddRange(await _orsClient.GetRoute(startStation.Position, endStation.Position));
            route.AddRange(await _orsClient.GetRoute(endStation.Position, end, OrsClient.Vehicle.FootWalking));
            return route;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return null;
        }
    }
}