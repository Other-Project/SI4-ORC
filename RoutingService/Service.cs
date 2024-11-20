using System.Text.Json;
using GeoCoordinatePortable;
using RoutingService.JCDecaux;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class Service : IService
{
    private static readonly HttpClient Client = new();
    private readonly JcDeacauxClient jcDeacauxClient = new(Client);


    public async Task<string?> CalculateRoute(string from, string to)
    {
        try
        {
            await jcDeacauxClient.RetrieveContractsAsync();
            var contract = jcDeacauxClient.Contracts[11];
            await jcDeacauxClient.RetrieveStationsAsync(contract.Name);
            var station = jcDeacauxClient.Stations[0];
            var nearestStation = jcDeacauxClient.FindNearestStation(station);
            if (nearestStation is null) return null;
            return station.Name + " -> " + nearestStation.Name;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return e.Message;
        }
    }
}