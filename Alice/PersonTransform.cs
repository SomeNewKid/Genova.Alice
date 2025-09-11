// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Word-level ELIZA-style transforms for person and gender swaps.
/// Assumes inputs have been uppercased and punctuation normalized by the preprocessor.
/// </summary>
internal static class PersonTransform
{
    /// <summary>
    /// Applies a person (first→second) transform to the specified text.
    /// </summary>
    /// <param name="text">The normalized input text (typically uppercase, space-delimited tokens).</param>
    /// <param name="personMap">Substitution map for first→second person tokens.</param>
    /// <returns>The transformed text.</returns>
    internal static string ApplyPerson(string text, IReadOnlyDictionary<string, string> personMap)
    {
        return ApplyWordMap(text, personMap);
    }

    /// <summary>
    /// Applies a person2 (second→first) transform to the specified text.
    /// </summary>
    /// <param name="text">The normalized input text (typically uppercase, space-delimited tokens).</param>
    /// <param name="person2Map">Substitution map for second→first person tokens.</param>
    /// <returns>The transformed text.</returns>
    internal static string ApplyPerson2(string text, IReadOnlyDictionary<string, string> person2Map)
    {
        return ApplyWordMap(text, person2Map);
    }

    /// <summary>
    /// Applies a gender swap transform to the specified text.
    /// </summary>
    /// <param name="text">The normalized input text (typically uppercase, space-delimited tokens).</param>
    /// <param name="genderMap">Substitution map for gendered tokens.</param>
    /// <returns>The transformed text.</returns>
    internal static string ApplyGender(string text, IReadOnlyDictionary<string, string> genderMap)
    {
        return ApplyWordMap(text, genderMap);
    }

    /// <summary>
    /// Performs exact token-level substitution over a space-delimited string using the provided map.
    /// Unknown tokens are left unchanged.
    /// </summary>
    /// <param name="text">The normalized input text (space-delimited tokens).</param>
    /// <param name="map">The substitution map to apply.</param>
    /// <returns>The transformed text.</returns>
    internal static string ApplyWordMap(string text, IReadOnlyDictionary<string, string> map)
    {
        if (string.IsNullOrEmpty(text) || map is null || map.Count == 0)
        {
            return text ?? string.Empty;
        }

        string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            string tok = tokens[i];
            if (map.TryGetValue(tok, out string? repl))
            {
                tokens[i] = repl;
            }
        }

        return string.Join(' ', tokens);
    }
}
