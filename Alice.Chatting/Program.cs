// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Genova.Generation;
using Genova.Generation.Gateways;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Genova.Alice.Chatting;

/// <summary>
/// The entry point for the Alice Chatting application.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Only used rarely when testing Alice.")]
internal class Program
{
    /// <summary>
    /// The main entry point for the Alice Chatting application.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Convention of the Main method.")]
    static async Task Main(string[] args)
    {
        IConfiguration configuration = BuildConfiguration();

        string? directory = configuration.GetValue<string>("OutputDirectory")!;
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("Output directory is not set in the configuration.");
        }

        if (!Directory.Exists(directory))
        {
            throw new InvalidOperationException("Output directory does not exist.");
        }

        string fullPath = Path.Combine(directory, "eliza.txt");

        ServiceProvider serviceProvider = BuildServiceProvider(configuration);
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        GenerationOptions generationOptions = new()
        {
            OpenAiApiKey = GetApiKey(),
        };
        OpenAiApiGateway openAiApiGateway = new(generationOptions, httpClientFactory);

        Alice eliza = new();

        Felix felix = new(configuration, openAiApiGateway);

        int turns = 12; // Set desired number of turns
        await StartChat(eliza, felix, turns, fullPath);
    }

    private static async Task StartChat(Alice eliza, Felix felix, int turns, string fullPath)
    {
        var transcript = new List<string>();

        string elizaReply = "Hello.";

        string elizaLine = $"ALICE: {elizaReply}";
        transcript.Add(elizaLine);
        Console.WriteLine(elizaLine);

        for (int i = 0; i < turns; i++)
        {
            // Felix replies to Alice, using the full transcript
            string felixReply = await felix.Reply(transcript);
            string felixLine = $"FELIX: {felixReply}";
            transcript.Add(felixLine);
            Console.WriteLine(felixLine);

            // Alice replies to Felix, using only Felix's last reply
            elizaReply = eliza.GetResponse(felixReply);
            elizaLine = $"ALICE: {elizaReply}";
            transcript.Add(elizaLine);
            Console.WriteLine(elizaLine);
        }

        // Write the entire transcript to the file once, after the conversation
        File.WriteAllLines(fullPath, transcript);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        ServiceCollection serviceCollection = new();
        ConfigureServices(serviceCollection, configuration);
        return serviceCollection.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddHttpClient();
    }

    private static string GetApiKey()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("API key is not set in the environment variables.");
        }
        return apiKey;
    }
}
