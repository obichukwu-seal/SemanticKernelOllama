using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace SkEmbeddings;

internal class OnnxExample
{
    private IVectorStore _vectorStore;
    private Kernel _kernel;
    private ILogger<LoggingInvocationFilter> _logger;
    private ILogger<LoggingPromptRenderFilter> _loggerPrompt;
#pragma warning disable SKEXP0001
    private ITextEmbeddingGenerationService _embeddingService;
#pragma warning restore SKEXP0001
    private string _collectionName;

    public OnnxExample(IServiceProvider serviceProvider)
    {
#pragma warning disable SKEXP0001
        _embeddingService = serviceProvider.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001
        _vectorStore = serviceProvider.GetRequiredService<IVectorStore>();
        _kernel = serviceProvider.GetRequiredService<Kernel>();
        _logger = serviceProvider.GetRequiredService<ILogger<LoggingInvocationFilter>>();
        _loggerPrompt = serviceProvider.GetRequiredService<ILogger<LoggingPromptRenderFilter>>();

        _collectionName = "demo_Collection01";
    }

#pragma warning disable SKEXP0001
    private async Task<VectorStoreTextSearch<InformationItem>> Initialize()
    {
        var collection = _vectorStore.GetCollection<string, InformationItem>(_collectionName);
        await collection.CreateCollectionIfNotExistsAsync();

        // Save some information to the memory
        foreach (var factTextFile in Directory.GetFiles("./Facts", "*.txt"))
        {
            var factContent = File.ReadAllText(factTextFile);
            await collection.UpsertAsync(new()
            {
                Id = Path.GetFileNameWithoutExtension(factTextFile),
                Text = factContent,
                Embedding = await _embeddingService.GenerateEmbeddingAsync(factContent)
            });
        }

        return new VectorStoreTextSearch<InformationItem>(collection, _embeddingService);
    }
    #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.        


    public async Task Chat()
    {
        var textSearch = await Initialize();
        //_kernel.FunctionInvocationFilters.Add(new LoggingInvocationFilter(_logger));
        _kernel.PromptRenderFilters.Add(new LoggingPromptRenderFilter(_loggerPrompt));
        _kernel.Plugins.Add(textSearch.CreateWithSearch("SearchPlugin"));

        // Start the conversation
        while (true)
        {
            // Get user input
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("User > ");
            var question = Console.ReadLine()!;

            // Clean resources and exit the demo if the user input is null or empty
            if (question is null || string.IsNullOrWhiteSpace(question))
            {
                // To avoid any potential memory leak all disposable
                // services created by the kernel are disposed
                return;
            }

            // Invoke the kernel with the user input
            var response = _kernel.InvokePromptStreamingAsync(
                promptTemplate: @"Question: {{input}}
                    Answer the question using the memory content:
                    {{#with (SearchPlugin-Search input)}}
                      {{#each this}}
                        {{this}}
                        -----------------
                      {{/each}}
                    {{/with}}",
                templateFormat: "handlebars",
                promptTemplateFactory: new HandlebarsPromptTemplateFactory(),
                arguments: new KernelArguments()
                {
                    { "input", question },
                });

            Console.Write("\nAssistant > ");

            await foreach (var message in response)
            {
                Console.Write(message);
            }

            Console.WriteLine();
        }
    }

#pragma warning disable SKEXP0001 
    public async Task EvaluateSearchesAsync()
    {
        var textSearch = await Initialize();
#pragma warning restore SKEXP0001
        var query = "What is the RAG?";

        // Search and return results as a string items
        KernelSearchResults<string> stringResults = await textSearch.SearchAsync(query, new() { Top = 2, Skip = 0 });
        Console.WriteLine("--- String Results ---\n");
        await foreach (string result in stringResults.Results)
        {
            Console.WriteLine(result);
            Console.WriteLine(new string('=', 50));
        }

        // Search and return results as TextSearchResult items
        KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
        Console.WriteLine("\n--- Text Search Results ---\n");
        await foreach (TextSearchResult result in textResults.Results)
        {
            Console.WriteLine($"Name:  {result.Name}");
            Console.WriteLine($"Value: {result.Value}");
            Console.WriteLine($"Link:  {result.Link}");
            Console.WriteLine(new string('=', 50));
        }

        // Search and returns results as DataModel items
        KernelSearchResults<object> fullResults = await textSearch.GetSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
        Console.WriteLine("\n--- DataModel Results ---\n");
        await foreach (InformationItem result in fullResults.Results)
        {
            Console.WriteLine($"Key:         {result.Id}");
            Console.WriteLine($"Text:        {result.Text}");
            Console.WriteLine($"Embedding:   {result.Embedding.Length}");
            Console.WriteLine(new string('=', 50));
        }
    }


}



/// <summary>
/// Information item to represent the embedding data stored in the memory
/// </summary>
internal sealed class InformationItem
{
    [VectorStoreRecordKey]
    [TextSearchResultName]
    public string Id { get; set; } = string.Empty;

    [VectorStoreRecordData]
    [TextSearchResultValue]
    public string Text { get; set; } = string.Empty;

    [VectorStoreRecordVector(Dimensions: 768)]
    //[VectorStoreRecordVector(Dimensions: 384)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}


public class LoggingInvocationFilter(ILogger<LoggingInvocationFilter> logger) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var function = context.Function;
        var pluginName = function.PluginName ?? "global";
        var functionName = function.Name;

        logger.LogInformation("➡️ Invoking {Plugin}.{Function} with args: {Args}",
            pluginName, functionName, context.Arguments?.ToString());

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await next(context); // 🚀 Calls the actual function

        stopwatch.Stop();

        var result = context.Result?.ToString() ?? "null";

        logger.LogInformation("✅ Finished {Plugin}.{Function} in {ElapsedMs} ms. Result: {Result}",
            pluginName, functionName, stopwatch.ElapsedMilliseconds, result);
    }
}