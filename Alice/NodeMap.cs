// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Graphmaster node whose children are keyed by token.
/// Terminal nodes may carry a <see cref="Category"/> when a full path terminates here.
/// Mirrors the responsibilities of Program D's Nodemapper.
/// </summary>
internal sealed class NodeMap
{
    private readonly Dictionary<string, NodeMap> _children = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the category stored at this node when a full path terminates here; otherwise <c>null</c>.
    /// </summary>
    internal Category? Category { get; set; }

    /// <summary>
    /// Attempts to retrieve the child node for the specified token.
    /// </summary>
    /// <param name="token">The exact token key to look up.</param>
    /// <param name="child">When this method returns, contains the child node if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a child with the given token exists; otherwise <c>false</c>.</returns>
    internal bool TryGet(string token, out NodeMap? child)
    {
        bool ok = _children.TryGetValue(token, out NodeMap? found);
        child = found;
        return ok;
    }

    /// <summary>
    /// Gets the existing child for the specified token or creates and returns a new one.
    /// </summary>
    /// <param name="token">The exact token key of the desired child.</param>
    /// <returns>The existing or newly created child node.</returns>
    internal NodeMap GetOrAdd(string token)
    {
        if (!_children.TryGetValue(token, out NodeMap? child))
        {
            child = new NodeMap();
            _children[token] = child;
        }

        return child;
    }

    /// <summary>
    /// Returns all literal children (excluding wildcard <c>"_"</c>, <c>"*"</c>, and segment markers
    /// <c>&lt;THAT&gt;</c> / <c>&lt;TOPIC&gt;</c>) for literal token matching precedence.
    /// </summary>
    /// <returns>An enumeration of literal-token children.</returns>
    internal IEnumerable<KeyValuePair<string, NodeMap>> GetLiteralChildren()
    {
        foreach (var kv in _children)
        {
            string k = kv.Key;
            if (!string.Equals(k, Graphmaster.UnderToken, StringComparison.Ordinal) &&
                !string.Equals(k, Graphmaster.StarToken, StringComparison.Ordinal) &&
                !string.Equals(k, Graphmaster.ThatSeparator, StringComparison.Ordinal) &&
                !string.Equals(k, Graphmaster.TopicSeparator, StringComparison.Ordinal))
            {
                yield return kv;
            }
        }
    }

    /// <summary>
    /// Gets the special underscore (<c>"_"</c>) child, which matches one or more tokens within a segment, if present.
    /// </summary>
    /// <returns>The underscore child node if present; otherwise <c>null</c>.</returns>
    internal NodeMap? GetUnderscoreChild()
    {
        _children.TryGetValue(Graphmaster.UnderToken, out NodeMap? n);
        return n;
    }

    /// <summary>
    /// Gets the special star (<c>"*"</c>) child, which matches zero or more tokens within a segment, if present.
    /// </summary>
    /// <returns>The star child node if present; otherwise <c>null</c>.</returns>
    internal NodeMap? GetStarChild()
    {
        _children.TryGetValue(Graphmaster.StarToken, out NodeMap? n);
        return n;
    }

    /// <summary>
    /// Gets the special <c>&lt;THAT&gt;</c> segment delimiter child, if present.
    /// </summary>
    /// <returns>The THAT child node if present; otherwise <c>null</c>.</returns>
    internal NodeMap? GetThatChild()
    {
        _children.TryGetValue(Graphmaster.ThatSeparator, out NodeMap? n);
        return n;
    }

    /// <summary>
    /// Gets the special <c>&lt;TOPIC&gt;</c> segment delimiter child, if present.
    /// </summary>
    /// <returns>The TOPIC child node if present; otherwise <c>null</c>.</returns>
    internal NodeMap? GetTopicChild()
    {
        _children.TryGetValue(Graphmaster.TopicSeparator, out NodeMap? n);
        return n;
    }
}
