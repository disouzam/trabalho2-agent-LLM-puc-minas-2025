using System.Text;

namespace ProcessoChat.LLM;

public class EmbeddingData
{
    public string Texto { get; set; }
    public List<double> Embedding { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var parteInicialDoTexto = Texto.Substring(0, 100);

        sb.AppendLine($"{ parteInicialDoTexto}[...]");

        var maxItemsToDisplay = 5;
        int count = 1;
        foreach (var item in Embedding)
        {
            sb.AppendLine($"Item {count}: {item.ToString()}");
            count++;
            if (count > maxItemsToDisplay)
            {
                break;
            }
        }

        return sb.ToString();
    }
}
