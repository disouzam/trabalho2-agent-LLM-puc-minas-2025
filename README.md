# ProcessoChat
É uma plataforma interativa que facilita a busca, acompanhamento e consulta de processos legislativos.

# Integrantes do Trabalho
206228 - Deivisson Silva <br/>
206241 - Edgar Júnior <br/>
208237 - Mirella Alves <br/>
211700 - Raphael Mendes <br/>
207441 - Viviane Leilane <br/>
208573 - Dickson Souza <br/>

# Trabalho1_IA

# Descrição do Trabalho

Este é um aplicativo console desenvolvido em .NET C# que utiliza a API da OpenAI para buscar processos legislativos. O sistema interage com modelos de linguagem para processar e retornar informações relevantes.

# Estrutura do Projeto

O projeto está organizado da seguinte forma:

Trabalho1_IA/
│── CHAT/
│   ├── ChatMessage.cs
│   ├── ChatResponse.cs
│
│── LLM/
│   ├── ChoiceResponse.cs
│   ├── CompletionTokensDetails.cs
│   ├── Datum.cs
│   ├── EmbeddingData.cs
│   ├── EmbeddingResponse.cs
│   ├── FunctionCall.cs
│   ├── Message.cs
│   ├── PromptTokensDetails.cs
│   ├── Usage.cs
│   ├── UsageResponse.cs
│
│── Processos/
│   ├── Autor.cs
│   ├── Processo.cs
│   ├── ResponseConsultaExternaModelo.cs
│   ├── Sessao.cs
│
│── Serviço/
│   ├── ClientAPI.cs
│   ├── Embeddings.cs
│   ├── OpenAIClient.cs
│   ├── ProcessoExternoService.cs
│
│── Program.cs
│── README.md
│── .gitignore

# Requisitos

Para executar o projeto, é necessário:

Uma chave de API da OpenAI

.NET instalado na máquina

# Modelo Utilizado

O projeto utiliza o modelo gpt-4o-mini da OpenAI para processar consultas e respostas relacionadas a processos legislativos.

# Como Executar

Configure sua chave da OpenAI no arquivo de configuração ou como variável de ambiente.

Compile e execute o projeto usando o .NET CLI:

dotnet run

# Contribuição

Sugestões e melhorias são bem-vindas! Sinta-se à vontade para abrir uma issue ou enviar um pull request.

