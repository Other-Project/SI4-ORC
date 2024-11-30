using System.Text.Json;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using GeoCoordinatePortable;
using RoutingService.ProxyCacheService;
using ISession = Apache.NMS.ISession;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class Service : IService
{
    public static double MaxWalkedDistance { get; set; }

    public async Task<string?> CalculateRoute(double startLon, double startLat, double endLon, double endLat)
    {
        try
        {
            var connecturi = new Uri("tcp://localhost:61616?wireFormat.maxInactivityDuration=0");
            var connectionFactory = new ConnectionFactory(connecturi);
            var connection = await connectionFactory.CreateConnectionAsync();
            await connection.StartAsync();
            var session = await connection.CreateSessionAsync();
            var name = "route--" + Guid.NewGuid();
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
                    await Task.Delay(2000);
                    await session.DeleteDestinationAsync(destination);

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

    private static async Task CalculateRoute(GeoCoordinate start, GeoCoordinate end, IMessageProducer producer, ISession session)
    {
        var proxyCacheClient = new ProxyCacheServiceClient();

        var startStation = (await proxyCacheClient.GetStationsAsync()).MinBy(s => start.GetDistanceTo(s.Position));
        if (startStation is null) return;
        var endStation = (await proxyCacheClient.GetStationsOfContractAsync(startStation.ContractName))?.MinBy(s => end.GetDistanceTo(s.Position));
        if (endStation is null) return;

        var straightDistance = start.GetDistanceTo(end);
        var walkedDistance = start.GetDistanceTo(startStation.Position) + end.GetDistanceTo(endStation.Position);

        IEnumerable<RouteSegment> route;
        if (walkedDistance > MaxWalkedDistance)
            route = await proxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.DrivingCar);
        else if (straightDistance <= walkedDistance)
            route = await proxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.FootWalking);
        else
            route = (await proxyCacheClient.GetRouteAsync(start.ToPosition(), startStation.Position, Vehicle.FootWalking))
                .Concat(await proxyCacheClient.GetRouteAsync(startStation.Position, endStation.Position, Vehicle.CyclingRegular))
                .Concat(await proxyCacheClient.GetRouteAsync(endStation.Position, end.ToPosition(), Vehicle.FootWalking));

        await AddRouteSegments(route, producer, session);
    }

    private static async Task AddRouteSegments(IEnumerable<RouteSegment> routeSegments, IMessageProducer producer,
        ISession session)
    {
        var compt = 0;
        foreach (var segment in routeSegments)
        {
            await producer.SendAsync(await session.CreateTextMessageAsync(JsonSerializer.Serialize(segment)));
            if (compt++ % 20 == 0)
                await Task.Delay(40);
        }
    }
}
