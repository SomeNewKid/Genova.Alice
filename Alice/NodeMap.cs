using System;
using System.Collections.Generic;

namespace Genova.Alice;

/// <summary>
/// Node map for Graphmaster (children keyed by token).
/// Leaves carry a Category reference.
/// Mirrors Program D's Nodemapper responsibilities.
/// </summary>
internal sealed class NodeMap
{
    private readonly Dictionary<string, NodeMap> _children = new(StringComparer.Ordinal);

    /// <summary>
    /// The category stored at this node (if any) when a full path terminates here.
    /// </summary>
    internal Category? Category { get; set; }

    /// <summary>
    /// Returns the child node for the given token if present.
    /// </summary>
    internal bool TryGet(string token, out NodeMap? child)
    {
        var ok = _children.TryGetValue(token, out var found);
        child = found;
        return ok;
    }

    /// <summary>
    /// Returns an existing child for the given token or creates one.
    /// </summary>
    internal NodeMap GetOrAdd(string token)
    {
        if (!_children.TryGetValue(token, out var child))
        {
            child = new NodeMap();
            _children[token] = child;
        }
        return child;
    }

    /// <summary>
    /// Returns all literal children (excludes wildcard '_' and '*' and segment markers).
    /// Helpful for implementing precedence (try '_' first, then literals, then '*').
    /// </summary>
    internal IEnumerable<KeyValuePair<string, NodeMap>> GetLiteralChildren()
    {
        foreach (var kv in _children)
        {
            var k = kv.Key;
            if (!string.Equals(k, Graphmaster.UnderToken, StringComparison.Ordinal) &&
                !string.Equals(k, Graphmaster.StarToken, StringComparison.Ordinal) &&
                !string.Equals(k, Graphmaster.ThatSeparator, StringComparison.Ordinal) &&
                !string.Equals(k, Graphmaster.TopicSeparator, StringComparison.Ordinal))
            {
                yield return kv;
            }
        }
    }

    /// <summary>Returns the special '_' (underscore) child if present.</summary>
    internal NodeMap? GetUnderscoreChild()
    {
        _children.TryGetValue(Graphmaster.UnderToken, out var n);
        return n;
    }

    /// <summary>Returns the special '*' (star) child if present.</summary>
    internal NodeMap? GetStarChild()
    {
        _children.TryGetValue(Graphmaster.StarToken, out var n);
        return n;
    }

    /// <summary>Returns the special &lt;THAT&gt; child if present.</summary>
    internal NodeMap? GetThatChild()
    {
        _children.TryGetValue(Graphmaster.ThatSeparator, out var n);
        return n;
    }

    /// <summary>Returns the special &lt;TOPIC&gt; child if present.</summary>
    internal NodeMap? GetTopicChild()
    {
        _children.TryGetValue(Graphmaster.TopicSeparator, out var n);
        return n;
    }
}
