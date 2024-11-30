namespace Cache;

[ServiceContract]
public interface IService
{
    [OperationContract]
    string GetData(int value);
}

public class Service : IService
{
    public string GetData(int value)
    {
        return string.Format("You entered: {0}", value);
    }
}