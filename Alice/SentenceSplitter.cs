// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Splits user input into sentences according to simple ALICE-style rules.
/// Program D splits on sentence enders (., !, ?); we do the same and return trimmed, non-empty segments.
/// </summary>
internal static class SentenceSplitter
{
    /// <summary>
    /// Splits the specified text into sentences, trimming and omitting empty results.
    /// </summary>
    /// <param name="input">The raw input text (may be <c>null</c> or whitespace).</param>
    /// <returns>A list of sentence strings; possibly empty.</returns>
    internal static IReadOnlyList<string> Split(string? input)
    {
        List<string> results = [];
        if (string.IsNullOrWhiteSpace(input))
        {
            return results;
        }

        ReadOnlySpan<char> span = input.AsSpan();
        int start = 0;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (c == '.' || c == '!' || c == '?')
            {
                // Take the slice [start, i)
                string slice = span.Slice(start, i - start).ToString().Trim();
                if (slice.Length > 0)
                {
                    results.Add(slice);
                }

                // Skip contiguous punctuation like "?!"
                while (i + 1 < span.Length && (span[i + 1] == '.' || span[i + 1] == '!' || span[i + 1] == '?'))
                {
                    i++;
                }

                // Move start to character after the punctuation run
                start = i + 1;
            }
        }

        // Tail
        if (start < span.Length)
        {
            string tail = span.Slice(start).ToString().Trim();
            if (tail.Length > 0)
            {
                results.Add(tail);
            }
        }

        return results;
    }
}
