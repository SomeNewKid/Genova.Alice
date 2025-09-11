// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Bot-level persona properties used by <c>&lt;bot name="…"/&gt;</c>. Keys are case-insensitive.
/// </summary>
internal sealed class BotProperties
{
    private readonly Dictionary<string, string> _map =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="BotProperties"/> class.
    /// </summary>
    internal BotProperties()
    {
    }

    /// <summary>
    /// Gets the number of properties stored.
    /// </summary>
    internal int Count => _map.Count;

    /// <summary>
    /// Sets a property to the specified value. If the name is blank, the call is ignored.
    /// </summary>
    /// <param name="name">Property name (case-insensitive).</param>
    /// <param name="value">Property value; <c>null</c> is treated as empty.</param>
    internal void Set(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        _map[name] = value ?? string.Empty;
    }

    /// <summary>
    /// Attempts to get a property value.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if the property exists; otherwise <c>false</c>.</returns>
    internal bool TryGet(string name, out string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            value = null!;
            return false;
        }

        bool ok = _map.TryGetValue(name, out string? v);
        value = ok ? v! : null!;
        return ok;
    }

    /// <summary>
    /// Gets the property value or an empty string if it does not exist.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <returns>The value if present; otherwise an empty string.</returns>
    internal string GetOrEmpty(string name)
    {
        return _map.TryGetValue(name, out string? v) ? v : string.Empty;
    }

    /// <summary>
    /// Removes all properties from the collection.
    /// </summary>
    internal void Clear() => _map.Clear();
}
