using System.Text.Json;
using Models.JCDecaux;
using GeoCoordinate = GeoCoordinatePortable.GeoCoordinate;

namespace RoutingService;

public class JcDecauxClient(HttpClient client)
{
    public static string ApiUrl { get; set; } = "https://api.jcdecaux.com/vls/v3";
    public static string? ApiKey { get; set; }

    private async Task<List<Contract>> RetrieveContractsAsync() =>
        await JsonSerializer.DeserializeAsync<List<Contract>>(
            await client.GetStreamAsync($"{ApiUrl}/contracts?apiKey={ApiKey}")
        ) ?? [];

    private async Task<List<Station>> RetrieveStationsAsync(string contractName) =>
        await JsonSerializer.DeserializeAsync<List<Station>>(
            await client.GetStreamAsync($"{ApiUrl}/stations?apiKey={ApiKey}&contract={contractName}")
        ) ?? [];

    private async Task<List<Station>> RetrieveStationsAsync() =>
        await JsonSerializer.DeserializeAsync<List<Station>>(
            await client.GetStreamAsync($"{ApiUrl}/stations?apiKey={ApiKey}")) ?? [];

    public async Task<Station?> FindNearestStation(GeoCoordinate coordinate) =>
        (await RetrieveStationsAsync()).MinBy(s => coordinate.GetDistanceTo(s.Position));

    public async Task<Station?> FindNearestStation(GeoCoordinate coordinate, string contractName) =>
        (await RetrieveStationsAsync(contractName))?.MinBy(s => coordinate.GetDistanceTo(s.Position));
}
