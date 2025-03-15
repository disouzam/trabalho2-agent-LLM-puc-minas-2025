using System.Text.Json.Serialization;

namespace ProcessoChat.LLM;

public class ChoiceResponse
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]    
    public Message Message { get; set; }

    [JsonPropertyName("logprobs")]
    public object LogProbs { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}