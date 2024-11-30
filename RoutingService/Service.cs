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
    public static Uri? ActiveMqUri { get; set; }
    public static double MaxWalkedDistance { get; set; }

    private static bool _shouldWait = true;

    public async Task<(string sendQueue, string receiveQueue)?> CalculateRoute(double startLon, double startLat,
        double endLon,
        double endLat)
    {
        try
        {
            var connectionFactory = new ConnectionFactory(ActiveMqUri);
            var connection = await connectionFactory.CreateConnectionAsync();
            await connection.StartAsync();
            var session = await connection.CreateSessionAsync();
            var name = "route--" + Guid.NewGuid();
            var destination = await session.GetQueueAsync(name);
            var producer = await session.CreateProducerAsync(destination);

            var nameQueue2 = "route--" + Guid.NewGuid();
            var destination2 = await session.GetQueueAsync(nameQueue2);
            var consumer = await session.CreateConsumerAsync(destination2);
            consumer.Listener += message =>
            {
                if (message is not ITextMessage) return;
                _shouldWait = false;
                Console.WriteLine("I'm in the listener");
                Console.WriteLine(message);
            };
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
            return (destination.QueueName, destination2.QueueName);
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
                .Append(new RouteSegment { InstructionText = "Prenez un vélo", InstructionType = StepInstructionType.ChangeVehicle, Vehicle = Vehicle.FootWalking, Points = [] })
                .Concat(await proxyCacheClient.GetRouteAsync(startStation.Position, endStation.Position, Vehicle.CyclingRegular))
                .Append(new RouteSegment { InstructionText = "Déposez votre vélo", InstructionType = StepInstructionType.ChangeVehicle, Vehicle = Vehicle.CyclingRegular, Points = [] })
                .Concat(await proxyCacheClient.GetRouteAsync(endStation.Position, end.ToPosition(), Vehicle.FootWalking));

        await AddRouteSegments(route, producer, session);
    }

    private static async Task AddRouteSegments(IEnumerable<RouteSegment> routeSegments, IMessageProducer producer,
        ISession session)
    {
        var compt = 1;
        foreach (var segment in routeSegments)
        {
            await producer.SendAsync(await session.CreateTextMessageAsync(JsonSerializer.Serialize(segment)));
            if (compt++ % 10 != 0) continue;
            while (_shouldWait)
            {
                await Task.Delay(500);
                //Console.WriteLine("In the while loop");
            }

            _shouldWait = true;
        }
    }
}
