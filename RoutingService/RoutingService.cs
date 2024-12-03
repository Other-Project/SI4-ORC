using System.Text.Json;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using GeoCoordinatePortable;
using RoutingService.ProxyCacheService;
using ISession = Apache.NMS.ISession;

namespace RoutingService;

// ReSharper disable once ClassNeverInstantiated.Global
public class RoutingService : IRoutingService
{
    public static Uri? ActiveMqUri { get; set; }
    public static double MaxWalkedDistance { get; set; }

    private bool shouldWait = true;

    private static readonly Random Random = new();

    private static readonly ProxyCacheServiceClient ProxyCacheClient = new();

    public async Task<(string receiveQueue, string sendQueue)> CalculateRoute(double startLon, double startLat, double endLon, double endLat)
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
                shouldWait = false;
            };
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

            _ = Task.Run(async () =>
            {
                try
                {
                    var start = new GeoCoordinate(startLat, startLon);
                    var end = new GeoCoordinate(endLat, endLon);
                    var startStation = (await ProxyCacheClient.GetStationsAsync())?.Where(StationHasBikes).MinBy(s => start.GetDistanceTo(s.Position));
                    var endStation = (await ProxyCacheClient.GetStationsAsync())?.Where(StationHasStands).MinBy(s => end.GetDistanceTo(s.Position));
                    if (startStation is null || endStation is null) return;
                    await CalculateRoute(start, end, startStation, endStation, producer, session);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    var message = await session.CreateTextMessageAsync(e.Message);
                    message.Properties.SetString("tag", "routing_error");
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
            return (null!, null!);
        }
    }

    private static bool StationHasBikes(Station station) => station is
        { Status: Station.StationStatus.OPEN, TotalStands.Availabilities.Bikes: > 0 };

    private static bool StationHasStands(Station station) => station is
        { Status: Station.StationStatus.OPEN, TotalStands.Availabilities.Stands: > 0 };

    private async Task CalculateRoute(GeoCoordinate start, GeoCoordinate end, Station startStation, Station endStation, IMessageProducer producer, ISession session, Vehicle vehicle = (Vehicle)(-1))
    {
        var straightDistance = start.GetDistanceTo(end);
        var walkedDistance = start.GetDistanceTo(startStation.Position) + end.GetDistanceTo(endStation.Position);

        IEnumerable<RouteSegment> route;
        if ((walkedDistance > MaxWalkedDistance && vehicle == (Vehicle)(-1)) || vehicle == Vehicle.DrivingCar)
            route = await ProxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.DrivingCar);
        else if (straightDistance <= walkedDistance)
            route = await ProxyCacheClient.GetRouteAsync(start.ToPosition(), end.ToPosition(), Vehicle.FootWalking);
        else
        {
            if (vehicle == Vehicle.CyclingRegular) route = [];
            else
                route = (await ProxyCacheClient.GetRouteAsync(start.ToPosition(), startStation.Position, Vehicle.FootWalking))
                    .Append(new RouteSegment
                    {
                        InstructionText = "Prenez un vélo",
                        InstructionType = InstructionType.ChangeVehicle,
                        Vehicle = Vehicle.FootWalking,
                        Points = []
                    });

            route = route.Concat(await ProxyCacheClient.GetRouteAsync(startStation.Position, endStation.Position, Vehicle.CyclingRegular))
                .Append(new RouteSegment
                {
                    InstructionText = "Déposez votre vélo",
                    InstructionType = InstructionType.ChangeVehicle,
                    Vehicle = Vehicle.CyclingRegular,
                    Points = []
                })
                .Concat(await ProxyCacheClient.GetRouteAsync(endStation.Position, end.ToPosition(), Vehicle.FootWalking));
        }

        await AddRouteSegments(route.ToList(), producer, session);
    }

    private async Task AddRouteSegments(IList<RouteSegment> routeSegments, IMessageProducer producer,
        ISession session)
    {
        var totalDistance = Math.Round(routeSegments.Sum(s => s.Distance));
        var totalDuration = Math.Round(routeSegments.Sum(s => s.Duration));
        await AddDistanceAndDuration(producer, session, totalDistance, totalDuration);
        var compt = 0;
        var problem = Random.Next() % 2 == 0;
        foreach (var segment in routeSegments)
        {
            var message = await session.CreateTextMessageAsync(JsonSerializer.Serialize(segment));
            message.Properties.SetString("tag", "instruction");
            await producer.SendAsync(message);
            if (compt++ % 10 != 0 || compt < 20) continue;
            while (shouldWait) await Task.Delay(500);

            if (problem && segment.Vehicle == Vehicle.CyclingRegular && Random.Next() % 2 == 0)
            {
                var distanceLeft = routeSegments.Skip(compt).Sum(s => s.Distance);
                var durationLeft = routeSegments.Skip(compt).Sum(s => s.Duration);
                await AddDistanceAndDuration(producer, session, -distanceLeft, -durationLeft);
                var lastSegment = routeSegments[^1];
                await RecalculateRoute(segment, lastSegment, producer, session);
                break;
            }
            shouldWait = true;
        }
    }

    private static async Task SendPopUp(IMessageProducer producer, ISession session, string textMessage)
    {
        var message = await session.CreateTextMessageAsync(JsonSerializer.Serialize(textMessage));
        message.Properties.SetString("tag", "route_change");
        await producer.SendAsync(message);
    }

    private async Task RecalculateRoute(RouteSegment currentSegment, RouteSegment lastSegment, IMessageProducer producer, ISession session)
    {
        if (currentSegment.Points.Length == 0) return;
        var start = new GeoCoordinate(currentSegment.Points[0].Latitude, currentSegment.Points[0].Longitude);
        var end = new GeoCoordinate(lastSegment.Points[^1].Latitude, lastSegment.Points[^1].Longitude);
        var startStation = (await ProxyCacheClient.GetStationsAsync())?.Where(StationHasBikes).MinBy(s => start.GetDistanceTo(s.Position));
        var endStation = (await ProxyCacheClient.GetStationsAsync())?.Where(StationHasStands).OrderBy(s => end.GetDistanceTo(s.Position)).ElementAt(1);
        if (startStation is null || endStation is null) return;
        await SendPopUp(producer, session, $"La station de destination n'as plus d'emplacement de rangement, déviation vers la station la plus proche:\n{endStation.Name}");
        await CalculateRoute(start, end, startStation, endStation, producer, session, currentSegment.Vehicle);
    }

    private static async Task AddDistanceAndDuration(IMessageProducer producer, ISession session, double distance, double duration)
    {
        var messageDistance = await session.CreateTextMessageAsync(JsonSerializer.Serialize(new { distance, duration }));
        messageDistance.Properties.SetString("tag", "infos");
        await producer.SendAsync(messageDistance);
    }
}
