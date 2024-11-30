using System.Runtime.Serialization;

namespace Models.OpenRouteService;

public enum Vehicle
{
    [EnumMember(Value = "driving-car")] DrivingCar,
    ///<summary>Heavy goods vehicle</summary>
    [EnumMember(Value = "driving-hgv")] DrivingHgv,
    [EnumMember(Value = "cycling-regular")] CyclingRegular,
    [EnumMember(Value = "cycling-road")] CyclingRoad,
    [EnumMember(Value = "cycling-mountain")] CyclingMountain,
    [EnumMember(Value = "cycling-electric")] CyclingElectric,
    [EnumMember(Value = "foot-walking")] FootWalking,
    [EnumMember(Value = "foot-hiking")] FootHiking,
    [EnumMember(Value = "wheelchair")] Wheelchair
}
