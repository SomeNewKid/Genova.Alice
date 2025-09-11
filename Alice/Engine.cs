// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Genova.Alice;

/// <summary>
/// Chat engine that orchestrates preprocessing, pattern matching, and template evaluation.
/// Splits user input into sentences, matches each against the AIML graph, and renders replies.
/// </summary>
internal sealed class Engine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Engine"/> class and wires SRAI recursion.
    /// </summary>
    /// <param name="bot">The bot context.</param>
    /// <param name="graph">The category graph (Graphmaster).</param>
    /// <param name="pre">The preprocessor for normalization.</param>
    /// <param name="templates">
    /// Optional template processor; when <c>null</c>, a default instance is created
    /// that invokes <see cref="Srai(string, UserSession, int)"/> for recursion.
    /// </param>
    internal Engine(Bot bot, Graphmaster graph, PreProcessor pre, TemplateProcessor? templates = null)
    {
        Bot = bot ?? throw new ArgumentNullException(nameof(bot));
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        Pre = pre ?? throw new ArgumentNullException(nameof(pre));

        // Wire TemplateProcessor to this engine's SRAI entry point
        Templates = templates ?? new TemplateProcessor(Bot, Pre, Srai, rng: new Random());
    }

    /// <summary>
    /// Gets the bot context (persona properties, substitutions, and configuration).
    /// </summary>
    internal Bot Bot { get; }

    /// <summary>
    /// Gets the Graphmaster instance used for pattern / THAT / TOPIC matching.
    /// </summary>
    internal Graphmaster Graph { get; }

    /// <summary>
    /// Gets the preprocessor used for normalization (case, punctuation, substitutions).
    /// </summary>
    internal PreProcessor Pre { get; }

    /// <summary>
    /// Gets the template processor used to evaluate AIML templates.
    /// </summary>
    internal TemplateProcessor Templates { get; }

    /// <summary>
    /// Processes a user utterance end-to-end: splits into sentences, matches each,
    /// evaluates templates, updates THAT history, and returns the concatenated reply.
    /// </summary>
    /// <param name="input">Raw user input text.</param>
    /// <param name="session">The user session containing predicates and histories.</param>
    /// <returns>The final reply text (may be empty if nothing matched).</returns>
    internal string Respond(string input, UserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (input is null)
        {
            input = string.Empty;
        }

        // Record the raw input (as typed), once per user message
        session.PushInput(input);

        IReadOnlyList<string> sentences = Pre.SplitSentences(input);
        if (sentences.Count == 0)
        {
            sentences = new List<string> { input };
        }

        List<string> outputs = [];

        foreach (string sentence in sentences)
        {
            // Normalize pipeline for matching
            string normInput = Pre.NormalizeInput(sentence);
            string normThat = Pre.NormalizeThat(session.ThatHistory.PeekOrEmpty());
            string normTopic = Pre.NormalizeTopic(session.Topic);

            Match? match = Graph.Match(normInput, normThat, normTopic);
            if (match is null)
            {
                continue;
            }

            string reply = Templates.Process(match.Category.Template, session, match, depth: 0);
            if (!string.IsNullOrWhiteSpace(reply))
            {
                outputs.Add(reply);
                session.PushThat(reply);
            }
        }

        string response = string.Join(" ", outputs);
        response = CollapseWhitespace(response);
        response = CorrectResponse(response);
        return response;
    }

    /// <summary>
    /// Resolves an SRAI (symbolic reduction) by matching a single text against the graph
    /// using the current THAT/TOPIC context and evaluating the resulting template.
    /// Does not modify the input history or push THAT.
    /// </summary>
    /// <param name="input">The text to re-ask (SRAI target).</param>
    /// <param name="session">The user session providing context.</param>
    /// <param name="depth">The current recursion depth (used for limiting).</param>
    /// <returns>The rendered reply for the SRAI target, or empty if no match.</returns>
    internal string Srai(string input, UserSession session, int depth)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // (Optional) guard against runaway depth if invoked directly
        if (depth >= TemplateProcessor.DefaultMaxDepth)
        {
            return string.Empty;
        }

        string normInput = Pre.NormalizeInput(input);
        string normThat = Pre.NormalizeThat(session.ThatHistory.PeekOrEmpty());
        string normTopic = Pre.NormalizeTopic(session.Topic);

        Match? match = Graph.Match(normInput, normThat, normTopic);
        if (match is null)
        {
            return string.Empty;
        }

        // Single match/render cycle; DO NOT push InputHistory and
        // do not push THAT here (caller Respond will push once per sentence)
        string reply = Templates.Process(match.Category.Template, session, match, depth);
        return reply ?? string.Empty;
    }

    private static string CorrectResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return string.Empty;
        }

        while (response.EndsWith(" ?"))
        {
            response = string.Concat(response.AsSpan(0, response.Length - 2), "?");
        }

        return response;
    }

    // Inside Genova.Alice.Core.Engine
    private static string CollapseWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder sb = new (text.Length);
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
        if (sb.Length > 0 && sb[0] == ' ')
        {
            sb.Remove(0, 1);
        }

        if (sb.Length > 0 && sb[^1] == ' ')
        {
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }
}
