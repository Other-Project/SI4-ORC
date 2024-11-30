using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Cache.JCDecaux;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
public class Contract
{
    /// <summary>Le nom du contrat (son identifiant)</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>Le nom commercial donné à ce contrat</summary>
    [JsonPropertyName("commercial_name")]
    public string CommercialName { get; set; } = null!;

    /// <summary>Le code (ISO 3166) du pays</summary>
    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = null!;

    /// <summary>La liste des villes associées au contrat</summary>
    [JsonPropertyName("cities")]
    public List<string> Cities { get; set; } = [];

    public override string ToString() => $"{Name} [{CountryCode}] ({CommercialName}) : {string.Join(", ", Cities)}";
}
