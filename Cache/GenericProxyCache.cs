namespace Cache;

public class GenericProxyCache<T>
{
    private HttpClient Client { get; } = new();
    private DateTimeOffset DtDefault { get; } = ObjectCache<T>.InfiniteAbsoluteExpiration;
    private Dictionary<string, ObjectCache<T>> Cache { get; } = new();

    private IObjectGetter<T> ObjectGetter { get; set; }

    public T? Get(string cacheItemName) => Get(cacheItemName, DtDefault);

    public T? Get(string cacheItemName, double dtSeconds) => Get(cacheItemName,
        new DateTimeOffset(DateTime.Now, TimeSpan.FromSeconds(dtSeconds)));

    public T? Get(string cacheItemName, DateTimeOffset dt)
    {
        if (Cache.TryGetValue(cacheItemName, out var objCache) && objCache.Expiry > DateTimeOffset.Now)
            return objCache.Value;
        var item = ObjectGetter.GetObject(cacheItemName);
        Cache[cacheItemName] = new ObjectCache<T>(item, dt);
        return item;
    }
}

internal record ObjectCache<T>(T? Value, DateTimeOffset Expiry)
{
    public static DateTimeOffset InfiniteAbsoluteExpiration => DateTimeOffset.MaxValue;
}

public interface IObjectGetter<out T>
{
    T GetObject(string itemName);
}