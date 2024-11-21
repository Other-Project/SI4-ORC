using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace RoutingService.JCDecaux;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
public class Station
{
    #region Static

    /// <summary>Le numéro de la station.</summary>
    /// <remarks>Attention, ce n'est pas un id, ce numéro n'est unique qu'au sein d'un contrat</remarks>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>Le nom du contrat de cette station</summary>
    [JsonPropertyName("contractName")]
    public string? ContractName { get; set; }

    /// <summary>Le nom de la station</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Adresse indicative de la station</summary>
    /// <remarks>Les données étant brutes, parfois il s'agit plus d'un commentaire que d'une adresse.</remarks>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>Les coordonnées au format WGS84</summary>
    [JsonPropertyName("position")]
    public Position Position { get; set; } = null!;

    /// <summary>Indique la présence d'un terminal de paiement</summary>
    [JsonPropertyName("banking")]
    public bool Banking { get; set; }

    /// <summary>Indique s'il s'agit d'une station bonus</summary>
    [JsonPropertyName("bonus")]
    public bool Bonus { get; set; }

    /// <summary>Indique si la station accepte le repose de vélos en overflow</summary>
    [JsonPropertyName("overflow")]
    public bool Overflow { get; set; }

    /// <summary>Non utilisé pour l'instant</summary>
    [JsonPropertyName("shape")]
    public object? Shape { get; set; }

    #endregion

    #region Dynamic

    /// <summary>Indique l'état de la station, peut être CLOSED ou OPEN</summary>
    [JsonPropertyName("status"), JsonConverter(typeof(JsonStringEnumConverter))]
    public StationStatus Status { get; set; }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum StationStatus
    {
        CLOSED,
        OPEN
    }

    /// <summary>Timestamp indiquant le moment de la dernière mise à jour</summary>
    [JsonPropertyName("lastUpdate")]
    public string? LastUpdate { get; set; }

    /// <summary>Indique si la station est connectée au système central</summary>
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    /// <summary>Indique la capacité totale de la station</summary>
    [JsonPropertyName("totalStands")]
    public Stands? TotalStands { get; set; }

    /// <summary>Indique la capacité physique de la station</summary>
    [JsonPropertyName("mainStands")]
    public Stands? MainStands { get; set; }

    /// <summary>Indique la capacité overflow de la station</summary>
    [JsonPropertyName("overflowStands")]
    public Stands? OverflowStands { get; set; }

    #endregion


    private bool Equals(Station other)
    {
        return Number == other.Number && ContractName == other.ContractName;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Station)obj);
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode() => HashCode.Combine(Number, ContractName);

    public static bool operator ==(Station left, Station? right) => Equals(left, right);
    public static bool operator !=(Station left, Station right) => !Equals(left, right);

    public override string? ToString() => Name;
}