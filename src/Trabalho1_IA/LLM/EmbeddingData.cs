using System.Text;

namespace ProcessoChat.LLM;

public class EmbeddingData
{
    public string Texto { get; set; }
    public List<double> Embedding { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var maxComprimentoTexto = 100;

        var comprimento = Math.Min(maxComprimentoTexto, Texto.Length);
        var parteInicialDoTexto = Texto.Substring(0, comprimento);

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
