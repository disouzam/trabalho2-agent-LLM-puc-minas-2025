using ProcessoChat.Chat;
using ProcessoChat.Processos;
using RestSharp;
using System.Text.Json;
using System.Text;

namespace ProcessoChat.Servicos;

public static class ProcessoExternoService
{
    public static async Task<ChatResponse> ObterInfoProcessoExterno(HttpClient client, string modelName, string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, int>>(argumentsJson);
        int numeroProjeto = args["Numero"];

        string resultado = await GetProcessoExterno(numeroProjeto);

        var followUpPayload = new
        {
            model = modelName,
            messages = new[]
            {
                new
                {
                    role = "system",
                     content = @"Você é um assistente especializado em responder perguntas com base exclusivamente nos dados fornecidos. 
                                Se necessário, utilize a função do agente para buscar informações adicionais. Se a resposta não estiver disponível, 
                                responda apenas 'Não sei'."
                },
                new
                {
                    role = "assistant",
                    content = resultado
                },
                new
                {
                    role = "user",
                    content = "Com base nas informações obtidas, gere um resumo detalhado e objetivo."
                }
            },

            temperature = 0.4,
        };

        string jsonFollowUpPayload = JsonSerializer.Serialize(followUpPayload);

        var response2 = await client.PostAsync(ClientAPI.OpenAiEndpoint,
        new StringContent(jsonFollowUpPayload, Encoding.UTF8, "application/json"));

        string response2Text = await response2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<ChatResponse>(response2Text);
        return result2;
    }

    private static async Task<string> GetProcessoExterno(int processoId)
    {
        if (string.IsNullOrEmpty(Sessao.Token))
        {
            Sessao.Token = await LoginAPIExterna();
        }

        string responseJson = await ConsultarProcesso(processoId, Sessao.Token);

        if (!string.IsNullOrEmpty(responseJson))
        {
            try
            {
                // Desserializa o JSON para um objeto em C#
                var responseObj = JsonSerializer.Deserialize<ResponseConsultaExternaModelo>(responseJson);

                if (responseObj?.Dados != null && responseObj.Dados.Any())
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

                    Embeddings.AtualizarEmbedding(resultadoJson);

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
        var options = new RestClientOptions("{inserir aqui a URL da API externa}")
        {
            Timeout = TimeSpan.FromMilliseconds(-1),
        };

        var client = new RestClient(options);
        var request = new RestRequest("{inserir aqui a URL do endpoint para efetuar o login}", Method.Post);

        var body = new
        {
            Login = "{insira aqui o login}",
            Senha = "{insira aqui a senha}",
        };

        request.AddJsonBody(body);

        RestResponse response = await client.ExecuteAsync(request);

        if (response.IsSuccessful && response.Content != null)
        {
            var jsonResponse = JsonDocument.Parse(response.Content);

            if (jsonResponse.RootElement.TryGetProperty("Dados", out var dados) &&
                dados.TryGetProperty("Authorization", out var token))
            {
                return token.GetString();
            }
        }

        return "Falha ao obter token.";
    }

    private static async Task<string> ConsultarProcesso(int Numero, string token)
    {
        var options = new RestClientOptions("{inserir aqui a URL da API externa}")
        {
            Timeout = TimeSpan.FromMilliseconds(-1),
        };
        var client = new RestClient(options);
        var request = new RestRequest("{inserir aqui a URL do endpoint para consultar o processo}", Method.Get);

        request.AddHeader("Authorization", $"{token}");

        RestResponse response = await client.ExecuteAsync(request);
        return response.Content;
    }
}
