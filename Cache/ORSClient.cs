using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.OpenApi.Extensions;
using ORC.Models;
using PolylineEncoder.Net.Utility.Decoders;

namespace Cache;

public class OrsClient : IObjectGetter<List<RouteSegment>>
{
    public static string ApiUrl { get; set; } = "https://api.openrouteservice.org/v2";
    public static string? ApiKey { get; set; }

    private static readonly GenericProxyCache<List<RouteSegment>> RouteSegmentCache = new(new OrsClient());
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { IncludeFields = true };

    public static async Task<List<RouteSegment>> GetRoute(Position start, Position end, Vehicle vehicle = Vehicle.CyclingRegular)
        => await RouteSegmentCache.GetAsync(JsonSerializer.Serialize((start, end, vehicle), JsonSerializerOptions));

    public async Task<List<RouteSegment>> GetObjectAsync(HttpClient client, string itemName)
    {
        var (start, end, vehicle) = JsonSerializer.Deserialize<(Position, Position, Vehicle)>(itemName, JsonSerializerOptions);
        if (ApiKey == null) throw new InvalidOperationException("API Key not set");

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
        if (response.RootElement.TryGetProperty("error", out var error))
            throw new HttpRequestException(error.GetProperty("message").GetString());
        var stepsElement = response.RootElement.GetProperty("routes")[0].GetProperty("segments")[0].GetProperty("steps");
        var waypointsElement = response.RootElement.GetProperty("routes")[0].GetProperty("geometry");
        var steps = stepsElement.EnumerateArray().Select(step => step.Deserialize<Step>()!).ToList();
        var waypoints = new Decoder()
            .Decode(waypointsElement.GetString())
            .Select(w => new Position { Longitude = w.Longitude, Latitude = w.Latitude })
            .ToArray();

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
}
