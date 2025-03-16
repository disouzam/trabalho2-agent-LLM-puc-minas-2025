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

            var resposta = await OpenAIClient.EnviarParaOpenAI(promptFinal, MaxTokensResposta);

            ContextoAtualizado = resposta.Item2.Any(x => x.contextoAtualizado == true);

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
}
