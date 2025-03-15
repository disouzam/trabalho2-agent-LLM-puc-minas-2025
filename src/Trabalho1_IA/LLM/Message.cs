namespace ProcessoChat.LLM;

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
    public function_call function_call { get; set; }
    public object refusal { get; set; }
}