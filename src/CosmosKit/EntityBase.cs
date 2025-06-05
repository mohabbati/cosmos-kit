using Newtonsoft.Json;

namespace CosmosKit;

public abstract class EntityBase
{
    [JsonProperty("id")]
    public string Id { get; set; } = default!;
}