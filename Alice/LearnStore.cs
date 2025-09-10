// =============================================================
// Genova.Alice.Core — Part 9
// LearnStore (in-memory implementation)
// =============================================================

using System;
using System.Collections.Generic;

namespace Genova.Alice;

internal sealed class LearnStore
{
    internal sealed record LearnedCategory(
        string Pattern,
        string That,
        string Topic,
        string TemplateXml,
        string? SourceName
    );

    private readonly List<LearnedCategory> _items = new();

    internal int Count => _items.Count;

    internal IReadOnlyList<LearnedCategory> All => _items.AsReadOnly();

    internal void Add(string pattern, string that, string topic, string templateXml, string? sourceName = null)
    {
        // Store exactly what we’re given; normalization is handled upstream (TemplateProcessor/PreProcessor).
        _items.Add(new LearnedCategory(
            pattern ?? string.Empty,
            that ?? string.Empty,
            topic ?? string.Empty,
            templateXml ?? string.Empty,
            sourceName
        ));
    }

    internal void Clear() => _items.Clear();
}
