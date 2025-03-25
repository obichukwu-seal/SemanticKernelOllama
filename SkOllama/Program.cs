// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// create httpclient with a timeout of 5 minutes pointing to ollama default endpoint

var endpoint = new Uri("http://localhost:11434");
var modelId = "llama3.2-vision:11b";
var httpClient = new HttpClient
{
    BaseAddress = endpoint,
    Timeout = TimeSpan.FromMinutes(5)
};

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var kernel = Kernel.CreateBuilder()
    .AddOllamaChatCompletion(modelId: modelId, httpClient: httpClient)
    .Build();
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Get the chat completion service
var chatService = kernel.GetRequiredService<IChatCompletionService>();
// Initialize chat history
var history = new ChatHistory("You are a helpful assistant.");

while (true)
{
    Console.Write("You: ");
    var userMessage = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userMessage))
    {
        break;
    }

    history.AddUserMessage(userMessage);
    var response = await chatService.GetChatMessageContentAsync(history);
    Console.WriteLine($"\nBot: {response}\n");

    history.Add(response);
}