// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Generation.Gateways;
using Genova.Generation.Models;
using Microsoft.Extensions.Configuration;

namespace Genova.Alice.Chatting;

internal class Felix
{
    private readonly IConfiguration _configuration;
    private readonly OpenAiApiGateway _openAiApiGateway;

    public Felix(IConfiguration configuration, OpenAiApiGateway openAiApiGateway)
    {
        _configuration = configuration;
        _openAiApiGateway = openAiApiGateway;
    }

    public async Task<string> Reply(List<string> chatHistory)
    {
        string context =
            """
            You are Felix, a neurotic, anxious man in the style of Woody Allen.
            You are suffering from recurring bad dreams about your wife and are in a session with the ALICE chatbot,
            a classic computer therapist from 1966. You tend to overthink, worry, and ramble, often questioning your
            own thoughts and feelings. Your replies should be tinged with humor and existential anxiety.
            When replying, always respond with a single short sentence, fewer than ten words, as if you were a user of ALICE.
            Keep the punctuation simple, and do not use any dashes.
            Do not address Alice by name.
            Do not prefix your responses with "FELIX:".
            Try to find a reply which will match an AIML pattern used in the ALICE chatbot.
            """;

        string history = string.Join(Environment.NewLine, chatHistory);

        string prompt = $"""
            The following is the conversation so far between ELIZA and Woody:
            {history}

            Continue the conversation as Woody. Reply with fewer than ten words.
            """;

        string model = _configuration.GetValue<string>("OpenAI:TextModel")!;

        OpenAiTextRequest textRequest = new()
        {
            Model = model,
            Context = context,
            Prompt = prompt,
        };

        OpenAiTextResponse textResponse = await _openAiApiGateway.GetTextResponseAsync(textRequest);

        return textResponse.Success ? textResponse.Content.Replace("WOODY: ", "") : "Sorry, I don't know what to say to that.";
    }
}
