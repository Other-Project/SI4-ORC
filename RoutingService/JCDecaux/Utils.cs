using System.Text.Json;
using GeoCoordinatePortable;

namespace RoutingService.JCDecaux;

public class JcDeacauxClient(HttpClient client)
{
    private const string ApiUrl = "https://api.jcdecaux.com/vls/v3";
    private const string ApiKey = "4b2d445d6f950af8944565222f04b7acc9f79531";

    public List<Contract> Contracts { get; private set; }
    public List<Station> Stations { get; private set; }

    public async Task RetrieveContractsAsync()
    {
        Contracts = await JsonSerializer.DeserializeAsync<List<Contract>>(
            await client.GetStreamAsync($"{ApiUrl}/contracts?apiKey={ApiKey}")
        ) ?? [];
    }

    public async Task RetrieveStationsAsync(string contractName)
    {
        Stations = await JsonSerializer.DeserializeAsync<List<Station>>(
            await client.GetStreamAsync($"{ApiUrl}/stations?apiKey={ApiKey}&contract={contractName}")
        ) ?? [];
    }

    public Station? FindNearestStation(Station station)
    {
        GeoCoordinate stationPos = station.Position;
        return Stations
            .Where(s => s.Number != station.Number)
            .MinBy(s => stationPos.GetDistanceTo(s.Position));
    }
}