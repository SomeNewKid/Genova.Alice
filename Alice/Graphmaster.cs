using System;
using System.Collections.Generic;

namespace Genova.Alice;

internal sealed class Graphmaster
{
    internal const string ThatSeparator = "<THAT>";
    internal const string TopicSeparator = "<TOPIC>";
    internal const string StarToken = "*";
    internal const string UnderToken = "_";

    private readonly NodeMap _root = new();

    internal void AddCategory(Category category)
    {
        if (category is null) throw new ArgumentNullException(nameof(category));
        AddCategory(category.Pattern, category.That, category.Topic, category.Template);
    }

    internal void AddCategory(string pattern, string that, string topic, string template)
    {
        var inputTokens = Tokenize(pattern);
        var thatTokens = Tokenize(that);
        var topicTokens = Tokenize(topic);

        var path = Path.FromSegments(inputTokens, thatTokens, topicTokens);

        var node = _root;
        foreach (var tok in path.Tokens)
        {
            node = node.GetOrAdd(tok);
        }

        node.Category = new Category(pattern, that, topic, template);
    }

    internal Match? Match(string normalizedInput, string normalizedThat, string normalizedTopic)
    {
        var inputTokens = Tokenize(normalizedInput);
        var thatTokens = Tokenize(normalizedThat);
        var topicTokens = Tokenize(normalizedTopic);

        var path = Path.FromSegments(inputTokens, thatTokens, topicTokens);
        var tokens = path.Tokens;

        var stars = new List<string>();
        var thatStars = new List<string>();
        var topicStars = new List<string>();

        var category = Dfs(_root, tokens, 0, path.ThatIndex, path.TopicIndex, stars, thatStars, topicStars);
        if (category is null) return null;

        var s = new Stars();
        foreach (var v in stars) s.AddStar(v);
        foreach (var v in thatStars) s.AddThatStar(v);
        foreach (var v in topicStars) s.AddTopicStar(v);

        return new Match(category, path, s, normalizedInput, normalizedThat, normalizedTopic);
    }

    internal static IReadOnlyList<string> Tokenize(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized)) return Array.Empty<string>();
        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
            return node.Category;

        var tok = tokens[index];

        // ----- Segment markers -----
        if (tok == ThatSeparator)
        {
            var thatChild = node.GetThatChild();
            if (thatChild is not null)
            {
                var r1 = Dfs(thatChild, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r1 is not null) return r1;
            }

            // Boundary STAR(0) fallback: needed so pattern-side '*' can be empty
            var starChildHere = node.GetStarChild();
            if (starChildHere is not null)
            {
                // Add empty capture to the previous segment (pattern)
                SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar)
                    ?.Add(string.Empty);

                var r2 = Dfs(starChildHere, tokens, index, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r2 is not null) return r2;

                // revert
                RemoveLastIfAny(SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar));
            }

            return null;
        }

        if (tok == TopicSeparator)
        {
            var topicChild = node.GetTopicChild();
            if (topicChild is not null)
            {
                var r1 = Dfs(topicChild, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r1 is not null) return r1;
            }

            // Boundary STAR(0) fallback: empty capture to THAT segment (previous)
            var starChildHere = node.GetStarChild();
            if (starChildHere is not null)
            {
                SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar)
                    ?.Add(string.Empty);

                var r2 = Dfs(starChildHere, tokens, index, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r2 is not null) return r2;

                RemoveLastIfAny(SelectStarListAtBoundary(index, thatIndex, topicIndex, star, thatStar, topicStar));
            }

            return null;
        }

        // ----- 1) '_' consumes one or more tokens within the current segment -----
        var underChild = node.GetUnderscoreChild();
        if (underChild is not null)
        {
            int end = SegmentBoundaryIndex(index, thatIndex, topicIndex, tokens.Count);
            for (int take = 1; index + take <= end; take++)
            {
                var capture = JoinTokens(tokens, index, take);
                var list = SelectStarList(index, thatIndex, topicIndex, star, thatStar, topicStar);

                // Record capture (underscore can't be zero-length)
                list.Add(capture);
                var r = Dfs(underChild, tokens, index + take, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r is not null) return r;
                list.RemoveAt(list.Count - 1);
            }
        }

        // ----- 2) LITERAL -----
        foreach (var kv in node.GetLiteralChildren())
        {
            if (string.Equals(kv.Key, tok, StringComparison.Ordinal))
            {
                var r = Dfs(kv.Value, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r is not null) return r;
            }
        }

        // ----- 3) '*' consumes zero or more tokens within the current segment (greedy/backtrack) -----
        var starChild = node.GetStarChild();
        if (starChild is not null)
        {
            int end = SegmentBoundaryIndex(index, thatIndex, topicIndex, tokens.Count);
            bool inThat = index > thatIndex && index < topicIndex;
            bool inTopic = index > topicIndex;

            for (int take = end - index; take >= 0; take--)
            {
                // Special-case: input THAT/TOPIC sentinel '*' should NOT create a capture
                bool sentinelCapture = (take == 1 && (inThat || inTopic) && tokens[index] == StarToken);

                if (sentinelCapture)
                {
                    var rSent = Dfs(starChild, tokens, index + 1, thatIndex, topicIndex, star, thatStar, topicStar);
                    if (rSent is not null) return rSent;
                    continue;
                }

                var capture = take == 0 ? string.Empty : JoinTokens(tokens, index, take);
                var list = SelectStarList(index, thatIndex, topicIndex, star, thatStar, topicStar);
                list.Add(capture);

                var r = Dfs(starChild, tokens, index + take, thatIndex, topicIndex, star, thatStar, topicStar);
                if (r is not null) return r;

                list.RemoveAt(list.Count - 1);
            }
        }

        return null;
    }

    private static int SegmentBoundaryIndex(int index, int thatIndex, int topicIndex, int totalCount)
    {
        if (index < thatIndex) return thatIndex;                        // PATTERN segment
        if (index > thatIndex && index < topicIndex) return topicIndex; // THAT segment
        return totalCount;                                              // TOPIC segment
    }

    private static List<string> SelectStarList(
        int index, int thatIndex, int topicIndex,
        List<string> star, List<string> thatStar, List<string> topicStar)
    {
        if (index < thatIndex) return star;
        if (index > thatIndex && index < topicIndex) return thatStar;
        return topicStar;
    }

    // For STAR(0) at boundary markers, record capture into the previous segment's list
    private static List<string>? SelectStarListAtBoundary(
        int index, int thatIndex, int topicIndex,
        List<string> star, List<string> thatStar, List<string> topicStar)
    {
        if (index == thatIndex) return star;       // about to cross <THAT>: previous is PATTERN
        if (index == topicIndex) return thatStar;  // about to cross <TOPIC>: previous is THAT
        return null;
    }

    private static void RemoveLastIfAny(List<string>? list)
    {
        if (list is null) return;
        if (list.Count > 0) list.RemoveAt(list.Count - 1);
    }

    private static string JoinTokens(IReadOnlyList<string> toks, int start, int count)
    {
        if (count <= 0) return string.Empty;
        if (count == 1) return toks[start];
        var arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = toks[start + i];
        return string.Join(' ', arr);
    }
}
