using System.Diagnostics.CodeAnalysis;
using CoreWCF.Web;

namespace RoutingService;

[ServiceContract]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IRoutingService
{
    [OperationContract]
    [WebInvoke(UriTemplate = "route?startLon={startLon}&startLat={startLat}&endLon={endLon}&endLat={endLat}",
        Method = "GET",
        BodyStyle = WebMessageBodyStyle.Wrapped,
        RequestFormat = WebMessageFormat.Json,
        ResponseFormat = WebMessageFormat.Json)]
    Task<(string receiveQueue, string sendQueue)> CalculateRoute(double startLon, double startLat, double endLon, double endLat);
}
