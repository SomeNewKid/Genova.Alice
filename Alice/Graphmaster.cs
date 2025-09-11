// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Graphmaster trie that stores AIML categories along a tokenized path:
/// INPUT tokens, the <c>&lt;THAT&gt;</c> marker and THAT tokens, then the
/// <c>&lt;TOPIC&gt;</c> marker and TOPIC tokens. Provides pattern/THAT/TOPIC
/// matching with ALICE precedence (<c>_</c>, literal, then <c>*</c>).
/// </summary>
internal sealed class Graphmaster
{
    /// <summary>
    /// Literal segment marker token inserted between INPUT and THAT segments.
    /// </summary>
    internal const string ThatSeparator = "<THAT>";

    /// <summary>
    /// Literal segment marker token inserted between THAT and TOPIC segments.
    /// </summary>
    internal const string TopicSeparator = "<TOPIC>";

    /// <summary>
    /// Wildcard token that matches zero or more tokens within a segment.
    /// </summary>
    internal const string StarToken = "*";

    /// <summary>
    /// Wildcard token that matches one or more tokens within a segment.
    /// </summary>
    internal const string UnderToken = "_";

    private readonly NodeMap _root = new();

    /// <summary>
    /// Splits a normalized segment into tokens (space-delimited), dropping empties.
    /// </summary>
    /// <param name="normalized">A normalized INPUT/THAT/TOPIC segment.</param>
    /// <returns>An immutable list of tokens.</returns>
    internal static IReadOnlyList<string> Tokenize(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Adds a fully constructed <see cref="Category"/> to the graph.
    /// </summary>
    /// <param name="category">The category to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="category"/> is <c>null</c>.</exception>
    internal void AddCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        AddCategory(category.Pattern, category.That, category.Topic, category.Template);
    }

    /// <summary>
    /// Adds a category to the graph using normalized components and raw template XML.
    /// </summary>
    /// <param name="pattern">Normalized INPUT pattern.</param>
    /// <param name="that">Normalized THAT qualifier.</param>
    /// <param name="topic">Normalized TOPIC qualifier.</param>
    /// <param name="template">Raw template XML associated with the category.</param>
    internal void AddCategory(string pattern, string that, string topic, string template)
    {
        IReadOnlyList<string> inputTokens = Tokenize(pattern);
        IReadOnlyList<string> thatTokens = Tokenize(that);
        IReadOnlyList<string> topicTokens = Tokenize(topic);

        Path path = Path.FromSegments(inputTokens, thatTokens, topicTokens);

        NodeMap node = _root;
        foreach (string tok in path.Tokens)
        {
            node = node.GetOrAdd(tok);
        }

        node.Category = new Category(pattern, that, topic, template);
    }

    /// <summary>
    /// Attempts to match a normalized (INPUT, THAT, TOPIC) triple and returns the
    /// resulting <see cref="Match"/> with wildcard captures, or <c>null</c> if no match.
    /// </summary>
    /// <param name="normalizedInput">Normalized INPUT sentence.</param>
    /// <param name="normalizedThat">Normalized THAT segment.</param>
    /// <param name="normalizedTopic">Normalized TOPIC segment.</param>
    /// <returns>A <see cref="Match"/> if successful; otherwise <c>null</c>.</returns>
    internal Match? Match(string normalizedInput, string normalizedThat, string normalizedTopic)
    {
        IReadOnlyList<string> inputTokens = Tokenize(normalizedInput);
        IReadOnlyList<string> thatTokens = Tokenize(normalizedThat);
        IReadOnlyList<string> topicTokens = Tokenize(normalizedTopic);

        Path path = Path.FromSegments(inputTokens, thatTokens, topicTokens);
        IReadOnlyList<string> tokens = path.Tokens;

        List<string> stars = [];
        List<string> thatStars = [];
        List<string> topicStars = [];

        Category? category = Dfs(_root, tokens, 0, path.ThatIndex, path.TopicIndex, stars, thatStars, topicStars);
        if (category is null)
        {
            return null;
        }

        Stars s = new ();
        foreach (string v in stars)
        {
            s.AddStar(v);
        }

        foreach (string v in thatStars)
        {
            s.AddThatStar(v);
        }

        foreach (string v in topicStars)
        {
            s.AddTopicStar(v);
        }

        return new Match(category, path, s, normalizedInput, normalizedThat, normalizedTopic);
    }

    private static Category? Dfs(
        NodeMap node,
        IReadOnlyList<string> tokens,
        int index,
        int thatIndex,
        int topicIndex,
        List<string> star,
        List<string> thatStar,
        List<string> topicStar)
    {
        if (index == tokens.Count)
        {
            return node.Category;
        }

        string tok = tokens[index];

        // ----- Segment markers -----
        if (tok == ThatSeparator)
        {
            NodeMap? thatChild = node.GetThatChild();
            if (thatChild is not null)
            {
                Category? r1 = Dfs(thatChild, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r1 is not null)
                {
                    return r1;
                }
            }

            // Boundary STAR(0) fallback: needed so pattern-side '*' can be empty
            NodeMap? starChildHere = node.GetStarChild();
            if (starChildHere is not null)
            {
                // Add empty capture to the previous segment (pattern)
                SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar)
                    ?.Add(string.Empty);

                Category? r2 = Dfs(starChildHere, tokens, index, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r2 is not null)
                {
                    return r2;
                }

                // revert
                RemoveLastIfAny(SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar));
            }

