using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});


#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddAzureAIInferenceChatCompletion(
    modelId: "",
    apiKey: "",
    endpoint: new Uri("")
);
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Services.AddTransient((serviceProvider) =>
{
    return new Kernel(serviceProvider);
});

var host = builder.Build();
await host.StartAsync();

var kernel = host.Services.GetRequiredService<Kernel>();
var chatFunction = kernel.CreateFunctionFromPrompt("You are a helpful assistant.");

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

    var response = kernel.InvokeStreamingAsync(chatFunction, new KernelArguments
    {
        ["input"] = question,
    });

    Console.Write("\nAssistant > ");

    await foreach (var message in response)
    {
        Console.Write(message);
    }

    Console.WriteLine();
}

