using System.Text.Json;
using GeoCoordinatePortable;
using RoutingService.JCDecaux;
using RoutingService.OpenRouteService;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using ISession = Apache.NMS.ISession;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class Service : IService
{
    private static readonly HttpClient Client = new();
    private readonly JcDecauxClient _jcDecauxClient = new(Client);
    private readonly OrsClient _orsClient = new(Client);

    public async Task<string?> CalculateRoute(double startLon, double startLat, double endLon,
        double endLat)
    {
        try
        {
            var connecturi = new Uri("tcp://localhost:61616?wireFormat.maxInactivityDuration=0");
            var connectionFactory = new ConnectionFactory(connecturi);
            var connection = await connectionFactory.CreateConnectionAsync();
            await connection.StartAsync();
            var session = await connection.CreateSessionAsync();
            var name = "route--" + System.Guid.NewGuid();
            var destination = await session.GetQueueAsync(name);
            var producer = await session.CreateProducerAsync(destination);
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

            _ = Task.Run(async () =>
            {
                try
                {
                    var start = new GeoCoordinate(startLat, startLon);
                    var end = new GeoCoordinate(endLat, endLon);
                    await CalculateRoute(start, end, producer, session);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
                finally
                {
                    // Don't forget to close your session and connection when finished.
                    await session.CloseAsync();
                    await connection.CloseAsync();
                }
            });
            return destination.QueueName;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return null;
        }
    }

    private async Task CalculateRoute(GeoCoordinate start, GeoCoordinate end, IMessageProducer producer,
        ISession session)
    {
        await _jcDecauxClient.RetrieveContractsAsync();
        await _jcDecauxClient.RetrieveStationsAsync();
        var startStation = _jcDecauxClient.FindNearestStation(start);
        if (startStation is null) return;
        var endStation = _jcDecauxClient.FindNearestStation(end);
        if (endStation is null) return;

        if (start.GetDistanceTo(end) <= start.GetDistanceTo(startStation.Position))
        {
            await AddRouteSegments(await _orsClient.GetRoute(start, end, OrsClient.Vehicle.FootWalking),producer, session);
            return;
        }

        await AddRouteSegments(await _orsClient.GetRoute(start, startStation.Position, OrsClient.Vehicle.FootWalking),
            producer, session);
        await AddRouteSegments(await _orsClient.GetRoute(startStation.Position, endStation.Position), producer,
            session);
        await AddRouteSegments(await _orsClient.GetRoute(endStation.Position, end, OrsClient.Vehicle.FootWalking),
            producer, session);
    }

    private static async Task AddRouteSegments(IEnumerable<RouteSegment> routeSegments, IMessageProducer producer,
        ISession session)
    {
        foreach (var segment in routeSegments)
            await producer.SendAsync(await session.CreateTextMessageAsync(JsonSerializer.Serialize(segment)));
    }
}