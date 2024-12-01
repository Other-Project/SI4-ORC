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

    private static Random _random = new Random();

    private static string[] _tags =
    [
        "De la pluie est prévue ", "Accident ", "Travaux ", "Bouchon", "Attention, vous allez trop vite !", "Probleme"
    ];

    public async Task<(string sendQueue, string receiveQueue)?> CalculateRoute(double startLon, double startLat,
        double endLon, double endLat, int index)
    {
        try
        {
            var connectionFactory = new ConnectionFactory(ActiveMqUri);
            var connection = await connectionFactory.CreateConnectionAsync();
            await connection.StartAsync();
            var session = await connection.CreateSessionAsync();
            var receiveQueueName = "route--" + Guid.NewGuid();
            var receiveQueue = await session.GetQueueAsync(receiveQueueName);
            var producer = await session.CreateProducerAsync(receiveQueue);

            var sendingQueueName = "route--" + Guid.NewGuid();
            var sendingQueue = await session.GetQueueAsync(sendingQueueName);
            var consumer = await session.CreateConsumerAsync(sendingQueue);
            consumer.Listener += message =>
            {
                if (message is not ITextMessage) return;
                _shouldWait = false;
            };
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

            _ = Task.Run(async () =>
            {
                try
                {
                    var start = new GeoCoordinate(startLat, startLon);
                    var end = new GeoCoordinate(endLat, endLon);
                    await CalculateRoute(start, end, producer, session, index);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
                finally
                {
                    await Task.Delay(2000);
                    await session.DeleteDestinationAsync(receiveQueue);
                    await session.DeleteDestinationAsync(sendingQueue);

                    // Don't forget to close your session and connection when finished.
                    await session.CloseAsync();
                    await connection.CloseAsync();
                    await consumer.CloseAsync();
                }
            });
            return (receiveQueue.QueueName, sendingQueue.QueueName);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return null;
        }
    }

    private static async Task CalculateRoute(GeoCoordinate start, GeoCoordinate end, IMessageProducer producer,
        ISession session, int index)
    {
        var proxyCacheClient = new ProxyCacheServiceClient();

        var startStation = (await proxyCacheClient.GetStationsAsync()).MinBy(s => start.GetDistanceTo(s.Position));
        if (startStation is null) return;
        var endStationList = (await proxyCacheClient.GetStationsOfContractAsync(startStation.ContractName))?
            .OrderBy(s =>
                end.GetDistanceTo(s.Position)).ToList();
        var endStation = endStationList?[index];

        //(await proxyCacheClient.GetStationsOfContractAsync(startStation.ContractName))?.MinBy(s =>end.GetDistanceTo(s.Position));
        if (endStation is null) return;

        var straightDistance = start.GetDistanceTo(end);
        var walkedDistance = start.GetDistanceTo(startStation.Position) + end.GetDistanceTo(endStation.Position);

        IEnumerable<RouteSegment> route;
        if (walkedDistance > MaxWalkedDistance)
            route = await proxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.DrivingCar);
        else if (straightDistance <= walkedDistance)
            route = await proxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.FootWalking);
        else
            route = (await proxyCacheClient.GetRouteAsync(start.ToPosition(), startStation.Position,
                    Vehicle.FootWalking))
                .Append(new RouteSegment
                {
                    InstructionText = "Prenez un vélo", InstructionType = StepInstructionType.ChangeVehicle,
                    Vehicle = Vehicle.FootWalking, Points = []
                })
                .Concat(await proxyCacheClient.GetRouteAsync(startStation.Position, endStation.Position,
                    Vehicle.CyclingRegular))
                .Append(new RouteSegment
                {
                    InstructionText = "Déposez votre vélo", InstructionType = StepInstructionType.ChangeVehicle,
                    Vehicle = Vehicle.CyclingRegular, Points = []
                })
                .Concat(
                    await proxyCacheClient.GetRouteAsync(endStation.Position, end.ToPosition(), Vehicle.FootWalking));

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
            }

            await TrySendPopUp(producer, session);
            _shouldWait = true;
        }
    }

    private static async Task TrySendPopUp(IMessageProducer producer, ISession session)
    {
        if (_random.Next(0, 2) == 0)
        {
            var value = _random.Next(0, _tags.Length);
            var messagePopUp = _tags[value]; // + (value <= 3 ? segment.RoadName : "");
            var message = await session.CreateTextMessageAsync(JsonSerializer.Serialize(messagePopUp));
            message.Properties.SetString("tag", "popup");
            await producer.SendAsync(message);
        }
    }
}