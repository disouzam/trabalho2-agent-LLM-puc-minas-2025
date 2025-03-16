using System.Text;
using System.Text.Json;


using ProcessoChat.Chat;
using ProcessoChat.LLM;
using ProcessoChat.Servicos;

namespace ProcessoChat;

public class Program
{
    private static readonly string ContextDados = "Lista "; // Arquivo JSON com dados
    private static readonly string ContextEmbeddingsFile = "embeddingsContexto.json"; // Arquivo JSON com embeddings do contexto
    private static readonly string MemoryEmbeddingsFile = "embeddingsMemoria.json"; // Arquivo JSON com embeddings da memória
    private static readonly int MaxTokensResposta = 500; // Limite de tokens na resposta
    private static bool ContextoAtualizado = false;

    public static async Task Main()
    {
        if(!File.Exists(ContextEmbeddingsFile))
        {
            var conteudoDados = Embeddings.LerArquivoTexto(ContextDados);
            var embeddingList = await Embeddings.GerarEmbedding(conteudoDados);

            Embeddings.SalvarArquivoDeEmbeddings(embeddingList, ContextEmbeddingsFile);
        }

        var embeddingsDaMemoria = new List<EmbeddingData>();
        var embeddingsDoContexto = new List<EmbeddingData>();
        var sessionUsageStatistics = new List<UsageResponse>();

      
        while(true)
        {
            string pergunta;

            do
            {
                Console.Write("Digite sua pergunta (ou 'sair' para encerrar): ");

                pergunta = Console.ReadLine()?.Trim();

                if(string.IsNullOrEmpty(pergunta))
                {
                    Console.WriteLine("Por favor, digite uma pergunta válida.");
                }

            } while(string.IsNullOrEmpty(pergunta));


            if(pergunta.Equals("sair", StringComparison.OrdinalIgnoreCase))
                break;

            if(embeddingsDoContexto.Count == 0 || ContextoAtualizado)
            {
                embeddingsDoContexto = Embeddings.CarregarEmbeddings(ContextEmbeddingsFile);
                ContextoAtualizado = false;
            }

            if(embeddingsDaMemoria.Count == 0)
            {
                embeddingsDaMemoria = Embeddings.CarregarEmbeddings(MemoryEmbeddingsFile);
            }

            var perguntaEmbedding = await Embeddings.ObterEmbedding(pergunta);

            var chunksRelevantesDoContexto = Embeddings.ObterChunksRelevantes(perguntaEmbedding, embeddingsDoContexto, 3);

            var numeroUltimasMensagens = 3;

            var chunksRelevantesDaMemoria = new List<string>();
            var numeroEmbeddingsDaMemoria = embeddingsDaMemoria.Count;

            if(numeroEmbeddingsDaMemoria > numeroUltimasMensagens)
            {
                chunksRelevantesDaMemoria = Embeddings.ObterChunksRelevantes(
                    perguntaEmbedding,
                    embeddingsDaMemoria.Take(numeroEmbeddingsDaMemoria - numeroUltimasMensagens).ToList(),
                    topN: 3
                );
            }

            var ultimasTresMensagens = embeddingsDaMemoria.TakeLast(numeroUltimasMensagens).Select(m => m.Texto).ToList();
            var chunksTotal = new List<string>();

            chunksTotal.AddRange(chunksRelevantesDoContexto);
            chunksTotal.AddRange(chunksRelevantesDaMemoria);
            chunksTotal.AddRange(ultimasTresMensagens);

            string contexto = string.Join("\n", chunksTotal);
            string promptFinal = $"Contexto relevante:\n{contexto}\n\nPergunta:\n{pergunta}";

            var resposta = await EnviarParaOpenAI(promptFinal, MaxTokensResposta);

            var textoDaResposta = resposta.Item1;
            if(textoDaResposta.Contains("Não sei", StringComparison.OrdinalIgnoreCase))
            {
                textoDaResposta = "Não sei. A resposta não foi encontrada no contexto fornecido.";
            }

            sessionUsageStatistics.AddRange(resposta.Item2);

            var perguntaEresposta = $"Pergunta do usuário:\n\n{pergunta}\n\nResposta da IA:\n\n{textoDaResposta}";
            var embeddingsDaResposta = await Embeddings.GerarEmbedding([perguntaEresposta]);

            embeddingsDaMemoria.AddRange(embeddingsDaResposta);

            Embeddings.SalvarArquivoDeEmbeddings(embeddingsDaMemoria, MemoryEmbeddingsFile);

            Console.WriteLine("\nResposta da IA:");
            Console.WriteLine(textoDaResposta);
            Console.WriteLine("\n----------------------------------\n");
        }

        foreach(var usageItem in sessionUsageStatistics)
        {
            var mensagem = $"\nTokens do prompt: {usageItem.prompt_tokens}\nTokens da resposta: {usageItem.completion_tokens}";
            Console.WriteLine(mensagem);
        }

        Console.WriteLine("Chat encerrado.");
    }

    private static async Task<(string, List<UsageResponse>)> EnviarParaOpenAI(string prompt, int maxTokensResposta)
    {
        using var client = new ClientAPI().ObterClientAPI();
        const string modelName = "gpt-4o-mini";

        var usageStatistics = new List<UsageResponse>();

        var result = await ChamadaPrincipal(client, modelName, prompt, maxTokensResposta);

        usageStatistics.Add(result.Usage);

        if(result.Choices.First().Message.FunctionCall != null)
        {
            string functionName = result.Choices.First().Message.FunctionCall.Name;
            string argumentsJson = result.Choices.First().Message.FunctionCall.Arguments;

            if(functionName == "GetProcessoExterno")
            {
                var result2 = await ProcessoExternoService.ObterInfoProcessoExterno(client, modelName, argumentsJson);
                ContextoAtualizado = true;

                usageStatistics.Add(result.Usage);
                var resposta2 = result2.Choices.First().Message.Content;
                return (resposta2, usageStatistics);
            }
        }

        var resposta = result.Choices.First().Message.Content;
        return (resposta, usageStatistics);
    }

    private static async Task<ChatResponse> ChamadaPrincipal(HttpClient client, string modelName, string prompt, int maxTokensResposta)
    {
        var payload = new
        {
            model = modelName,

            messages = new[]
            {
                new
                {
                    role = "system",
                    content = @"Você deve responder **exclusivamente** com base no contexto fornecido. Se a resposta não estiver no contexto,
                    também pode usar a função para buscar, caso não encontrar em nenhum responda  'Não sei'."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            functions = new[]
            {
                new
                {
                    name = "GetProcessoExterno",
                    description = "Busca detalhes de um processo fora do modelo de dados usando uma api externa",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            Numero = new { type = "integer", description = "Numero do processo" }
                        },
                        required = new[] { "Numero" }
                    }
                }
            },

            temperature = 0.4,
            max_tokens = maxTokensResposta,
        };

        string jsonPayload = JsonSerializer.Serialize(payload);

        var response = await client.PostAsync(ClientAPI.OpenAiEndpoint,
            new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        string responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChatResponse>(responseText);
        return result;
    }

}
