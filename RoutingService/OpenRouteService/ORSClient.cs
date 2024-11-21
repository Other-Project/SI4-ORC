using System.Net.Http.Headers;
using System.Text.Json;
using GeoCoordinatePortable;
using Microsoft.OpenApi.Extensions;
using PolylineEncoder.Net.Utility.Decoders;

namespace RoutingService.OpenRouteService;

public class OrsClient(HttpClient client)
{
    public static string ApiUrl { get; set; } = "https://api.openrouteservice.org/v2";
    public static string? ApiKey { get; set; }


    public async Task<List<RouteSegment>> GetRoute(GeoCoordinate start, GeoCoordinate end, Vehicle vehicle = Vehicle.CyclingRegular)
    {
        if (ApiKey == null) throw new InvalidOperationException("API Key not set");

        /*https://api.openrouteservice.org/v2/directions/driving-car/json
        var url = $"{ApiUrl}/directions/{vehicle.GetDisplayName()}/json;" +
                  $"?api_key={ApiKey}&start={start.Longitude},{start.Latitude}&end={end.Longitude},{end.Latitude}";

        await client.PostAsJsonAsync($"{ApiUrl}/contracts?apiKey={ApiKey}", new
        {
            coordinates = new double[][] { [start.Longitude, start.Latitude], [end.Longitude, end.Latitude] },
            language = "fr",
            roundabout_exits = true
        });*/


        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{ApiUrl}/directions/{vehicle.GetAttributeOfType<EnumMemberAttribute>().Value}/json"),
            Method = HttpMethod.Post,
            Content = JsonContent.Create(new
            {
                coordinates = new double[][] { [start.Longitude, start.Latitude], [end.Longitude, end.Latitude] },
                language = "fr",
                roundabout_exits = true
            }),
            Headers =
            {
                Authorization = new AuthenticationHeaderValue(ApiKey)
            }
        };
        var response = await JsonDocument.ParseAsync(await (await client.SendAsync(request)).Content.ReadAsStreamAsync());
        var stepsElement = response.RootElement.GetProperty("routes")[0].GetProperty("segments")[0].GetProperty("steps");
        var waypointsElement = response.RootElement.GetProperty("routes")[0].GetProperty("geometry");
        var steps = stepsElement.EnumerateArray().Select(step => step.Deserialize<Step>()!).ToList();
        var waypoints = new Decoder().Decode(waypointsElement.GetString()).ToArray();

        var result = steps.Select(step => new RouteSegment
        {
            RoadName = step.Name,
            Vehicle = vehicle,
            InstructionText = step.Instruction,
            InstructionType = step.Type,
            Distance = step.Distance,
            Duration = step.Duration,
            Points = waypoints[new Range(step.Waypoints[0], step.Waypoints[1] + 1)]
        }).ToList();

        return result;
    }

    public enum Vehicle
    {
        [EnumMember(Value = "driving-car")] DrivingCar,
        [EnumMember(Value = "driving-hgv")] DrivingHgv,
        [EnumMember(Value = "cycling-regular")] CyclingRegular,
        [EnumMember(Value = "cycling-road")] CyclingRoad,
        [EnumMember(Value = "cycling-mountain")] CyclingMountain,
        [EnumMember(Value = "cycling-electric")] CyclingElectric,
        [EnumMember(Value = "foot-walking")] FootWalking,
        [EnumMember(Value = "foot-hiking")] FootHiking,
        [EnumMember(Value = "wheelchair")] Wheelchair
    }
}
