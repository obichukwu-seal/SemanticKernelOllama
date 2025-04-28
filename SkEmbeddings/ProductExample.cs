using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkEmbeddings;

internal class ProductExample
{
    private IVectorStore _vectorStore;
    private Kernel _kernel;
    private ILogger<LoggingPromptRenderFilter> _logger;
#pragma warning disable SKEXP0001
    private ITextEmbeddingGenerationService _embeddingService;
#pragma warning restore SKEXP0001
    private string _collectionName;

    public ProductExample(IServiceProvider serviceProvider)
    {
#pragma warning disable SKEXP0001
        _embeddingService = serviceProvider.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001
        _vectorStore = serviceProvider.GetRequiredService<IVectorStore>();
        _kernel = serviceProvider.GetRequiredService<Kernel>();
        _logger = serviceProvider.GetRequiredService<ILogger<LoggingPromptRenderFilter>>();

        _collectionName = "demo_ProductCollection00";
    }

#pragma warning disable SKEXP0001
    private async Task Initialize()
    {
        var collection = _vectorStore.GetCollection<string, ProductItem>(_collectionName);
        await collection.CreateCollectionIfNotExistsAsync();

        // Save some information to the memory
        var factContent = File.ReadAllText("./Facts/product_descriptions_100k.json");
        var products = JsonSerializer.Deserialize<List<ProductItem>>(factContent);

        foreach (var product in products)
        {
            product.Embedding = await _embeddingService.GenerateEmbeddingAsync(product.Description);
            await collection.UpsertAsync(product);
        }

        //var searchVector = await _embeddingService.GenerateEmbeddingAsync("Stainless steel");
        //var search = await collection.VectorizedSearchAsync(searchVector, new VectorSearchOptions<ProductItem>
        //{
        //    Top = 4,
        //    VectorProperty = r => r.Embedding
        //});

        //var searchList = search.Results.ToBlockingEnumerable().ToList();
        //foreach (var result in searchList)
        //{
        //    Console.WriteLine("Result: " + result.Record.Description);
        //    Console.WriteLine("Score: " + result.Score);
        //}
    }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.        


    public async Task Chat()
    {
        await Initialize();

#pragma warning disable SKEXP0001
        var textSearch = _kernel.GetRequiredService<VectorStoreTextSearch<ProductItem>>();
#pragma warning restore SKEXP0001

        _kernel.PromptRenderFilters.Add(new LoggingPromptRenderFilter(_logger));
        _kernel.Plugins.Add(textSearch.CreateWithGetSearchResults("SearchPlugin"));

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
                    {{#with (SearchPlugin-GetSearchResults input)}}
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


    public async Task EvaluateSearchesAsync()
    {
        await Initialize();

#pragma warning disable SKEXP0001 
        var textSearch = _kernel.GetRequiredService<VectorStoreTextSearch<ProductItem>>();
        var searchByDi = _kernel.GetRequiredService<IVectorizedSearch<ProductItem>>();
#pragma warning restore SKEXP0001
        var query = "Headphones";

        // Search and return results as a string items
        KernelSearchResults<string> stringResults = await textSearch.SearchAsync(query, new() { Top = 10, Skip = 0 });
        Console.WriteLine("--- String Results ---\n");
        await foreach (string result in stringResults.Results)
        {
            Console.WriteLine(result);
            Console.WriteLine(new string('=', 50));
        }

        var searchVector = await _embeddingService.GenerateEmbeddingAsync(query);
        var search = await searchByDi.VectorizedSearchAsync(searchVector, new VectorSearchOptions<ProductItem>{ Top = 10 });
        foreach (var result in search.Results.ToBlockingEnumerable())
        {
            Console.WriteLine("Result: " + result.Record.Description);
            Console.WriteLine("Score: " + result.Score);
            Console.WriteLine(new string('=', 50));
        }
        Console.WriteLine(new string('*', 60));
        Console.WriteLine(new string('*', 60));

        // Search and return results as TextSearchResult items
        KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 10, Skip = 0, });
        Console.WriteLine("\n--- Text Search Results ---\n");
        await foreach (TextSearchResult result in textResults.Results)
        {
            Console.WriteLine($"Name:  {result.Name}");
            Console.WriteLine($"Value: {result.Value}");
            Console.WriteLine($"Link:  {result.Link}");
            Console.WriteLine(new string('=', 50));
        }

        search = await searchByDi.VectorizedSearchAsync(searchVector, new VectorSearchOptions<ProductItem> { Top = 10 });
        foreach (var result in search.Results.ToBlockingEnumerable())
        {
            Console.WriteLine("Result: " + result.Record.Name);
            Console.WriteLine("Result: " + result.Record.Description);
            Console.WriteLine("Score: " + result.Score);
            Console.WriteLine(new string('=', 50));
        }
        Console.WriteLine(new string('*', 60));
        Console.WriteLine();

        // Search and returns results as DataModel items
        KernelSearchResults<object> fullResults = await textSearch.GetSearchResultsAsync(query, new() { Top = 10, Skip = 0 });
        Console.WriteLine("\n--- DataModel Results ---\n");
        await foreach (ProductItem result in fullResults.Results)
        {
            Console.WriteLine($"Key:         {result.Id}");
            Console.WriteLine($"Name:         {result.Name}");
            Console.WriteLine($"Price:         {result.Price}");
            Console.WriteLine($"Text:        {result.Description}");
            Console.WriteLine($"Embedding:   {result.Embedding.Length}");
            Console.WriteLine(new string('=', 50));
        }
        search = await searchByDi.VectorizedSearchAsync(searchVector, new VectorSearchOptions<ProductItem> { Top = 10 });
        foreach (var result in search.Results.ToBlockingEnumerable())
        {
            Console.WriteLine($"Key:         {result.Record.Id}");
            Console.WriteLine($"Name:         {result.Record.Name}");
            Console.WriteLine($"Price:         {result.Record.Price}");
            Console.WriteLine($"Text:        {result.Record.Description}");
            Console.WriteLine($"Embedding:   {result.Record.Embedding.Length}");
            Console.WriteLine("Score: " + result.Score);
            Console.WriteLine(new string('=', 50));
        }
        Console.WriteLine(new string('*', 60));
        Console.WriteLine();
    }
}

/// <summary>
/// Information item to represent the embedding data stored in the memory
/// </summary>
internal sealed class ProductItem
{
    [VectorStoreRecordKey]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [VectorStoreRecordData]
    [TextSearchResultName]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [VectorStoreRecordData]
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [VectorStoreRecordData]
    [TextSearchResultValue]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [VectorStoreRecordData]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [VectorStoreRecordVector(Dimensions: 768)]
    //[VectorStoreRecordVector(Dimensions: 384)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}


public class LoggingPromptRenderFilter(ILogger<LoggingPromptRenderFilter> logger) : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        // Render the actual prompt
        await next(context);

        // Log the final rendered prompt
        logger.LogInformation("\nFinal Rendered Prompt:\t{Prompt}", context.RenderedPrompt);
    }
}