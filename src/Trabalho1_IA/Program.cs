using System.Text;
using System.Text.Json;

using Microsoft.ML;

using RestSharp;

using ProcessoChat.Chat;
using ProcessoChat.LLM;
using ProcessoChat.Processos;
using ProcessoChat.Servicos;

namespace ProcessoChat;

public class Program
{
    private static readonly string ContextDados = "dadosV2.txt"; // Arquivo JSON com dados
    private static readonly string EmbeddingsFile = "embeddings.json"; // Arquivo JSON com embeddings
    private static readonly int MaxTokensResposta = 500; // Limite de tokens na resposta

    public static async Task Main()
    {
        if(!File.Exists(EmbeddingsFile))
        {
            var embeddingList = await Embeddings.GerarEmbedding(ContextDados, EmbeddingsFile);
        }

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

            var embeddingsData = Embeddings.CarregarEmbeddings(EmbeddingsFile);

            var perguntaEmbedding = await Embeddings.ObterEmbedding(pergunta);

            var chunksRelevantes = Embeddings.ObterChunksRelevantes(perguntaEmbedding, embeddingsData, 3);

            // (Dickson) Passo 4: Nesse ponto o contexto já está formado por chunks do RAG e da memória
            string contexto = string.Join("\n", chunksRelevantes);
            string promptFinal = $"Contexto relevante:\n{contexto}\n\nPergunta:\n{pergunta}";

            string resposta = await EnviarParaOpenAI(promptFinal, MaxTokensResposta);

            // Rascunho de implementação do uso de memória
            // (Dickson) Passo 0: Converter a resposta em embeddings
            // (Dickson) Passo 1: Salvar esses embeddings em um arquivo separado

            Console.WriteLine("\nResposta da IA:");
            Console.WriteLine(resposta);
            Console.WriteLine("\n----------------------------------\n");
        }

        Console.WriteLine("Chat encerrado.");
    }



    static async Task<string> EnviarParaOpenAI(string prompt, int maxTokensResposta)
    {
        using var client = new ClientAPI().ObterClientAPI();

        var payload = new
        {
            model = "gpt-4o-mini",

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

        if(result.Choices.First().Message.FunctionCall != null)
        {
            string functionName = result.Choices.First().Message.FunctionCall.Name;
            string argumentsJson = result.Choices.First().Message.FunctionCall.Arguments;

            if(functionName == "GetProcessoExterno")
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, int>>(argumentsJson);
                int numeroProjeto = args["Numero"];

                string resultado = await GetProcessoExterno(numeroProjeto);

                var responseMessage = new
                {
                    role = "assistant",
                    content = resultado
                };

                var followUpPayload = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                       new
                       {
                            role = "system",
                            content = "Você é um assistente especializado em responder perguntas com base nos dados fornecidos."
                       },
                       new
                       {
                            role = "assistant",
                            content = resultado
                       },
                       new
                       {
                            role = "user",
                            content = "Com base no resultado obtido da função, forneça um resumo detalhado."
                       },
                    }
                };

                string jsonFollowUpPayload = JsonSerializer.Serialize(followUpPayload);

                var response2 = await client.PostAsync(ClientAPI.OpenAiEndpoint,
                new StringContent(jsonFollowUpPayload, Encoding.UTF8, "application/json"));

                string response2Text = await response2.Content.ReadAsStringAsync();
                var result2 = JsonSerializer.Deserialize<ChatResponse>(response2Text);

                return result2.Choices.First().Message.Content;
            }
        }

        string resposta = result.Choices.First().Message.Content;

        if(resposta.Contains("Não sei", StringComparison.OrdinalIgnoreCase))
        {
            resposta = "Não sei. A resposta não foi encontrada no contexto fornecido.";
        }

        return resposta;
    }

    static async Task<string> GetProcessoExterno(int processoId)
    {
        if(string.IsNullOrEmpty(Sessao.Token))
        {
            Sessao.Token = await LoginAPIExterna();
        }

        string responseJson = await ConsultarProcesso(processoId, Sessao.Token);

        if(!string.IsNullOrEmpty(responseJson))
        {
            try
            {
                // Desserializa o JSON para um objeto em C#
                var responseObj = JsonSerializer.Deserialize<ResponseConsultaExternaModelo>(responseJson);

                if(responseObj?.Dados != null && responseObj.Dados.Any())
                {
                    var processo = responseObj.Dados.First();

                    var resultado = new
                    {
                        tipo = processo.tipo,
                        numero = processo.numero,
                        processo = processo.processo,
                        ano = processo.ano,
                        ementa = processo.assunto,
                        data = processo.data,
                        autor = processo.AutorRequerenteDados?.nomeRazao ?? "Desconhecido",
                        situacao = processo.situacao
                    };

                    string resultadoJson = JsonSerializer.Serialize(resultado, new JsonSerializerOptions { WriteIndented = true });
                    return resultadoJson;
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }
        }
        else
        {
            return "";
        }
    }

    private static async Task<string> LoginAPIExterna()
    {
        var options = new RestClientOptions("https://homolog.nopapercloud.com.br")
        {
            Timeout = TimeSpan.FromMilliseconds(-1),
        };

        var client = new RestClient(options);
        var request = new RestRequest("/camaramodelo/api/custom/base/login.aspx", Method.Post);

        var body = new
        {
            Login = "teste.usuario",
            Senha = "teste"
        };

        request.AddJsonBody(body);

        RestResponse response = await client.ExecuteAsync(request);
        Console.WriteLine(response.Content);

        if(response.IsSuccessful && response.Content != null)
        {
            var jsonResponse = JsonDocument.Parse(response.Content);

            if(jsonResponse.RootElement.TryGetProperty("Dados", out var dados) &&
                dados.TryGetProperty("Authorization", out var token))
            {
                return token.GetString();
            }
        }

        return "Falha ao obter token.";
    }

    static async Task<string> ConsultarProcesso(int Numero, string token)
    {
        var options = new RestClientOptions("https://homolog.nopapercloud.com.br")
        {
            Timeout = TimeSpan.FromMilliseconds(-1),
        };
        var client = new RestClient(options);
        var request = new RestRequest($"/camaramodelo/api/custom/base/processos_consultar.aspx?Processo={Numero}", Method.Get);

        request.AddHeader("Authorization", $"{token}");

        RestResponse response = await client.ExecuteAsync(request);
        return response.Content;
    }
}
