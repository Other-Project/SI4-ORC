using Models.JCDecaux;
using Models.OpenRouteService;

namespace Cache;

[ServiceContract]
public interface IProxyCacheService
{
    [OperationContract] Task<List<Station>> GetStations();
    [OperationContract] Task<List<Station>> GetStationsOfContract(string contractName);
    [OperationContract] Task<List<RouteSegment>> GetRoute(Position start, Position end, Vehicle vehicle = Vehicle.CyclingRegular);
}
[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class ProxyCacheService : IProxyCacheService
{
    public Task<List<Station>> GetStations() => JcDecauxClient.GetStationsAsync();
    public Task<List<Station>> GetStationsOfContract(string contractName) => JcDecauxClient.GetStationsAsync(contractName);
    public Task<List<RouteSegment>> GetRoute(Position start, Position end, Vehicle vehicle = Vehicle.CyclingRegular) => OrsClient.GetRoute(start, end, vehicle);
}
