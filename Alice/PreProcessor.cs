// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;

namespace Genova.Alice;

/// <summary>
/// Classic ALICE-style preprocessing:
/// uppercasing, punctuation→space (preserving '*' and '_'),
/// token-level “Normal” substitutions, whitespace collapsing, and sentence splitting.
/// </summary>
internal sealed class PreProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreProcessor"/> class with the specified substitution tables.
    /// </summary>
    /// <param name="substitutions">Substitution tables to use (required).</param>
    internal PreProcessor(SubstitutionTables substitutions)
    {
        Substitutions = substitutions ?? throw new ArgumentNullException(nameof(substitutions));
    }

    /// <summary>
    /// Gets the substitution tables used by this preprocessor.
    /// </summary>
    internal SubstitutionTables Substitutions { get; }

    /// <summary>
    /// Runs the full normalization pipeline for user input (before matching).
    /// </summary>
    /// <param name="input">Raw user input (may be <c>null</c>).</param>
    /// <returns>Normalized text suitable for matching.</returns>
    internal string NormalizeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Uppercase first (Program D tradition)
        string up = input.ToUpperInvariant();
        up = ReplacePunctuationWithSpace(up);
        up = ApplyNormalSubstitutions(up);
        up = CollapseWhitespace(up);
        return up.Trim();
    }

    /// <summary>
    /// Normalizes AIML-side pattern/that/topic fields.
    /// </summary>
    /// <param name="pattern">Pattern text (may be <c>null</c>).</param>
    /// <returns>Normalized pattern text.</returns>
    internal string NormalizePattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return string.Empty;
        }

        string up = pattern.ToUpperInvariant();
        up = ReplacePunctuationWithSpace(up);
        up = CollapseWhitespace(up);
        return up.Trim();
    }

    /// <summary>
    /// Normalizes a THAT field.
    /// </summary>
    /// <param name="that">THAT text (may be <c>null</c>).</param>
    /// <returns>Normalized THAT text.</returns>
    internal string NormalizeThat(string? that) => NormalizePattern(that);

    /// <summary>
    /// Normalizes a TOPIC field.
    /// </summary>
    /// <param name="topic">TOPIC text (may be <c>null</c>).</param>
    /// <returns>Normalized TOPIC text.</returns>
    internal string NormalizeTopic(string? topic) => NormalizePattern(topic);

    /// <summary>
    /// Splits the input into sentence units using <see cref="SentenceSplitter"/>.
    /// </summary>
    /// <param name="input">Raw input (may be <c>null</c>).</param>
    /// <returns>Trimmed, non-empty sentences.</returns>
    internal IReadOnlyList<string> SplitSentences(string? input)
    {
        return SentenceSplitter.Split(input);
    }

    /// <summary>
    /// Applies token-level “Normal” substitutions to already-uppercased, punctuation-sanitized text.
    /// </summary>
    /// <param name="text">Text to transform.</param>
    /// <returns>Transformed text.</returns>
    internal string ApplyNormalSubstitutions(string text)
    {
        if (string.IsNullOrEmpty(text) || Substitutions.Normal.Count == 0)
        {
            return text ?? string.Empty;
        }

        // Token-level replacement: split on spaces, map exact tokens.
        string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string token = parts[i];
            if (Substitutions.Normal.TryGetValue(token, out string? replacement))
            {
                parts[i] = replacement;
            }
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// Replaces punctuation with spaces, preserving wildcard characters (*) and (_).
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <returns>Text with punctuation mapped to spaces.</returns>
    internal string ReplacePunctuationWithSpace(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        Span<char> buffer = text.Length <= 1024
            ? stackalloc char[text.Length]
            : new char[text.Length];

        int j = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '*' || c == '_')
            {
                buffer[j++] = c;
            }
            else
            {
                buffer[j++] = ' ';
            }
        }

        return new string(buffer[..j]);
    }

    /// <summary>
    /// Collapses runs of whitespace to a single space and trims the result.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <returns>Whitespace-collapsed text.</returns>
    internal string CollapseWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        StringBuilder sb = new (text.Length);
        bool prevSpace = false;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                prevSpace = false;
            }
        }

        // Trim possible leading/trailing single space added
        string result = sb.ToString();
        return result.Length > 0 ? result.Trim() : result;
    }

    /// <summary>
    /// Converts text to Title Case using the invariant culture.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <returns>Title-cased text.</returns>
    internal string ToTitleCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    /// <summary>
    /// Converts text to Sentence case (first character upper, remainder lower).
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <returns>Sentence-cased text.</returns>
    internal string ToSentenceCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string lower = text.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + (lower.Length > 1 ? lower[1..] : string.Empty);
    }
}
