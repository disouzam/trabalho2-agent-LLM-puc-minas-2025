using System.Text;
using System.Text.Json;

using Microsoft.ML;

using ProcessoChat.LLM;

namespace ProcessoChat.Servicos;

public static class Embeddings
{
    public static async Task<List<double>> ObterEmbedding(string texto)
    {
        var payload = new { model = "text-embedding-3-small", input = texto };
        string jsonPayload = JsonSerializer.Serialize(payload);

        using var client = new ClientAPI().ObterClientAPI();
        var response = await client.PostAsync(ClientAPI.EmbeddingsUrl,
            new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        string responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseText);

        return result.Data.First().Embedding;
    }

    public static List<EmbeddingData> CarregarEmbeddings(string embeddingsFile)
    {
        if(File.Exists(embeddingsFile))
        {
        string json = File.ReadAllText(embeddingsFile);
        return JsonSerializer.Deserialize<List<EmbeddingData>>(json);
        }

        return new List<EmbeddingData>();
    }

    public static List<string> ObterChunksRelevantes(List<double> perguntaEmbedding, List<EmbeddingData> embeddingsData, int topN)
    {
        var mlContext = new MLContext();

        // (Dickson) Passo 3: Mesclar chunks do contexto RAG com chunks do contexto de memória do chat.

        return embeddingsData
            .Select(d => new { d.Texto, Similaridade = CalcularSimilaridade(perguntaEmbedding, d.Embedding) })
            .OrderByDescending(x => x.Similaridade)
            .Take(topN)
            .Select(x => x.Texto)
            .ToList();
    }

    private static double CalcularSimilaridade(List<double> v1, List<double> v2)
    {
        double dotProduct = v1.Zip(v2, (a, b) => a * b).Sum();
        double magnitude1 = (double)Math.Sqrt(v1.Sum(a => a * a));
        double magnitude2 = (double)Math.Sqrt(v2.Sum(b => b * b));

        return dotProduct / (magnitude1 * magnitude2);
    }

    public static async Task<List<EmbeddingData>> GerarEmbedding(List<string> listaDeChunks)
    {
        var embeddingsList = new List<EmbeddingData>();

        foreach(var chunk in listaDeChunks)
        {
            if(string.IsNullOrWhiteSpace(chunk)) continue;

            var embedding = await Embeddings.ObterEmbedding(chunk);

            embeddingsList.Add(new EmbeddingData { Texto = chunk, Embedding = embedding });
        }

        return embeddingsList;
    }

    public static List<string> LerArquivoTexto(string caminhoDoArquivo)
    {
        List<string> linhasDoArquivo = [];

        using(var sr = new StreamReader(caminhoDoArquivo))
        {
            string textoLinha;

            while((textoLinha = sr.ReadLine()) != null)
            {
                linhasDoArquivo.Add(textoLinha);
            }
        }

        return linhasDoArquivo;
    }

    public static void SalvarArquivoDeEmbeddings(List<EmbeddingData> embeddingsList,string arquivo )
    {
        // Salvar no JSON com Embedding de saida
        string jsonEmbeddings = JsonSerializer.Serialize(embeddingsList, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(arquivo, jsonEmbeddings);
    }
}
