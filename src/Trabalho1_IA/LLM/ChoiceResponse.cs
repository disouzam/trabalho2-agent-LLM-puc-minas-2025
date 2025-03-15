namespace ProcessoChat.LLM;

public class ChoiceResponse
{
    public int index { get; set; }
    public Message message { get; set; }
    public object logprobs { get; set; }
    public string finish_reason { get; set; }
}