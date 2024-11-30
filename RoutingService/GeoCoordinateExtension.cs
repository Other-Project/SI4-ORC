using GeoCoordinatePortable;
using RoutingService.ProxyCacheService;

namespace RoutingService;

public static class GeoCoordinateExtension
{
    public static double GetDistanceTo(this GeoCoordinate geoCoordinate, Position position) => geoCoordinate.GetDistanceTo(new GeoCoordinate(position.Latitude, position.Longitude));

    public static Position ToPosition(this GeoCoordinate geoCoordinate) => new() { Latitude = geoCoordinate.Latitude, Longitude = geoCoordinate.Longitude };
}
