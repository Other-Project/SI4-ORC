using System.Text.Json;
using GeoCoordinatePortable;
using RoutingService.JCDecaux;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class Service : IService
{
    private static readonly HttpClient Client = new();
    private JcDeacauxClient JcDeacauxClient = new(Client);


    public async Task<string?> CalculateRoute(string from, string to)
    {
        try
        {
            await JcDeacauxClient.RetrieveContractsAsync();
            var contract = JcDeacauxClient.Contracts[11];
            await JcDeacauxClient.RetrieveStationsAsync(contract.Name);
            var station = JcDeacauxClient.Stations[0];
            var nearestStation = JcDeacauxClient.FindNearestStation(station);
            if (nearestStation is null) return null;
            return station.Name + " -> " + nearestStation.Name;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}