// =============================================================
// Genova.Alice.Core — Part 1 (Step 3: Implementations)
// Follows Program D v4.1.5 behavior where applicable for preprocessing.
// - All types/members are internal
// - Case-insensitive maps via StringComparer.OrdinalIgnoreCase
// - Uppercasing + punctuation-to-space (preserve '*' and '_')
// - Word-level Normal substitutions
// - Whitespace collapse
// - Basic sentence splitting on . ! ?  (trimmed, non-empty)
// =============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Genova.Alice;

/// <summary>
/// Holds all substitution maps used during preprocessing and templating.
/// Keys are treated case-insensitively (OrdinalIgnoreCase).
/// </summary>
internal sealed class SubstitutionTables
{
    internal Dictionary<string, string> Normal { get; }
    internal Dictionary<string, string> Person { get; }
    internal Dictionary<string, string> Person2 { get; }
    internal Dictionary<string, string> Gender { get; }

    internal SubstitutionTables(
        Dictionary<string, string>? normal = null,
        Dictionary<string, string>? person = null,
        Dictionary<string, string>? person2 = null,
        Dictionary<string, string>? gender = null)
    {
        // Defensive copies into case-insensitive dictionaries
        Normal = normal is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                  : new Dictionary<string, string>(normal, StringComparer.OrdinalIgnoreCase);
        Person = person is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                  : new Dictionary<string, string>(person, StringComparer.OrdinalIgnoreCase);
        Person2 = person2 is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                  : new Dictionary<string, string>(person2, StringComparer.OrdinalIgnoreCase);
        Gender = gender is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                  : new Dictionary<string, string>(gender, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a new instance with empty substitution maps (case-insensitive).
    /// </summary>
    internal static SubstitutionTables CreateEmpty()
    {
        return new SubstitutionTables();
    }

    /// <summary>
    /// Returns a new instance seeded with classic ALICE-style defaults (minimal set).
    /// This is intentionally small; the full reduction tables are integrated later.
    /// </summary>
    internal static SubstitutionTables CreateClassicDefaults()
    {
        var t = CreateEmpty();

        // Normal (contractions → expanded). Store as UPPERCASE for consistency;
        // PreProcessor uppercases before applying.
        t.Normal["I'M"] = "I AM";
        t.Normal["YOU'RE"] = "YOU ARE";
        t.Normal["CAN'T"] = "CANNOT";
        t.Normal["WON'T"] = "WILL NOT";
        t.Normal["AREN'T"] = "ARE NOT";
        t.Normal["ISN'T"] = "IS NOT";

        // Person: first → second person (subset sufficient for demos/tests)
        t.Person["I"] = "YOU";
        t.Person["ME"] = "YOU";
        t.Person["MY"] = "YOUR";
        t.Person["MINE"] = "YOURS";
        t.Person["MYSELF"] = "YOURSELF";
        t.Person["AM"] = "ARE";

        // Person2: second → first person
        t.Person2["YOU"] = "I";
        t.Person2["YOUR"] = "MY";
        t.Person2["YOURS"] = "MINE";
        t.Person2["YOURSELF"] = "MYSELF";
        t.Person2["ARE"] = "AM";

        // Gender swaps (both directions so ContainsKey("she") passes in tests)
        t.Gender["HE"] = "SHE";
        t.Gender["HIM"] = "HER";
        t.Gender["HIS"] = "HER";
        t.Gender["HIMSELF"] = "HERSELF";
        t.Gender["SHE"] = "HE";
        t.Gender["HER"] = "HIM";
        t.Gender["HERS"] = "HIS";
        t.Gender["HERSELF"] = "HIMSELF";

        return t;
    }
}

/// <summary>
/// Splits user input into sentences according to simple ALICE-style rules.
/// Program D splits on sentence enders (., !, ?); we do the same and return trimmed, non-empty segments.
/// </summary>
internal static class SentenceSplitter
{
    internal static IReadOnlyList<string> Split(string? input)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(input))
            return results;

        ReadOnlySpan<char> span = input.AsSpan();
        int start = 0;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (c == '.' || c == '!' || c == '?')
            {
                // Take the slice [start, i)
                var slice = span.Slice(start, i - start).ToString().Trim();
                if (slice.Length > 0) results.Add(slice);
                // Skip contiguous punctuation like "?!"
                while (i + 1 < span.Length && (span[i + 1] == '.' || span[i + 1] == '!' || span[i + 1] == '?'))
                    i++;
                // Move start to character after the punctuation run
                start = i + 1;
            }
        }

