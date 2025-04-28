using EfCoreMultiColumnVector.DataAccess;
using EfCoreMultiColumnVector.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

public static class Migrator
{
    public static async Task MigrateAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var embeddingGenerator = scope.ServiceProvider.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        await SeedAsync(dbContext, embeddingGenerator);
    }


#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private static async Task SeedAsync(AppDbContext dbContext, ITextEmbeddingGenerationService embeddingGenerator)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    {
        // Check if the database is empty
        if (!await dbContext.BankInfos.AnyAsync())
        {
            var bankList = BankListJson();
            var banks = JsonSerializer.Deserialize<List<BankInfo>>(bankList, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (banks != null)
            {
                foreach (var bank in banks)
                {
                    // Generate embeddings for the bank name and slogan
                    var nameEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(bank.BankName);
                    var sloganEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(bank.Slogan);
                    // Set the embeddings to the bank object
                    bank.NameEmbedding = new Pgvector.Vector(nameEmbedding);
                    bank.SloganEmbedding = new Pgvector.Vector(sloganEmbedding);
                    // Add the bank to the database
                    await dbContext.BankInfos.AddAsync(bank);
                }
                await dbContext.SaveChangesAsync();
            }
        }
    }

    public static string BankListJson()
    {
        return @"[
          {
            ""bankname"": ""Access Bank"",
            ""slogan"": ""More than banking""
          },
          {
            ""bankname"": ""First Bank of Nigeria"",
            ""slogan"": ""The premier bank in West Africa""
          },
          {
            ""bankname"": ""Guaranty Trust Bank (GTBank)"",
            ""slogan"": ""Everyday, Everyway""
          },
          {
            ""bankname"": ""United Bank for Africa (UBA)"",
            ""slogan"": ""Africa’s Global Bank""
          },
          {
            ""bankname"": ""Zenith Bank"",
            ""slogan"": ""People, Technology, Service""
          },
          {
            ""bankname"": ""Ecobank Nigeria"",
            ""slogan"": ""The pan-African bank""
          },
          {
            ""bankname"": ""Fidelity Bank"",
            ""slogan"": ""We keep our word""
          },
          {
            ""bankname"": ""Stanbic IBTC Bank"",
            ""slogan"": ""Moving Forward""
          },
          {
            ""bankname"": ""Union Bank of Nigeria"",
            ""slogan"": ""Big. Strong. Reliable.""
          },
          {
            ""bankname"": ""Polaris Bank"",
            ""slogan"": ""Your Reliable Banking Partner""
          },
          {
            ""bankname"": ""Keystone Bank"",
            ""slogan"": ""Simply Innovative""
          },
          {
            ""bankname"": ""Wema Bank"",
            ""slogan"": ""Banking for all, with you""
          },
          {
            ""bankname"": ""Sterling Bank"",
            ""slogan"": ""The one-customer bank""
          },
          {
            ""bankname"": ""FCMB (First City Monument Bank)"",
            ""slogan"": ""With you all the way""
          },
          {
            ""bankname"": ""Heritage Bank"",
            ""slogan"": ""Building wealth together""
          },
          {
            ""bankname"": ""Jaiz Bank"",
            ""slogan"": ""The first non-interest bank in Nigeria""
          },
          {
            ""bankname"": ""Providus Bank"",
            ""slogan"": ""Banking made simple""
          },
          {
            ""bankname"": ""Titan Trust Bank"",
            ""slogan"": ""Banking made easy""
          },
          {
            ""bankname"": ""SunTrust Bank"",
            ""slogan"": ""Banking made easy""
          },
          {
            ""bankname"": ""Unity Bank"",
            ""slogan"": ""Together for your good""
          },
          {
            ""bankname"": ""Citibank Nigeria"",
            ""slogan"": ""The Citi never sleeps""
          },
          {
            ""bankname"": ""Standard Chartered Bank Nigeria"",
            ""slogan"": ""Here for good""
          },
          {
            ""bankname"": ""Coronation Merchant Bank"",
            ""slogan"": ""Banking on Excellence""
          },
          {
            ""bankname"": ""Rand Merchant Bank Nigeria"",
            ""slogan"": ""Inspired by Africa""
          },
          {
            ""bankname"": ""FSDH Merchant Bank"",
            ""slogan"": ""Building sustainable value""
          },
          {
            ""bankname"": ""Nova Merchant Bank"",
            ""slogan"": ""Banking redefined""
          },
          {
            ""bankname"": ""Globus Bank"",
            ""slogan"": ""Banking made simple""
          },
          {
            ""bankname"": ""Parallex Bank"",
            ""slogan"": ""Banking with a difference""
          },
          {
            ""bankname"": ""Lotus Bank"",
            ""slogan"": ""Banking with Integrity""
          },
          {
            ""bankname"": ""TAJBank"",
            ""slogan"": ""Non-interest banking at its best""
          }
        ]";
    }
}