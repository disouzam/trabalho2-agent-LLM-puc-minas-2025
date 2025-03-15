namespace ProcessoChat.LLM;

public class Datum
{
    public string @object { get; set; }
    public int index { get; set; }
    public List<double> embedding { get; set; }
}
