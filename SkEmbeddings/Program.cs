using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Npgsql;
using SkEmbeddings;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

ConfigureSkServices(builder.Services, useOllama: true);

var host = builder.Build();
await host.StartAsync();


//var skTextExample = new SkTextMemoryExample(host.Services);
//await skTextExample.RunExampleAsync(memoryCollectionName: "demo_02");

//var onnxExample = new OnnxExample(host.Services);
//await onnxExample.EvaluateSearchesAsync();
//await onnxExample.Chat();

var productExample = new ProductExample(host.Services);
await productExample.EvaluateSearchesAsync();
await productExample.Chat();

var kernel = host.Services.GetRequiredService<Kernel>();
DisposeServices(kernel);


void ConfigureSkServices(IServiceCollection services, bool useOllama)
{
    if (useOllama)
        ConfigureOllamaServices(builder.Services);
    else
        ConfigureOnnxServices(builder.Services);

    ConfigureStoresServices(services);

    services.AddTransient((serviceProvider) =>
    {
        return new Kernel(serviceProvider);
    });
}

void ConfigureStoresServices(IServiceCollection services)
{
    var postgresConnection = "Host=localhost;Port=5432;Username=devseal;Password=ytrewqPoiu;Database=vector_db;";
    builder.Services.AddPostgresVectorStore(postgresConnection);

#pragma warning disable SKEXP0001
    builder.Services.AddTransient<IMemoryStore>((serviceProvider) =>
    {
        var storeSize = serviceProvider.GetService<VectorSize>();

        NpgsqlDataSourceBuilder dataSourceBuilder = new(postgresConnection);
        dataSourceBuilder.UseVector();
        NpgsqlDataSource dataSource = dataSourceBuilder.Build();

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return new PostgresMemoryStore(dataSource, vectorSize: storeSize.Size, schema: "public");
#pragma warning restore SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    });
#pragma warning restore SKEXP0001

#pragma warning disable SKEXP0001
    builder.Services.AddTransient<ISemanticTextMemory>((serviceProvider) =>
    {
        var memoryStore = serviceProvider.GetService<IMemoryStore>();
        var embeddingGenerator = serviceProvider.GetService<ITextEmbeddingGenerationService>();

        return new SemanticTextMemory(memoryStore, embeddingGenerator);
    });
#pragma warning restore SKEXP0001
}

void ConfigureOllamaServices(IServiceCollection services)
{
    var endpoint = new Uri("http://localhost:11434");
    var modelId = "llama3.2:3b"; // "llama3.2-vision:11b";
    var embeddingModel = "nomic-embed-text:latest";

    var httpClient = new HttpClient
    {
        BaseAddress = endpoint,
        Timeout = TimeSpan.FromMinutes(5)
    };

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    services.AddOllamaChatCompletion(
        modelId: modelId,
        httpClient: httpClient
    );

    services.AddOllamaTextEmbeddingGeneration(
        modelId: embeddingModel,
        httpClient: httpClient
    );
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    services.AddScoped<VectorSize>((_) => new VectorSize { Size = 768 });
}

void ConfigureOnnxServices(IServiceCollection services)
{
#pragma warning disable SKEXP0070
    builder.Services.AddOnnxRuntimeGenAIChatCompletion("phi3", "./Onnx/cpu-int4-rtn-block-32");

    builder.Services.AddBertOnnxTextEmbeddingGeneration(
        onnxModelPath: "./Onnx/model_quantized.onnx",
        vocabPath: "./Onnx/vocab.txt"
    );
#pragma warning restore SKEXP0070

    services.AddScoped<VectorSize>((_) => new VectorSize { Size = 384 });

}

static void DisposeServices(Kernel kernel)
{
    foreach (var target in kernel
        .GetAllServices<IChatCompletionService>()
        .OfType<IDisposable>())
    {
        target.Dispose();
    }
}

class VectorSize
{
    public int Size { get; set; }
}
