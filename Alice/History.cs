// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Fixed-capacity, most-recent-first history of strings (e.g., THAT or INPUT).
/// </summary>
internal sealed class History
{
    private readonly List<string> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="History"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of entries to retain.</param>
    internal History(int capacity)
    {
        Capacity = capacity > 0 ? capacity : 1;
        _items = new List<string>(Capacity);
    }

    /// <summary>
    /// Gets the maximum number of entries retained.
    /// </summary>
    internal int Capacity { get; }

    /// <summary>
    /// Gets the current number of entries in the history.
    /// </summary>
    internal int Count => _items.Count;

    /// <summary>
    /// Pushes a new entry to the head of the history; drops the oldest if capacity is exceeded.
    /// </summary>
    /// <param name="value">The entry to push; <c>null</c> is treated as empty.</param>
    internal void Push(string value)
    {
        // most-recent-first
        _items.Insert(0, value ?? string.Empty);
        if (_items.Count > Capacity)
        {
            _items.RemoveAt(_items.Count - 1);
        }
    }

    /// <summary>
    /// Returns the most recent entry, or an empty string if the history is empty.
    /// </summary>
    /// <returns>The most recent entry or an empty string.</returns>
    internal string PeekOrEmpty()
    {
        return _items.Count == 0 ? string.Empty : _items[0];
    }

    /// <summary>
    /// Gets the entry at the given 1-based index (1 = most recent). Returns empty when out of range.
    /// </summary>
    /// <param name="index1">A 1-based index into the history.</param>
    /// <returns>The entry text at the specified index, or an empty string.</returns>
    internal string At(int index1)
    {
        int i0 = index1 - 1;
        return (i0 >= 0 && i0 < _items.Count) ? _items[i0] : string.Empty;
    }

    /// <summary>
    /// Clears all entries from the history.
    /// </summary>
    internal void Clear() => _items.Clear();
}
