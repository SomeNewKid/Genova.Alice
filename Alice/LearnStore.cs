// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// In-memory registry of categories learned at runtime via the AIML <c>&lt;learn&gt;</c> tag.
/// Entries are stored exactly as provided by the template processor (post-normalization).
/// </summary>
internal sealed class LearnStore
{
    private readonly List<LearnedCategory> _items = [];

    /// <summary>
    /// Gets the number of learned categories currently stored.
    /// </summary>
    internal int Count => _items.Count;

    /// <summary>
    /// Gets a read-only view of all learned categories in insertion order.
    /// </summary>
    internal IReadOnlyList<LearnedCategory> All => _items.AsReadOnly();

    /// <summary>
    /// Adds a learned category to the store.
    /// The provided values are expected to be normalized upstream by the preprocessor.
    /// </summary>
    /// <param name="pattern">Normalized INPUT pattern.</param>
    /// <param name="that">Normalized THAT qualifier.</param>
    /// <param name="topic">Normalized TOPIC qualifier.</param>
    /// <param name="templateXml">Raw template XML associated with the category.</param>
    /// <param name="sourceName">Optional logical source identifier (e.g., filename) for diagnostics.</param>
    internal void Add(string pattern, string that, string topic, string templateXml, string? sourceName = null)
    {
        // Store exactly what we’re given; normalization is handled upstream (TemplateProcessor/PreProcessor).
        _items.Add(new LearnedCategory(
            pattern ?? string.Empty,
            that ?? string.Empty,
            topic ?? string.Empty,
            templateXml ?? string.Empty,
            sourceName));
    }

    /// <summary>
    /// Removes all learned categories from the store.
    /// </summary>
    internal void Clear() => _items.Clear();
}
