// =============================================================
// Genova.Alice.Core — Part 2 (Step 3: Implementations)
// Implements Category, Path, Stars, Match following Program D concepts.
// - Internal types & members
// - Path layout: INPUT <THAT> THAT <TOPIC> TOPIC
// - Stars collections with 1-based safe accessors
// =============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Genova.Alice;

/// <summary>
/// Represents a single AIML category: (pattern, that, topic, template).
/// Mirrors the data carried by Program D's Category.
/// </summary>
internal sealed class Category
{
    internal string Pattern { get; }
    internal string That { get; }
    internal string Topic { get; }
    internal string Template { get; }

    internal Category(string pattern, string that, string topic, string template)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        That = that ?? throw new ArgumentNullException(nameof(that));
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }

    /// <summary>
    /// Human-readable signature: "PATTERN &lt;THAT&gt; THAT &lt;TOPIC&gt; TOPIC".
    /// </summary>
    internal string Signature()
    {
        return $"{Pattern} {Path.ThatSeparator} {That} {Path.TopicSeparator} {Topic}";
    }
}

/// <summary>
/// Represents the token path used by Graphmaster:
/// PATTERN tokens, then &lt;THAT&gt;, then THAT tokens, then &lt;TOPIC&gt;, then TOPIC tokens.
/// </summary>
internal sealed class Path
{
    internal static string ThatSeparator => "<THAT>";
    internal static string TopicSeparator => "<TOPIC>";

    private readonly List<string> _tokens;

    /// <summary>All tokens in order: input segment, THEN &lt;THAT&gt;, then that-segment, THEN &lt;TOPIC&gt;, then topic-segment.</summary>
    internal IReadOnlyList<string> Tokens => _tokens;

    /// <summary>Index in Tokens where the &lt;THAT&gt; marker resides (0-based).</summary>
    internal int ThatIndex { get; }

    /// <summary>Index in Tokens where the &lt;TOPIC&gt; marker resides (0-based).</summary>
    internal int TopicIndex { get; }

    /// <summary>Count of tokens in the input (pattern) segment.</summary>
    internal int InputCount => ThatIndex;

    /// <summary>Count of tokens in the that segment.</summary>
    internal int ThatCount => TopicIndex - ThatIndex - 1;

    /// <summary>Count of tokens in the topic segment.</summary>
    internal int TopicCount => _tokens.Count - TopicIndex - 1;

    internal Path(IReadOnlyList<string> tokens, int thatIndex, int topicIndex)
    {
        if (tokens is null) throw new ArgumentNullException(nameof(tokens));
        if (thatIndex < 0 || thatIndex >= tokens.Count) throw new ArgumentOutOfRangeException(nameof(thatIndex));
        if (topicIndex < 0 || topicIndex >= tokens.Count) throw new ArgumentOutOfRangeException(nameof(topicIndex));
        if (!string.Equals(tokens[thatIndex], ThatSeparator, StringComparison.Ordinal))
            throw new ArgumentException("Tokens[thatIndex] must be <THAT> marker.", nameof(thatIndex));
        if (!string.Equals(tokens[topicIndex], TopicSeparator, StringComparison.Ordinal))
            throw new ArgumentException("Tokens[topicIndex] must be <TOPIC> marker.", nameof(topicIndex));
        if (thatIndex >= topicIndex)
            throw new ArgumentException("<THAT> marker must precede <TOPIC> marker.");

        _tokens = new List<string>(tokens);
        ThatIndex = thatIndex;
        TopicIndex = topicIndex;
    }

    /// <summary>
    /// Builds a Path from the three normalized segments.
    /// (Normalization happens earlier; this merely joins with segment markers.)
    /// </summary>
    internal static Path FromSegments(IReadOnlyList<string> inputTokens,
                                      IReadOnlyList<string> thatTokens,
                                      IReadOnlyList<string> topicTokens)
    {
        if (inputTokens is null) throw new ArgumentNullException(nameof(inputTokens));
        if (thatTokens is null) throw new ArgumentNullException(nameof(thatTokens));
        if (topicTokens is null) throw new ArgumentNullException(nameof(topicTokens));

        var list = new List<string>(inputTokens.Count + thatTokens.Count + topicTokens.Count + 2);

        // Input segment
        if (inputTokens.Count > 0) list.AddRange(inputTokens);

        // <THAT>
        int thatIndex = list.Count;
        list.Add(ThatSeparator);

        // That segment
        if (thatTokens.Count > 0) list.AddRange(thatTokens);

        // <TOPIC>
        int topicIndex = list.Count;
        list.Add(TopicSeparator);

        // Topic segment
        if (topicTokens.Count > 0) list.AddRange(topicTokens);

        return new Path(list, thatIndex, topicIndex);
    }

