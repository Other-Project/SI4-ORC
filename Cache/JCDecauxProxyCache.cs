using Models.JCDecaux;

namespace Cache;

public class JCDecauxProxy : IObjectGetter<Station>
{
    public Task<Station> GetObjectAsync(string itemName)
    {
        throw new NotImplementedException();
    }
}
