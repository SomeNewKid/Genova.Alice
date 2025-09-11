// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Represents the token path used by Graphmaster:
/// INPUT tokens, then the <c>&lt;THAT&gt;</c> marker and THAT tokens,
/// then the <c>&lt;TOPIC&gt;</c> marker and TOPIC tokens.
/// </summary>
internal sealed class Path
{
    private readonly List<string> _tokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="Path"/> class with the full token list and segment marker indices.
    /// </summary>
    /// <param name="tokens">Full token sequence including segment markers.</param>
    /// <param name="thatIndex">Index of the <c>&lt;THAT&gt;</c> marker.</param>
    /// <param name="topicIndex">Index of the <c>&lt;TOPIC&gt;</c> marker.</param>
    internal Path(IReadOnlyList<string> tokens, int thatIndex, int topicIndex)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (thatIndex < 0 || thatIndex >= tokens.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(thatIndex));
        }

        if (topicIndex < 0 || topicIndex >= tokens.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(topicIndex));
        }

        if (!string.Equals(tokens[thatIndex], ThatSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException("Tokens[thatIndex] must be <THAT> marker.", nameof(thatIndex));
        }

        if (!string.Equals(tokens[topicIndex], TopicSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException("Tokens[topicIndex] must be <TOPIC> marker.", nameof(topicIndex));
        }

        if (thatIndex >= topicIndex)
        {
            throw new ArgumentException("<THAT> marker must precede <TOPIC> marker.");
        }

        _tokens = [.. tokens];
        ThatIndex = thatIndex;
        TopicIndex = topicIndex;
    }

    /// <summary>
    /// Gets the literal THAT segment separator token: <c>&lt;THAT&gt;</c>.
    /// </summary>
    internal static string ThatSeparator => "<THAT>";

    /// <summary>
    /// Gets the literal TOPIC segment separator token: <c>&lt;TOPIC&gt;</c>.
    /// </summary>
    internal static string TopicSeparator => "<TOPIC>";

    /// <summary>
    /// Gets the full tokenized path: INPUT … <c>&lt;THAT&gt;</c> … <c>&lt;TOPIC&gt;</c> … .
    /// </summary>
    internal IReadOnlyList<string> Tokens => _tokens;

    /// <summary>
    /// Gets the 0-based index of the <c>&lt;THAT&gt;</c> marker within <see cref="Tokens"/>.
    /// </summary>
    internal int ThatIndex { get; }

    /// <summary>
    /// Gets the 0-based index of the <c>&lt;TOPIC&gt;</c> marker within <see cref="Tokens"/>.
    /// </summary>
    internal int TopicIndex { get; }

    /// <summary>
    /// Gets the number of tokens in the INPUT segment (tokens before <c>&lt;THAT&gt;</c>).
    /// </summary>
    internal int InputCount => ThatIndex;

    /// <summary>
    /// Gets the number of tokens in the THAT segment (between <c>&lt;THAT&gt;</c> and <c>&lt;TOPIC&gt;</c>).
    /// </summary>
    internal int ThatCount => TopicIndex - ThatIndex - 1;

    /// <summary>
    /// Gets the number of tokens in the TOPIC segment (after <c>&lt;TOPIC&gt;</c>).
    /// </summary>
    internal int TopicCount => _tokens.Count - TopicIndex - 1;

    /// <summary>
    /// Returns a space-joined representation of the path tokens (for diagnostics).
    /// </summary>
    /// <returns>A string representation of the path.</returns>
    public override string ToString()
    {
        // Join tokens with single spaces for readability (matches tests)
        return string.Join(' ', _tokens);
    }

    /// <summary>
    /// Creates a <see cref="Path"/> from three token segments (INPUT/THAT/TOPIC),
    /// inserting the segment markers in between.
    /// </summary>
    /// <param name="inputTokens">INPUT tokens.</param>
    /// <param name="thatTokens">THAT tokens (may be empty).</param>
    /// <param name="topicTokens">TOPIC tokens (may be empty).</param>
    /// <returns>A constructed <see cref="Path"/> instance.</returns>
    internal static Path FromSegments(
        IReadOnlyList<string> inputTokens,
        IReadOnlyList<string> thatTokens,
        IReadOnlyList<string> topicTokens)
    {
        ArgumentNullException.ThrowIfNull(inputTokens);
        ArgumentNullException.ThrowIfNull(thatTokens);
        ArgumentNullException.ThrowIfNull(topicTokens);

        List<string> list = new (inputTokens.Count + thatTokens.Count + topicTokens.Count + 2);

        // Input segment
        if (inputTokens.Count > 0)
        {
            list.AddRange(inputTokens);
        }

        // <THAT>
        int thatIndex = list.Count;
        list.Add(ThatSeparator);

        // That segment
        if (thatTokens.Count > 0)
        {
            list.AddRange(thatTokens);
        }

        // <TOPIC>
        int topicIndex = list.Count;
        list.Add(TopicSeparator);

        // Topic segment
        if (topicTokens.Count > 0)
        {
            list.AddRange(topicTokens);
        }

        return new Path(list, thatIndex, topicIndex);
    }

    /// <summary>
    /// Gets the INPUT segment tokens.
    /// </summary>
    /// <returns>An immutable list of INPUT tokens.</returns>
    internal IReadOnlyList<string> GetInputTokens()
    {
        if (InputCount == 0)
        {
            return Array.Empty<string>();
        }

        string[] arr = new string[InputCount];
        _tokens.CopyTo(0, arr, 0, InputCount);
        return arr;
    }

    /// <summary>
    /// Gets the THAT segment tokens.
    /// </summary>
    /// <returns>An immutable list of THAT tokens.</returns>
    internal IReadOnlyList<string> GetThatTokens()
    {
        if (ThatCount == 0)
        {
            return Array.Empty<string>();
        }

        string[] arr = new string[ThatCount];
        _tokens.CopyTo(ThatIndex + 1, arr, 0, ThatCount);
        return arr;
    }

    /// <summary>
    /// Gets the TOPIC segment tokens.
    /// </summary>
    /// <returns>An immutable list of TOPIC tokens.</returns>
    internal IReadOnlyList<string> GetTopicTokens()
    {
        if (TopicCount == 0)
        {
            return Array.Empty<string>();
        }

        string[] arr = new string[TopicCount];
        _tokens.CopyTo(TopicIndex + 1, arr, 0, TopicCount);
        return arr;
    }
}