            return null;
        }

        if (tok == TopicSeparator)
        {
            NodeMap? topicChild = node.GetTopicChild();
            if (topicChild is not null)
            {
                Category? r1 = Dfs(topicChild, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r1 is not null)
                {
                    return r1;
                }
            }

            // Boundary STAR(0) fallback: empty capture to THAT segment (previous)
            NodeMap? starChildHere = node.GetStarChild();
            if (starChildHere is not null)
            {
                SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar)
                    ?.Add(string.Empty);

                Category? r2 = Dfs(starChildHere, tokens, index, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r2 is not null)
                {
                    return r2;
                }

                RemoveLastIfAny(SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar));
            }

            return null;
        }

        // ----- 1) '_' consumes one or more tokens within the current segment -----
        NodeMap? underChild = node.GetUnderscoreChild();
        if (underChild is not null)
        {
            int end = SegmentBoundaryIndex(index, thatIndex, topicIndex, tokens.Count);
            for (int take = 1; index + take <= end; take++)
            {
                string capture = JoinTokens(tokens, index, take);
                List<string> list = SelectStarList(index, thatIndex, topicIndex, star, thatStar, topicStar);

                // Record capture (underscore can't be zero-length)
                list.Add(capture);
                Category? r = Dfs(underChild, tokens, index + take, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r is not null)
                {
                    return r;
                }

                list.RemoveAt(list.Count - 1);
            }
        }

        // ----- 2) LITERAL -----
        foreach (KeyValuePair<string, NodeMap> kv in node.GetLiteralChildren())
        {
            if (string.Equals(kv.Key, tok, StringComparison.Ordinal))
            {
                Category? r = Dfs(kv.Value, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r is not null)
                {
                    return r;
                }
            }
        }

        // ----- 3) '*' consumes zero or more tokens within the current segment (greedy/backtrack) -----
        NodeMap? starChild = node.GetStarChild();
        if (starChild is not null)
        {
            int end = SegmentBoundaryIndex(index, thatIndex, topicIndex, tokens.Count);
            bool inThat = index > thatIndex && index < topicIndex;
            bool inTopic = index > topicIndex;

            for (int take = end - index; take >= 0; take--)
            {
                // Special-case: input THAT/TOPIC sentinel '*' should NOT create a capture
                bool sentinelCapture = take == 1 && (inThat || inTopic) && tokens[index] == StarToken;

                if (sentinelCapture)
                {
                    Category? rSent = Dfs(starChild, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                    if (rSent is not null)
                    {
                        return rSent;
                    }

                    continue;
                }

                string capture = take == 0 ? string.Empty : JoinTokens(tokens, index, take);
                List<string> list = SelectStarList(index, thatIndex, topicIndex, star, thatStar, topicStar);
                list.Add(capture);

                Category? r = Dfs(starChild, tokens, index + take, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r is not null)
                {
                    return r;
                }

                list.RemoveAt(list.Count - 1);
            }
        }

        return null;
    }

    private static int SegmentBoundaryIndex(int index, int thatIndex, int topicIndex, int totalCount)
    {
        if (index < thatIndex)
        {
            return thatIndex; // PATTERN segment
        }

        if (index > thatIndex && index < topicIndex)
        {
            return topicIndex; // THAT segment
        }

        return totalCount; // TOPIC segment
    }

    private static List<string> SelectStarList(
        int index,
        int thatIndex,
        int topicIndex,
        List<string> star,
        List<string> thatStar,
        List<string> topicStar)
    {
        if (index < thatIndex)
        {
            return star;
        }

        if (index > thatIndex && index < topicIndex)
        {
            return thatStar;
        }

        return topicStar;
    }

    // For STAR(0) at boundary markers, record capture into the previous segment's list
    private static List<string>? SelectStarListAtBoundary(
        int index,
        int thatIndex,
        int topicIndex,
        List<string> star,
        List<string> thatStar,
        List<string> topicStar)
    {
        if (index == thatIndex)
        {
            return star; // about to cross <THAT>: previous is PATTERN
        }

        if (index == topicIndex)
        {
            return thatStar; // about to cross <TOPIC>: previous is THAT
        }

        return null;
    }

    private static void RemoveLastIfAny(List<string>? list)
    {
        if (list is null)
        {
            return;
        }

        if (list.Count > 0)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    private static string JoinTokens(IReadOnlyList<string> toks, int start, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        if (count == 1)
        {
            return toks[start];
        }

        string[] arr = new string[count];
        for (int i = 0; i < count; i++)
        {
            arr[i] = toks[start + i];
        }

        return string.Join(' ', arr);
    }
}
