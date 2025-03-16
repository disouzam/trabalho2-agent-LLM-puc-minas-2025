using System.Text.Json.Serialization;

namespace ProcessoChat.LLM;

public class UsageResponse
{
    [JsonPropertyName("prompt_tokens")]
    public int prompt_tokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int completion_tokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int total_tokens { get; set; }

    [JsonPropertyName("prompt_tokens_details")]
    public PromptTokensDetails prompt_tokens_details { get; set; }

    [JsonPropertyName("completion_tokens_details")]
    public CompletionTokensDetails completion_tokens_details { get; set; }

    [JsonPropertyName("contextoAtualizado")]
    public bool contextoAtualizado { get; set; }
}