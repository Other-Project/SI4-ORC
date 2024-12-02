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

    private static readonly Random Random = new Random();

    private static readonly string[] Tags =
    [
        "Pluie est prévue sur votre trajet ", "Accident dans 200m, veuillez faire attention",
        "Travaux avenue rue de la savoie\nDéviation route des champs", "Bouchons sur votre trajet",
        "Attention, vous allez trop vite !", "Station d'arrivée pleine, le trajet a été recalculé"
    ];

    private static readonly ProxyCacheServiceClient ProxyCacheClient = new();

    private static bool _hasAlreadyProblem;

    public async Task<(string sendQueue, string receiveQueue)?> CalculateRoute(double startLon, double startLat,
        double endLon, double endLat, int index)
    {
        try
        {
            _hasAlreadyProblem = false;
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
                    var startStation = await GetClosestStation(start, 0);
                    var endStation = await GetClosestStation(end, 0);
                    if (startStation is null || endStation is null) return;
                    await CalculateRoute(start, end, startStation, endStation, producer, session, Vehicle.FootWalking);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    var message = await session.CreateTextMessageAsync(e.Message);
                    message.Properties.SetString("tag", "routing_error"); // TODO: Waiting for Alban's changes
                    await producer.SendAsync(message);
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

    private static bool StationHasBikes(Station station) => station is
        { Status: Station.StationStatus.OPEN, TotalStands.Availabilities.Bikes: > 0 };

    private static bool StationHasStands(Station station) => station is
        { Status: Station.StationStatus.OPEN, TotalStands.Availabilities.Stands: > 0 };

    private static async Task<Station?> GetClosestStation(GeoCoordinate pos, int index)
    {
        return (await ProxyCacheClient.GetStationsAsync())?.Where(StationHasBikes)
            .OrderBy(s => pos.GetDistanceTo(s.Position))
            .ElementAt(index);
    }

    private static async Task CalculateRoute(GeoCoordinate start, GeoCoordinate end, Station startStation,
        Station endStation, IMessageProducer producer,
        ISession session, Vehicle vehicle)
    {
        var straightDistance = start.GetDistanceTo(end);
        var walkedDistance = start.GetDistanceTo(startStation.Position) + end.GetDistanceTo(endStation.Position);

        IEnumerable<RouteSegment> route;
        if ((walkedDistance > MaxWalkedDistance && !_hasAlreadyProblem) || vehicle == Vehicle.DrivingCar)
            route = await ProxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.DrivingCar);
        else if (straightDistance <= walkedDistance)
            route = await ProxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.FootWalking);
        else if (vehicle == Vehicle.FootWalking)
            route = (await ProxyCacheClient.GetRouteAsync(start.ToPosition(), startStation.Position,
                    Vehicle.FootWalking))
                .Append(new RouteSegment
                {
                    InstructionText = "Prenez un vélo", InstructionType = StepInstructionType.ChangeVehicle,
                    Vehicle = Vehicle.FootWalking, Points = []
                })
                .Concat(await ProxyCacheClient.GetRouteAsync(startStation.Position, endStation.Position,
                    Vehicle.CyclingRegular))
                .Append(new RouteSegment
                {
                    InstructionText = "Déposez votre vélo", InstructionType = StepInstructionType.ChangeVehicle,
                    Vehicle = Vehicle.CyclingRegular, Points = []
                })
                .Concat(
                    await ProxyCacheClient.GetRouteAsync(endStation.Position, end.ToPosition(), Vehicle.FootWalking));
        else
            route = (await ProxyCacheClient.GetRouteAsync(start.ToPosition(), endStation.Position,
                    Vehicle.CyclingRegular))
                .Append(new RouteSegment
                {
                    InstructionText = "Déposez votre vélo", InstructionType = StepInstructionType.ChangeVehicle,
                    Vehicle = Vehicle.CyclingRegular, Points = []
                })
                .Concat(
                    await ProxyCacheClient.GetRouteAsync(endStation.Position, end.ToPosition(), Vehicle.FootWalking));
        await AddRouteSegments(route, producer, session);
    }

    private static async Task AddRouteSegments(IEnumerable<RouteSegment> routeSegments, IMessageProducer producer,
        ISession session)
    {
        var compt = 1;
        foreach (var segment in routeSegments)
        {
            var message = await session.CreateTextMessageAsync(JsonSerializer.Serialize(segment));
            message.Properties.SetString("tag", "instruction");
            await producer.SendAsync(message);
            if (compt++ % 10 != 0) continue;
            while (_shouldWait)
            {
                await Task.Delay(500);
            }

            var problem = await TrySendPopUp(producer, session);
            if (problem && !_hasAlreadyProblem)
            {
                _hasAlreadyProblem = true;
                var lastSegment = routeSegments.Last();
                await RecalculateRoute(segment, lastSegment, producer, session);
                return;
            }

            _shouldWait = true;
        }
    }

    private static async Task<bool> TrySendPopUp(IMessageProducer producer, ISession session)
    {
        if (Random.Next(0, 5) == 0)
        {
            var value = Random.Next(0, Tags.Length);// To have the other problem
            var messagePopUp = Tags[value];
            var message = await session.CreateTextMessageAsync(JsonSerializer.Serialize(messagePopUp));
            message.Properties.SetString("tag", value.ToString());
            await producer.SendAsync(message);
            if (value == 5)
            {
                return true;
            }
        }
        return false;
    }

    private static async Task RecalculateRoute(RouteSegment currentSegment, RouteSegment lastSegment,
        IMessageProducer producer, ISession session)
    {
        var start = new GeoCoordinate(currentSegment.Points.First().Latitude, currentSegment.Points.First().Longitude);
        var end = new GeoCoordinate(lastSegment.Points.Last().Latitude, lastSegment.Points.Last().Longitude);
        var startStation = await GetClosestStation(start, 0);
        var endStation = await GetClosestStation(end, 1);
        if (startStation is null || endStation is null) return;
        await CalculateRoute(start, end, startStation, endStation, producer, session, currentSegment.Vehicle);
    }
}