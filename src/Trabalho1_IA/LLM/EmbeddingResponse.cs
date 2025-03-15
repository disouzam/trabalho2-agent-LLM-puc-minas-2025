namespace ProcessoChat.LLM;

public class EmbeddingResponse
{
    public string @object { get; set; }
    public List<Datum> data { get; set; }
    public string model { get; set; }
    public Usage usage { get; set; }
}