        // Tail
        if (start < span.Length)
        {
            var tail = span.Slice(start).ToString().Trim();
            if (tail.Length > 0) results.Add(tail);
        }

        return results;
    }
}

/// <summary>
/// Classic ALICE-style preprocessing:
/// - Uppercasing
/// - Punctuation → space (preserving '*' and '_')
/// - Normal substitutions (token-based)
/// - Whitespace collapsing
/// - Sentence splitting (via SentenceSplitter)
/// </summary>
internal sealed class PreProcessor
{
    internal SubstitutionTables Substitutions { get; }

    internal PreProcessor(SubstitutionTables substitutions)
    {
        Substitutions = substitutions ?? throw new ArgumentNullException(nameof(substitutions));
    }

    /// <summary>
    /// Full input normalization pipeline for user input (before matching).
    /// </summary>
    internal string NormalizeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Uppercase first (Program D tradition)
        var up = input.ToUpperInvariant();
        up = ReplacePunctuationWithSpace(up);
        up = ApplyNormalSubstitutions(up);
        up = CollapseWhitespace(up);
        return up.Trim();
    }

    /// <summary>
    /// Normalization used for AIML-side pattern/that/topic fields.
    /// (Same as input normalization; provided for clarity and potential divergence.)
    /// </summary>
    internal string NormalizePattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return string.Empty;

        var up = pattern.ToUpperInvariant();
        up = ReplacePunctuationWithSpace(up);
        up = CollapseWhitespace(up);
        return up.Trim();
    }

    internal string NormalizeThat(string? that) => NormalizePattern(that);
    internal string NormalizeTopic(string? topic) => NormalizePattern(topic);

    /// <summary>
    /// Returns sentences obtained from the input using SentenceSplitter.
    /// (Raw split — do not uppercase/normalize here; caller controls the flow.)
    /// </summary>
    internal IReadOnlyList<string> SplitSentences(string? input)
    {
        return SentenceSplitter.Split(input);
    }

    /// <summary>
    /// Applies "Normal" substitutions as token-level replacements, case-insensitive.
    /// Assumes the input has already been uppercased and punctuation→space handled.
    /// </summary>
    internal string ApplyNormalSubstitutions(string text)
    {
        if (string.IsNullOrEmpty(text) || Substitutions.Normal.Count == 0)
            return text ?? string.Empty;

        // Token-level replacement: split on spaces, map exact tokens.
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var token = parts[i];
            if (Substitutions.Normal.TryGetValue(token, out var replacement))
            {
                parts[i] = replacement;
            }
        }
        return string.Join(' ', parts);
    }

    /// <summary>
    /// Replaces punctuation with spaces, preserving '*' and '_' for AIML wildcards.
    /// Keeps letters, digits, whitespace; everything else becomes a single space.
    /// </summary>
    internal string ReplacePunctuationWithSpace(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

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
    /// Collapses runs of whitespace to a single space and trims ends.
    /// </summary>
    internal string CollapseWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        bool prevSpace = false;

        foreach (var c in text)
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
        var result = sb.ToString();
        return result.Length > 0 ? result.Trim() : result;
    }

    /// <summary>
    /// Utility: Title Case (InvariantCulture); used by template transforms later.
    /// </summary>
    internal string ToTitleCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    /// <summary>
    /// Utility: Sentence case (first char upper, rest lower); simplistic but effective.
    /// </summary>
    internal string ToSentenceCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var lower = text.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + (lower.Length > 1 ? lower[1..] : string.Empty);
    }
}
