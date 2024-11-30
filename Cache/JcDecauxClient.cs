using System.Text.Json;
using Models.JCDecaux;

namespace Cache;

public class JcDecauxClient : IObjectGetter<List<Station>>
{
    public static string ApiUrl { get; set; } = "https://api.jcdecaux.com/vls/v3";
    public static string? ApiKey { get; set; }

    private static readonly GenericProxyCache<List<Station>> StationCache = new(new JcDecauxClient());

    public static Task<List<Station>> GetStationsAsync(string contractName) => StationCache.GetAsync($"stations?contract={contractName}");

    public static Task<List<Station>> GetStationsAsync() => StationCache.GetAsync($"stations?");

    public async Task<List<Station>> GetObjectAsync(HttpClient client, string itemName)
        => await JsonSerializer.DeserializeAsync<List<Station>>(await client.GetStreamAsync($"{ApiUrl}/{itemName}&apiKey={ApiKey}")) ?? [];
}
