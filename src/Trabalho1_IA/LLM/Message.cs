using System.Text.Json.Serialization;

namespace ProcessoChat.LLM;

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("function_call")]
    public FunctionCall FunctionCall { get; set; }

    [JsonPropertyName("refusal")]
    public object Refusal { get; set; }
}