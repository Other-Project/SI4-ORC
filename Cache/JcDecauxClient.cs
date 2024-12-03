using System.Text.Json;
using Cache.JCDecaux;

namespace Cache;

public class JcDecauxClient : IObjectGetter<List<Station>>
{
    public static string ApiUrl { get; set; } = "https://api.jcdecaux.com/vls/v3";
    public static string? ApiKey { get; set; }

    private static readonly GenericProxyCache<List<Station>> StationCache = new(new JcDecauxClient());

    public static Task<List<Station>> GetStationsAsync(string contractName) => StationCache.GetAsync($"stations?contract={contractName}", 60 * 5); // stays 5 min in cache

    public static Task<List<Station>> GetStationsAsync() => StationCache.GetAsync($"stations?", 60 * 60 * 3); // stays 3h in cache

    public async Task<List<Station>> GetObjectAsync(HttpClient client, string itemName)
        => await JsonSerializer.DeserializeAsync<List<Station>>(await client.GetStreamAsync($"{ApiUrl}/{itemName}&apiKey={ApiKey}")) ?? [];
}
