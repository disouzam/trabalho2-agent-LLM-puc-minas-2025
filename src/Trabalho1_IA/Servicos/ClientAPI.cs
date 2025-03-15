namespace ProcessoChat.Servicos;

public class ClientAPI : IDisposable
{
    private static readonly string OpenAiApiKey = "{insira sua chave aqui}"; // Substitua pela sua chave de API OpenAI
    private bool disposedValue;

    public static string EmbeddingsUrl { get; } = "https://api.openai.com/v1/embeddings";
    public static string OpenAiEndpoint { get; } = "https://api.openai.com/v1/chat/completions";

    public HttpClient Client { get; private set; } = null;

    public HttpClient ObterClientAPI()
    {
        Client = new();
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");

        return Client;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                Client.Dispose();
                Client = null;
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }
    ~ClientAPI()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
