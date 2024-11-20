using System.Diagnostics.CodeAnalysis;
using CoreWCF.Web;
using RoutingService.OpenRouteService;

namespace RoutingService;

[ServiceContract]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IService
{
    [OperationContract]
    [WebInvoke(UriTemplate = "route?from={from}&to={to}",
        Method = "GET",
        BodyStyle = WebMessageBodyStyle.Wrapped,
        RequestFormat = WebMessageFormat.Json,
        ResponseFormat = WebMessageFormat.Json)]
    Task<RouteSegment[]?> CalculateRoute(string from, string to);
}
