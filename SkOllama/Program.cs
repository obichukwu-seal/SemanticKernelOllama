// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = Host.CreateApplicationBuilder(args);
ConfigureServices(builder.Services);
var host = builder.Build();
await host.StartAsync();

// Get the chat completion service
var chatService = host.Services.GetRequiredService<IChatCompletionService>();
await RunChatAsync(chatService);

async Task RunChatAsync(IChatCompletionService chatService)
{
    var history = new ChatHistory("You are a helpful assistant.");

    history.AddUserMessage(new ChatMessageContentItemCollection
    {
        new TextContent("What is this image?"), 
        new ImageContent(new Uri("https://upload.wikimedia.org/wikipedia/commons/6/62/Panthera_tigris_sumatran_subspecies.jpg"))
    });

    while (true)
    {
        Console.Write("You: ");
        var userMessage = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userMessage))
            break;

        history.AddUserMessage(userMessage);
        var response = await chatService.GetChatMessageContentAsync(history);
        Console.WriteLine($"\nBot: {response}\n");

        history.Add(response);
    }
}

void ConfigureServices(IServiceCollection services)
{
    var endpoint = new Uri("http://localhost:11434");
    var modelId = "llama3.2-vision:11b";
    var httpClient = new HttpClient
    {
        BaseAddress = endpoint,
        Timeout = TimeSpan.FromMinutes(5)
    };

    #pragma warning disable SKEXP0070
    services.AddOllamaChatCompletion(
        modelId: modelId,
        httpClient: httpClient
    );
    #pragma warning restore SKEXP0070

    services.AddTransient((serviceProvider) =>
    {
        return new Kernel(serviceProvider);
    });
}