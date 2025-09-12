// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Genova.Common.Attributes;

namespace Genova.Alice;

/// <summary>
/// High-level façade for interacting with the ALICE chatbot.
/// Constructs the runtime, loads embedded AIML resources, and
/// exposes a single-turn API to obtain a response for user input.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by the RustyKane.com website.")]
public sealed class Alice
{
    private readonly Engine _engine;
    private readonly UserSession _session;

    /// <summary>
    /// Initializes a new instance of the <see cref="Alice"/> class,
    /// loading reductions, core AIML, and bot properties from embedded resources,
    /// and preparing a session for conversation.
    /// </summary>
    public Alice()
    {
        // Build the runtime (no Engine yet)
        AliceRuntimeBuilder builder = new (
            botProperties: new BotProperties(),
            substitutions: SubstitutionTables.CreateClassicDefaults(),
            thatHistoryDepth: 8);

        Assembly assembly = typeof(Alice).Assembly;
        string embeddedFilesFolder = GetEmbeddedFilesFolder(assembly);

        string[] reductionFiles =
        [
            "reduction0.safe.aiml", // core normalization & reductions (apply first)
            "reduction1.safe.aiml", // additional general reductions
            "reduction2.safe.aiml", // includes HI -> <srai>HELLO</srai> and similar bridges
            "reduction3.safe.aiml", // further normalization/redirect patterns
            "reduction4.safe.aiml", // final layer of safe reductions
            "reduction.names.aiml", // name-related reductions (e.g., "my name is …")
        ];

        string[] coreFiles =
        [
            "that.aiml",            // enables <that>-conditioned categories
            "bot_profile.aiml",     // <bot name="…"/> lookups (bot properties)
            "client_profile.aiml",  // user predicates (e.g., “what is my name”)
            "client.aiml",          // user predicate readers like "MY NAME"
            "atomic.aiml",          // base/common categories (e.g., HOW ARE YOU)
            "salutations.aiml",     // greetings & farewells
            "iu.aiml",              // greetings & farewells
            "inquiry.aiml",         // “who are you?”, “how are you?”, etc.
            "ai.aiml",              // AI identity / self-description
            "bot.aiml",             // ARE YOU A ROBOT (unconstrained replies used by reductions)
            "alice.aiml",           // broad, classic ALICE persona content
            "personality.aiml",     // persona refinements & opinions
            "emotion.aiml",         // feelings/empathy responses
            "humor.aiml",           // jokes & playful replies
            "computers.aiml",       // computer/tech small talk
            "continuation.aiml",    // “go on / tell me more” style prompts
            "gossip.aiml",          // minimal, to resolve GOSSIP
            "date.aiml",            // date and time questions
            "default.aiml",         // catch-alls & safe fallbacks
            "updates.aiml",         // custom reductions
            "fallback.aiml",        // catch anything else (load last)
        ];

        foreach (var reductionFile in reductionFiles)
        {
            builder.LoadReduction(
                () => typeof(AimlLoader).Assembly.GetManifestResourceStream($"{embeddedFilesFolder}{reductionFile}") !,
                reductionFile);
        }

        foreach (string coreFile in coreFiles)
        {
            builder.LoadCoreAiml(
                () => typeof(AimlLoader).Assembly.GetManifestResourceStream($"{embeddedFilesFolder}{coreFile}") !,
                coreFile);
        }

        builder.LoadBotProperties(
            () => typeof(AimlLoader).Assembly.GetManifestResourceStream($"{embeddedFilesFolder}bot.properties") !);

        if (string.IsNullOrWhiteSpace(builder.Bot.Properties.GetOrEmpty("name")))
        {
            builder.Bot.Properties.Set("name", "ALICE");
        }

        _engine = builder.CreateEngine();

        _session = new UserSession("user-1", builder.Bot);
    }

    /// <summary>
    /// Processes a single user utterance and returns ALICE's reply.
    /// This method performs normalization, matching, template evaluation,
    /// and updates the conversational context (e.g., <c>&lt;that&gt;</c>, predicates).
    /// </summary>
    /// <param name="input">The raw user input text.</param>
    /// <returns>The chatbot's response for the given input.</returns>
    public string GetResponse(string input)
    {
        return _engine.Respond(input, _session);
    }

    private static string GetEmbeddedFilesFolder(Assembly assembly)
    {
        string? name = assembly.FullName?.Split(',')[0];
        string folder = name + ".Data.";
        return folder;
    }
}
