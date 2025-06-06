using System.Text.Json.Serialization;

namespace CosmosKit;

public abstract class EntityBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;
}