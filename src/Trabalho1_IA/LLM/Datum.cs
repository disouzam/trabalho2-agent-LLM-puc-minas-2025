using System.Text.Json.Serialization;

namespace ProcessoChat.LLM;

public class Datum
{
    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("embedding")]
    public List<double> Embedding { get; set; }
}
