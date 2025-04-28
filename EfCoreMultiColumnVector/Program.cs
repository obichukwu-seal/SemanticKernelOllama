using EfCoreMultiColumnVector.DataAccess;
using EfCoreMultiColumnVector.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Pgvector.EntityFrameworkCore;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

// register AppDbContext to DI
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, o => o.UseVector());
});

builder.Services.AddPostgresVectorStore(connectionString);

builder.Services.AddTransient<IVectorizedSearch<BankInfoVector>>((serviceProvider) => {
    var _vectorStore = serviceProvider.GetRequiredService<IVectorStore>();
    var collection = _vectorStore.GetCollection<int, BankInfoVector>("BankInfos");

    return collection;
});


#pragma warning disable SKEXP0070
builder.Services.AddBertOnnxTextEmbeddingGeneration(
    onnxModelPath: "./OnnxModels/model_quantized.onnx",
    vocabPath: "./OnnxModels/vocab.txt"
);
#pragma warning restore SKEXP0070


builder.Services.AddTransient((serviceProvider) =>
{
    return new Kernel(serviceProvider);
});

var host = builder.Build();
await host.StartAsync();

await Migrator.MigrateAsync(host);

// Start the conversation

var searchByDi = host.Services.GetRequiredService<IVectorizedSearch<BankInfoVector>>();
#pragma warning disable SKEXP0001
var embeddingService = host.Services.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001

var appDbContext = host.Services.GetRequiredService<AppDbContext>();

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

    var searchVector = await embeddingService.GenerateEmbeddingAsync(question);
    var pgVectorSearch = new Pgvector.Vector(searchVector);
    // Search By Name - Both Methods
    Console.WriteLine("Search By Name");
    Console.WriteLine(new string('=', 50));

    var nameVectorSearch = await searchByDi.VectorizedSearchAsync(searchVector,
        new VectorSearchOptions<BankInfoVector> { Top = 5, VectorProperty = e => e.NameEmbedding });

    var nameEfSearch = await appDbContext.BankInfos
        .AsNoTracking()
        .OrderBy(e => e.NameEmbedding!.CosineDistance(pgVectorSearch))
        .Take(5)
        .Select(e => new EfVectorQueryResult(e.BankName, e.Slogan, 1 - (double)e.NameEmbedding!.CosineDistance(pgVectorSearch)))
        .ToListAsync();

    DisplayZippedResults(nameVectorSearch.Results.ToBlockingEnumerable(), nameEfSearch, "Name");

    // Search By Slogan - Both Methods
    Console.WriteLine("Search By Slogan");
    Console.WriteLine(new string('=', 50));

    var sloganVectorSearch = await searchByDi.VectorizedSearchAsync(searchVector,
        new VectorSearchOptions<BankInfoVector> { Top = 5, VectorProperty = e => e.SloganEmbedding });

    var sloganEfSearch = await appDbContext.BankInfos
        .AsNoTracking()
        .OrderBy(e => e.SloganEmbedding!.CosineDistance(pgVectorSearch))
        .Take(5)
        .Select(e => new EfVectorQueryResult(e.BankName, e.Slogan, 1 - (double)e.SloganEmbedding!.CosineDistance(pgVectorSearch)))
        .ToListAsync();

    DisplayZippedResults(sloganVectorSearch.Results.ToBlockingEnumerable(), sloganEfSearch, "Slogan");


    Console.Write("\nAssistant > ");

    //await foreach (var message in response)
    //{
    //    Console.Write(message);
    //}

    Console.WriteLine();
}


// Function to display zipped results
void DisplayZippedResults(IEnumerable<dynamic> vectorResults, List<EfVectorQueryResult> efResults, string searchType)
{
    const int BankNameWidth = 30;
    const int SloganWidth = 40;

    Console.WriteLine($"\n{searchType} Search Comparison");
    Console.WriteLine(new string('-', 86));

    // Print table header
    Console.WriteLine($"| {"Source",-8} | {"Score",-8} | {"Bank Name",-BankNameWidth} | {"Slogan",-SloganWidth} |");
    Console.WriteLine($"|{new string('-', 10)}|{new string('-', 10)}|{new string('-', BankNameWidth + 2)}|{new string('-', SloganWidth + 2)}|");

    var combined = vectorResults
        .Select((v, i) => new {
            Vector = v,
            EF = i < efResults.Count ? efResults[i] : null
        });

    foreach (var result in combined)
    {
        PrintRow("Vector", result.Vector.Score, result.Vector.Record.BankName, result.Vector.Record.Slogan);

        if (result.EF != null)
        {
            PrintRow("EF Core", result.EF.Score, result.EF.BankName, result.EF.Slogan);
        }

        Console.WriteLine($"|{new string('-', 10)}|{new string('-', 10)}|{new string('-', BankNameWidth + 2)}|{new string('-', SloganWidth + 2)}|");
    }

    void PrintRow(string source, double score, string bankName, string slogan)
    {
        Console.Write($"| {source,-8} | {score,-8:F4} | ");

        if (searchType == "Name")
        {
            WriteWithColor(bankName.PadRight(BankNameWidth));
            Console.Write($" | {slogan.PadRight(SloganWidth)} |");
        }
        else
        {
            Console.Write($"{bankName.PadRight(BankNameWidth)} | ");
            WriteWithColor(slogan.PadRight(SloganWidth));
            Console.Write(" |");
        }

        Console.WriteLine();
    }

    void WriteWithColor(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(text);
        Console.ResetColor();
    }
}

internal record EfVectorQueryResult(string BankName, string Slogan, double Score);
