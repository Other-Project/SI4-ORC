using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GeoCoordinatePortable;
using Microsoft.OpenApi.Extensions;

namespace RoutingService.OpenRouteService;

public class OrsClient
{
    public static string ApiUrl { get; set; } = "https://api.openrouteservice.org/v2";
    public static string? ApiKey { get; set; }


    public static string? GetRoute(GeoCoordinate start, GeoCoordinate end, Vehicle vehicle = Vehicle.CyclingRegular)
    {
        var url = $"{ApiUrl}/directions/{vehicle.GetDisplayName()}?api_key={ApiKey}&start={start.Longitude},{start.Latitude}&end={end.Longitude},{end.Latitude}";

        return null;
    }

    public enum Vehicle
    {
        [Display(Name = "driving-car")] DrivingCar,
        [Display(Name = "driving-hgv")] DrivingHgv,
        [Display(Name = "cycling-regular")] CyclingRegular,
        [Display(Name = "cycling-road")] CyclingRoad,
        [Display(Name = "cycling-mountain")] CyclingMountain,
        [Display(Name = "cycling-electric")] CyclingElectric,
        [Display(Name = "foot-walking")] FootWalking,
        [Display(Name = "foot-hiking")] FootHiking,
        [Display(Name = "wheelchair")] Wheelchair
    }
}
