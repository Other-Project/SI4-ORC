using System.Text.Json;
using GeoCoordinatePortable;

namespace RoutingService.JCDecaux;

public class JcDeacauxClient(HttpClient client)
{
    public static string ApiUrl { get; set; } = "https://api.jcdecaux.com/vls/v3";
    public static string? ApiKey { get; set; }

    public List<Contract>? Contracts { get; private set; }
    public List<Station>? Stations { get; private set; }

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
        return Stations?
            .Where(s => s.Number != station.Number)
            .MinBy(s => stationPos.GetDistanceTo(s.Position));
    }
}