    /// <summary>Returns the tokens of the input (pattern) segment.</summary>
    internal IReadOnlyList<string> GetInputTokens()
    {
        if (InputCount == 0) return Array.Empty<string>();
        var arr = new string[InputCount];
        _tokens.CopyTo(0, arr, 0, InputCount);
        return arr;
    }

    /// <summary>Returns the tokens of the that segment.</summary>
    internal IReadOnlyList<string> GetThatTokens()
    {
        if (ThatCount == 0) return Array.Empty<string>();
        var arr = new string[ThatCount];
        _tokens.CopyTo(ThatIndex + 1, arr, 0, ThatCount);
        return arr;
    }

    /// <summary>Returns the tokens of the topic segment.</summary>
    internal IReadOnlyList<string> GetTopicTokens()
    {
        if (TopicCount == 0) return Array.Empty<string>();
        var arr = new string[TopicCount];
        _tokens.CopyTo(TopicIndex + 1, arr, 0, TopicCount);
        return arr;
    }

    public override string ToString()
    {
        // Join tokens with single spaces for readability (matches tests)
        return string.Join(' ', _tokens);
    }
}

/// <summary>
/// Holds wildcard captures for the current match: star, thatstar, topicstar.
/// Indexing is AIML-style (1-based in public getters); zero-based accessors provided too.
/// Mirrors responsibilities of Program D's Stars/StarBindings.
/// </summary>
internal sealed class Stars
{
    private readonly List<string> _star = new();
    private readonly List<string> _thatStar = new();
    private readonly List<string> _topicStar = new();

    internal int StarCount => _star.Count;
    internal int ThatStarCount => _thatStar.Count;
    internal int TopicStarCount => _topicStar.Count;

    internal Stars() { }

    internal void AddStar(string value) => _star.Add(value ?? string.Empty);
    internal void AddThatStar(string value) => _thatStar.Add(value ?? string.Empty);
    internal void AddTopicStar(string value) => _topicStar.Add(value ?? string.Empty);

    /// <summary>Returns the 1-based star capture; empty string if out of range.</summary>
    internal string StarAt(int index1)
    {
        if (index1 <= 0 || index1 > _star.Count) return string.Empty;
        return _star[index1 - 1];
    }

    /// <summary>Returns the 1-based thatstar capture; empty string if out of range.</summary>
    internal string ThatStarAt(int index1)
    {
        if (index1 <= 0 || index1 > _thatStar.Count) return string.Empty;
        return _thatStar[index1 - 1];
    }

    /// <summary>Returns the 1-based topicstar capture; empty string if out of range.</summary>
    internal string TopicStarAt(int index1)
    {
        if (index1 <= 0 || index1 > _topicStar.Count) return string.Empty;
        return _topicStar[index1 - 1];
    }

    /// <summary>Zero-based access; throws if out of range (for internal use).</summary>
    internal string StarAtZero(int index0)
    {
        if ((uint)index0 >= (uint)_star.Count) throw new ArgumentOutOfRangeException(nameof(index0));
        return _star[index0];
    }

    internal string ThatStarAtZero(int index0)
    {
        if ((uint)index0 >= (uint)_thatStar.Count) throw new ArgumentOutOfRangeException(nameof(index0));
        return _thatStar[index0];
    }

    internal string TopicStarAtZero(int index0)
    {
        if ((uint)index0 >= (uint)_topicStar.Count) throw new ArgumentOutOfRangeException(nameof(index0));
        return _topicStar[index0];
    }
}

/// <summary>
/// Captures the result of a successful match: the Category, Path, and the Stars.
/// Mirrors Program D's Match object responsibilities.
/// </summary>
internal sealed class Match
{
    internal Category Category { get; }
    internal Path Path { get; }
    internal Stars Stars { get; }

    /// <summary>The normalized input that produced this match (for tracing/diagnostics).</summary>
    internal string NormalizedInput { get; }

    /// <summary>The normalized THAT segment used during matching (for tracing).</summary>
    internal string NormalizedThat { get; }

    /// <summary>The normalized TOPIC segment used during matching (for tracing).</summary>
    internal string NormalizedTopic { get; }

    internal Match(Category category, Path path, Stars stars,
                   string normalizedInput, string normalizedThat, string normalizedTopic)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Stars = stars ?? throw new ArgumentNullException(nameof(stars));

        NormalizedInput = normalizedInput ?? string.Empty;
        NormalizedThat = normalizedThat ?? string.Empty;
        NormalizedTopic = normalizedTopic ?? string.Empty;
    }

    /// <summary>Convenience passthroughs for template processing (1-based).</summary>
    internal string Star(int index1) => Stars.StarAt(index1);
    internal string ThatStar(int index1) => Stars.ThatStarAt(index1);
    internal string TopicStar(int index1) => Stars.TopicStarAt(index1);
}
