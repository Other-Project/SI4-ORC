namespace Cache;

public class GenericProxyCache<T>(IObjectGetter<T> ObjectGetter)
{
    private HttpClient Client { get; } = new();
    private DateTimeOffset DtDefault { get; } = ObjectCache<T>.InfiniteAbsoluteExpiration;
    private Dictionary<string, ObjectCache<T>> Cache { get; } = new();

    public Task<T> GetAsync(string cacheItemName) => GetAsync(cacheItemName, DtDefault);

    public Task<T> GetAsync(string cacheItemName, double dtSeconds) => GetAsync(cacheItemName, DateTimeOffset.Now.Add(TimeSpan.FromSeconds(dtSeconds)));

    public async Task<T> GetAsync(string cacheItemName, DateTimeOffset dt)
    {
        if (Cache.TryGetValue(cacheItemName, out var objCache) && objCache.Expiry > DateTimeOffset.Now)
            return objCache.Value;
        var item = await ObjectGetter.GetObjectAsync(Client, cacheItemName);
        Cache[cacheItemName] = new ObjectCache<T>(item, dt);
        return item;
    }
}
internal record ObjectCache<T>(T Value, DateTimeOffset Expiry)
{
    public static DateTimeOffset InfiniteAbsoluteExpiration => DateTimeOffset.MaxValue;
}
public interface IObjectGetter<T>
{
    Task<T> GetObjectAsync(HttpClient client, string itemName);
}
