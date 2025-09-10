// =============================================================
// Genova.Alice.Core — Part 8 (Step 3: Implementations)
// Engine (chat loop & SRAI) and optional REPL
// =============================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace Genova.Alice;

internal sealed class Engine
{
    internal Bot Bot { get; }
    internal Graphmaster Graph { get; }
    internal PreProcessor Pre { get; }
    internal TemplateProcessor Templates { get; }

    internal Engine(Bot bot, Graphmaster graph, PreProcessor pre, TemplateProcessor? templates = null)
    {
        Bot = bot ?? throw new ArgumentNullException(nameof(bot));
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        Pre = pre ?? throw new ArgumentNullException(nameof(pre));

        // Wire TemplateProcessor to this engine's SRAI entry point
        Templates = templates ?? new TemplateProcessor(Bot, Pre, Srai, rng: new Random());
    }

    internal string Respond(string input, UserSession session)
    {
        if (session is null) throw new ArgumentNullException(nameof(session));
        if (input is null) input = string.Empty;

        // Record the raw input (as typed), once per user message
        session.PushInput(input);

        var sentences = Pre.SplitSentences(input);
        if (sentences.Count == 0)
            sentences = new List<string> { input };

        var outputs = new List<string>();

        foreach (var sentence in sentences)
        {
            // Normalize pipeline for matching
            var normInput = Pre.NormalizeInput(sentence);
            var normThat = Pre.NormalizeThat(session.ThatHistory.PeekOrEmpty());
            var normTopic = Pre.NormalizeTopic(session.Topic);

            var match = Graph.Match(normInput, normThat, normTopic);
            if (match is null)
                continue;

            var reply = Templates.Process(match.Category.Template, session, match, depth: 0);
            if (!string.IsNullOrWhiteSpace(reply))
            {
                outputs.Add(reply);
                // Update THAT context with the bot's reply
                session.PushThat(reply);
            }
        }

        string response = string.Join(" ", outputs);
        response = CollapseWhitespace(response);
        response = CorrectResponse(response);
        return response;
    }

    private static string CorrectResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return string.Empty;

        while (response.EndsWith(" ?"))
        {
            response = response.Substring(0, response.Length - 2) + "?";
        }

        return response;
    }

    // Inside Genova.Alice.Core.Engine
    private static string CollapseWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new System.Text.StringBuilder(text.Length);
        bool inSpace = false;

        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!inSpace)
                {
                    sb.Append(' ');
                    inSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                inSpace = false;
            }
        }

        // Trim a single leading/trailing space if present
        if (sb.Length > 0 && sb[0] == ' ') sb.Remove(0, 1);
        if (sb.Length > 0 && sb[^1] == ' ') sb.Remove(sb.Length - 1, 1);

        return sb.ToString();
    }

    internal string Srai(string input, UserSession session, int depth)
    {
        if (session is null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // (Optional) guard against runaway depth if invoked directly
        if (depth >= TemplateProcessor.DefaultMaxDepth) return string.Empty;

        var normInput = Pre.NormalizeInput(input);
        var normThat = Pre.NormalizeThat(session.ThatHistory.PeekOrEmpty());
        var normTopic = Pre.NormalizeTopic(session.Topic);

        var match = Graph.Match(normInput, normThat, normTopic);
        if (match is null) return string.Empty;

        // Single match/render cycle; DO NOT push InputHistory and
        // do not push THAT here (caller Respond will push once per sentence)
        var reply = Templates.Process(match.Category.Template, session, match, depth);
        return reply ?? string.Empty;
    }
}

internal static class Repl
{
    internal static void Run(Engine engine, UserSession session)
    {
        if (engine is null) throw new ArgumentNullException(nameof(engine));
        if (session is null) throw new ArgumentNullException(nameof(session));

        Console.WriteLine("Genova.Alice REPL — type 'quit' to exit.\n");
        for (; ; )
        {
            Console.Write("You> ");
            var line = Console.ReadLine();
            if (line is null) break;
            if (line.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

            var reply = engine.Respond(line, session);
            Console.WriteLine($"Bot> {reply}");
        }
    }
}
