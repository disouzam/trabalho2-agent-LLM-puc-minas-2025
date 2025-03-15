namespace ProcessoChat.Chat;

public class ChatResponse
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<ChoiceResponse> choices { get; set; }
    public UsageResponse usage { get; set; }
    public string service_tier { get; set; }
    public string system_fingerprint { get; set; }
}
