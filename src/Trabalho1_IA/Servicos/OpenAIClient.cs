using ProcessoChat.Chat;
using ProcessoChat.LLM;

using System.Text;
using System.Text.Json;


namespace ProcessoChat.Servicos;

public static class OpenAIClient
{
    public static async Task<(string, List<UsageResponse>)> EnviarParaOpenAI(string prompt, int maxTokensResposta)
    {
        using var client = new ClientAPI().ObterClientAPI();
        const string modelName = "gpt-4o-mini";

        var usageStatistics = new List<UsageResponse>();

        var result = await EnviarConsultaParaOpenAI(client, modelName, prompt, maxTokensResposta);

        usageStatistics.Add(result.Usage);

        if(result.Choices.First().Message.FunctionCall != null)
        {
            string functionName = result.Choices.First().Message.FunctionCall.Name;
            string argumentsJson = result.Choices.First().Message.FunctionCall.Arguments;

            if(functionName == "GetProcessoExterno")
            {
                var result2 = await ProcessoExternoService.ObterInfoProcessoExterno(client, modelName, argumentsJson);
                result.Usage.contextoAtualizado = true;
                usageStatistics.Add(result.Usage);
                var resposta2 = result2.Choices.First().Message.Content;
                return (resposta2, usageStatistics);
            }
        }

        var resposta = result.Choices.First().Message.Content;
        return (resposta, usageStatistics);
    }

    private static async Task<ChatResponse> EnviarConsultaParaOpenAI(HttpClient client, string modelName, string prompt, int maxTokensResposta)
    {
        var payload = new
        {
            model = modelName,

            messages = new[]
            {
                new
                {
                    role = "system",
                    content = @"Você deve responder **somente** com base no contexto fornecido. 
                                Se a resposta não estiver no contexto, utilize a função do agente para buscar mais informações. 
                                Caso a informação não seja encontrada no contexto nem na função, responda apenas: 'Não sei'."
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
