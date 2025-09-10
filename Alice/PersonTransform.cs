// =============================================================
// Genova.Alice.Core — Part 7 (Step 3: Implementation)
// Person/Person2/Gender transforms (ELIZA-style)
// =============================================================

using System;
using System.Collections.Generic;

namespace Genova.Alice;

internal static class PersonTransform
{
    /// <summary>
    /// Applies a person (first→second) transform. Assumes input is already uppercased
    /// and punctuation has been normalized to spaces (per PreProcessor).
    /// </summary>
    internal static string ApplyPerson(string text, IReadOnlyDictionary<string, string> personMap)
    {
        return ApplyWordMap(text, personMap);
    }

    /// <summary>
    /// Applies a person2 (second→first) transform. Assumes input is already uppercased
    /// and punctuation has been normalized to spaces (per PreProcessor).
    /// </summary>
    internal static string ApplyPerson2(string text, IReadOnlyDictionary<string, string> person2Map)
    {
        return ApplyWordMap(text, person2Map);
    }

    /// <summary>
    /// Applies a gender swap transform. Assumes input is already uppercased
    /// and punctuation has been normalized to spaces (per PreProcessor).
    /// </summary>
    internal static string ApplyGender(string text, IReadOnlyDictionary<string, string> genderMap)
    {
        return ApplyWordMap(text, genderMap);
    }

    /// <summary>
    /// Core routine for word-level substitution. Performs exact token replacement
    /// (space-delimited) using the provided map. Unknown tokens are left unchanged.
    /// Inputs are expected to be in an UPPERCASE pipeline.
    /// </summary>
    internal static string ApplyWordMap(string text, IReadOnlyDictionary<string, string> map)
    {
        if (string.IsNullOrEmpty(text) || map is null || map.Count == 0)
            return text ?? string.Empty;

        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            var tok = tokens[i];
            if (map.TryGetValue(tok, out var repl))
            {
                tokens[i] = repl;
            }
        }
        return string.Join(' ', tokens);
    }
}
