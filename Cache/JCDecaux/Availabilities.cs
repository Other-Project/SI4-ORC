using System.Text.Json.Serialization;

namespace Cache.JCDecaux;

public class Availabilities
{
    [JsonPropertyName("bikes")] public int Bikes { get; set; }
    [JsonPropertyName("stands")] public int Stands { get; set; }
    [JsonPropertyName("mechanicalBikes")] public int MechanicalBikes { get; set; }
    [JsonPropertyName("electricalBikes")] public int ElectricalBikes { get; set; }
    [JsonPropertyName("electricalInternalBatteryBikes")] public int ElectricalInternalBatteryBikes { get; set; }
    [JsonPropertyName("electricalRemovableBatteryBikes")] public int ElectricalRemovableBatteryBikes { get; set; }
}
