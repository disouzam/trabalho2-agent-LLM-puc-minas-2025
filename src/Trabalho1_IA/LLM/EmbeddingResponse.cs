using System.Text.Json.Serialization;

namespace ProcessoChat.LLM;

public class EmbeddingResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("data")]
    public List<Datum> Data { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; }
}