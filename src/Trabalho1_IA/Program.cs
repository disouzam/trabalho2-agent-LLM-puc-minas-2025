using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;

class Program
{
    private static readonly string OpenAiApiKey = "{insira sua chave aqui}"; // Substitua pela sua chave de API OpenAI
    private static readonly string OpenAiEndpoint = "https://api.openai.com/v1/chat/completions";
    private static readonly string ContextDados = "dadosV2.txt"; // Arquivo JSON com dados
    private static readonly string EmbeddingsFile = "embeddings.json"; // Arquivo JSON com embeddings
    private static readonly int    MaxTokensResposta = 500; // Limite de tokens na resposta

    static async Task Main()
    {
        if (!File.Exists(EmbeddingsFile))
        {
            GerarEmbedding();
        }

        while (true)
        {
            string pergunta;

            do
            {
                Console.Write("Digite sua pergunta (ou 'sair' para encerrar): ");

                pergunta = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(pergunta))
                {
                    Console.WriteLine("Por favor, digite uma pergunta válida.");
                }

            } while (string.IsNullOrEmpty(pergunta));

           
            if (pergunta.Equals("sair", StringComparison.OrdinalIgnoreCase))
                break;

           

            var embeddingsData = CarregarEmbeddings();

            var perguntaEmbedding = await ObterEmbedding(pergunta);

            var chunksRelevantes = ObterChunksRelevantes(perguntaEmbedding, embeddingsData, 3);

            string contexto = string.Join("\n", chunksRelevantes);
            string promptFinal = $"Contexto relevante:\n{contexto}\n\nPergunta:\n{pergunta}";

            string resposta = await EnviarParaOpenAI(promptFinal, MaxTokensResposta);

            Console.WriteLine("\nResposta da IA:");
            Console.WriteLine(resposta);
            Console.WriteLine("\n----------------------------------\n");
        }

        Console.WriteLine("Chat encerrado.");
    }

    static async void GerarEmbedding()
    {
        List<string> documentoDataJsonList = [];

        using (StreamReader sr = new StreamReader(ContextDados))
        {
            string textoLinha;

            while ((textoLinha = sr.ReadLine()) != null)
            {
                documentoDataJsonList.Add(textoLinha);
            }
        }

        List<EmbeddingData> embeddingsList = new();
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");

        foreach (var texto in documentoDataJsonList)
        {
            if (string.IsNullOrWhiteSpace(texto)) continue; 

            var embedding = await ObterEmbedding(texto);

            embeddingsList.Add(new EmbeddingData { Texto = texto, Embedding = embedding });
        }

        // Salvar no JSON com Embedding de saida
        string jsonEmbeddings = JsonSerializer.Serialize(embeddingsList, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(EmbeddingsFile, jsonEmbeddings);
    }

    static List<EmbeddingData> CarregarEmbeddings()
    {
        string json = File.ReadAllText(EmbeddingsFile);
        return JsonSerializer.Deserialize<List<EmbeddingData>>(json);
    }

    static async Task<List<double>> ObterEmbedding(string texto)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");

        var payload = new { model = "text-embedding-3-small", input = texto };
        string jsonPayload = JsonSerializer.Serialize(payload);

        var response = await client.PostAsync("https://api.openai.com/v1/embeddings",
            new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        string responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseText);

       return result.data.First().embedding;
    }

    static List<string> ObterChunksRelevantes(List<double> perguntaEmbedding, List<EmbeddingData> embeddingsData, int topN)
    {
        var mlContext = new MLContext();
        return embeddingsData
            .Select(d => new { d.Texto, Similaridade = CalcularSimilaridade(perguntaEmbedding, d.Embedding) })
            .OrderByDescending(x => x.Similaridade)
            .Take(topN)
            .Select(x => x.Texto)
            .ToList();
    }

    static double CalcularSimilaridade(List<double> v1, List<double> v2)
    {
        double dotProduct = v1.Zip(v2, (a, b) => a * b).Sum();
        double magnitude1 = (double)Math.Sqrt(v1.Sum(a => a * a));
        double magnitude2 = (double)Math.Sqrt(v2.Sum(b => b * b));

        return dotProduct / (magnitude1 * magnitude2);
    }

    static async Task<string> EnviarParaOpenAI(string prompt, int maxTokensResposta)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");

        var payload = new
        {
            model = "gpt-4o-mini",

            messages = new[] 
            {
                new 
                { 
                    role = "system", 
                    content = "Responda com base no contexto fornecido." 
                }, 
                new 
                { 
                  role = "user", 
                  content = prompt 
                } 
            },

            temperature = 0.7,
            max_tokens = maxTokensResposta, 
        };

        string jsonPayload = JsonSerializer.Serialize(payload);

        var response = await client.PostAsync(OpenAiEndpoint,
            new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        string responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChatResponse>(responseText);

        return result.choices.First().message.content;
    }
}

// Classes auxiliares para serialização
public class EmbeddingData
{
    public string Texto { get; set; }
    public List<double> Embedding { get; set; }
}

public class DocumentoDataJson
{
    public string tipo { get; set; }
    public string numero { get; set; }
    public string processo { get; set; }
    public string ano { get; set; }
    public string ementa { get; set; }
    public string data { get; set; }
    public string autor { get; set; }
    public string situacao { get; set; }
}

public class DataJson
{
    public string Texto { get; set; }
}

//public class EmbeddingResponse
//{
//    public List<EmbeddingItem> Data { get; set; }
//}

public class EmbeddingItem
{
    public List<float> Embedding { get; set; }
}

public class Choice
{
    public ChatMessage Message { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Datum
{
    public string @object { get; set; }
    public int index { get; set; }
    public List<double> embedding { get; set; }
}

public class EmbeddingResponse
{
    public string @object { get; set; }
    public List<Datum> data { get; set; }
    public string model { get; set; }
    public Usage usage { get; set; }
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int total_tokens { get; set; }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class ChoiceResponse
{
    public int index { get; set; }
    public Message message { get; set; }
    public object logprobs { get; set; }
    public string finish_reason { get; set; }
}

public class CompletionTokensDetails
{
    public int reasoning_tokens { get; set; }
    public int audio_tokens { get; set; }
    public int accepted_prediction_tokens { get; set; }
    public int rejected_prediction_tokens { get; set; }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
    public object refusal { get; set; }
}

public class PromptTokensDetails
{
    public int cached_tokens { get; set; }
    public int audio_tokens { get; set; }
}

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

public class UsageResponse
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
    public PromptTokensDetails prompt_tokens_details { get; set; }
    public CompletionTokensDetails completion_tokens_details { get; set; }
}

