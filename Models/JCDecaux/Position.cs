using System.Text.Json.Serialization;
using GeoCoordinate = GeoCoordinatePortable.GeoCoordinate;

namespace Models.JCDecaux;

public class Position
{
    [JsonPropertyName("latitude")] public double Latitude { get; set; }
    [JsonPropertyName("longitude")] public double Longitude { get; set; }

    public static implicit operator GeoCoordinate(Position position) => new(position.Latitude, position.Longitude);
}
