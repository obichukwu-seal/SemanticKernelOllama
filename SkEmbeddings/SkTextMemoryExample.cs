using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Memory;
using System.Text.Json;

namespace SkEmbeddings
{
    internal class SkTextMemoryExample
    {
#pragma warning disable SKEXP0001
        ISemanticTextMemory _memory;
#pragma warning restore SKEXP0001

        public SkTextMemoryExample(IServiceProvider serviceProvider) {
#pragma warning disable SKEXP0001
            _memory = serviceProvider.GetRequiredService<ISemanticTextMemory>();
#pragma warning restore SKEXP0001
        }


        public async Task RunExampleAsync(string memoryCollectionName)
        {
            await StoreMemoryAsync(memoryCollectionName);

            await SearchMemoryAsync( memoryCollectionName, "How do I get started?");

            await SearchMemoryAsync( memoryCollectionName, "Can I build a chat with SK?");


            //await StoreMoreMemoryAsync(memoryCollectionName);

            //await SearchMemoryAsync(memoryCollectionName, "Where can I get Smart TV");

            //await SearchMemoryAsync(memoryCollectionName, "What are organic Shirts");
        }

        private async Task SearchMemoryAsync(string memoryCollectionName, string query)
        {
            Console.WriteLine("\nQuery: " + query + "\n");

            var memoryResults = _memory.SearchAsync(memoryCollectionName, query, limit: 3, minRelevanceScore: 0.55);

            int i = 0;
            await foreach (var memoryResult in memoryResults)
            {
                Console.WriteLine($"Result {++i}:");
                Console.WriteLine("  URL:     : " + memoryResult.Metadata.Id);
                Console.WriteLine("  Title    : " + memoryResult.Metadata.Description);
                Console.WriteLine("  Relevance: " + memoryResult.Relevance);
                Console.WriteLine();
            }

            Console.WriteLine("----------------------");
        }

        private async Task StoreMemoryAsync(string memoryCollectionName)
        {
            /* Store some data in the semantic memory.
             *
             * When using Azure AI Search the data is automatically indexed on write.
             *
             * When using the combination of VolatileStore and Embedding generation, SK takes
             * care of creating and storing the index
             */

            Console.WriteLine("\nAdding some GitHub file URLs and their descriptions to the semantic memory.");
            var githubFiles = SampleData();
            var i = 0;
            foreach (var entry in githubFiles)
            {
                await _memory.SaveReferenceAsync(
                    collection: memoryCollectionName,
                    externalSourceName: "GitHub",
                    externalId: entry.Key,
                    description: entry.Value,
                    text: entry.Value);

                Console.Write($" #{++i} saved.");
            }

            Console.WriteLine("\n----------------------");
        }

        private async Task StoreMoreMemoryAsync(string memoryCollectionName)
        {
            /* Store some data in the semantic memory.
             *
             * When using Azure AI Search the data is automatically indexed on write.
             *
             * When using the combination of VolatileStore and Embedding generation, SK takes
             * care of creating and storing the index
             */

            Console.WriteLine("\nAdding some GitHub file URLs and their descriptions to the semantic memory.");
            
            var factContent = File.ReadAllText("./Facts/product_descriptions_100k.json");
            var products = JsonSerializer.Deserialize<List<ProductItem>>(factContent);

            foreach (var entry in products)
            {
                await _memory.SaveReferenceAsync(
                     collection: memoryCollectionName,
                     externalSourceName: "GitHub",
                     externalId: entry.Id,
                     description: entry.Name,
                     text: entry.Name);
            }

            Console.WriteLine("\n----------------------");
        }


        private static Dictionary<string, string> SampleData()
        {
            return new Dictionary<string, string>
            {
                ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
                    = "README: Installation, getting started, and how to contribute",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
                    = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/00-getting-started.ipynb"]
                    = "Jupyter notebook describing how to get started with the Semantic Kernel",
                ["https://github.com/microsoft/semantic-kernel/tree/main/prompt_template_samples/ChatPlugin/ChatGPT"]
                    = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Memory/VolatileMemoryStore.cs"]
                    = "C# class that defines a volatile embedding store",
            };
        }
    }
}
