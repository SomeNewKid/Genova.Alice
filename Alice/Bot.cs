// =============================================================
// Genova.Alice.Core — Part 5 (Step 3: Implementations)
// Bot, Predicates, BotProperties, UserSession, History
// =============================================================

using System;
using System.Collections.Generic;

namespace Genova.Alice;

/// <summary>
/// Global bot context: properties (<bot name="..."/>), substitutions, and defaults.
/// </summary>
internal sealed class Bot
{
    internal BotProperties Properties { get; }
    internal SubstitutionTables Substitutions { get; }
    internal int ThatHistoryDepth { get; }

    internal Bot(BotProperties? properties = null, SubstitutionTables? substitutions = null, int thatHistoryDepth = 8)
    {
        Properties = properties ?? new BotProperties();
        Substitutions = substitutions ?? SubstitutionTables.CreateEmpty();
        ThatHistoryDepth = thatHistoryDepth > 0 ? thatHistoryDepth : 8;
    }
}

/// <summary>
/// Case-insensitive map of user predicates (for <set name="x"/> and <get name="x"/>).
/// </summary>
internal sealed class Predicates
{
    private readonly Dictionary<string, string> _map =
        new(StringComparer.OrdinalIgnoreCase);

    internal int Count => _map.Count;

    internal Predicates() { }

    internal void Set(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        _map[name] = value ?? string.Empty;
    }

    internal bool TryGet(string name, out string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            value = null!;
            return false;
        }
        var ok = _map.TryGetValue(name, out var v);
        value = ok ? v! : null!;
        return ok;
    }

    internal string GetOrEmpty(string name)
    {
        return _map.TryGetValue(name, out var v) ? v : string.Empty;
    }

    internal void Clear() => _map.Clear();
}

/// <summary>
/// Bot-level properties (for <bot name="..."/> lookups). Case-insensitive keys.
/// </summary>
internal sealed class BotProperties
{
    private readonly Dictionary<string, string> _map =
        new(StringComparer.OrdinalIgnoreCase);

    internal int Count => _map.Count;

    internal BotProperties() { }

    internal void Set(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        _map[name] = value ?? string.Empty;
    }

    internal bool TryGet(string name, out string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            value = null!;
            return false;
        }
        var ok = _map.TryGetValue(name, out var v);
        value = ok ? v! : null!;
        return ok;
    }

    internal string GetOrEmpty(string name)
    {
        return _map.TryGetValue(name, out var v) ? v : string.Empty;
    }

    internal void Clear() => _map.Clear();
}

/// <summary>
/// Fixed-capacity, most-recent-first history of strings. Used for THAT and INPUT stacks.
/// </summary>
internal sealed class History
{
    private readonly int _capacity;
    private readonly List<string> _items;

    internal int Capacity => _capacity;
    internal int Count => _items.Count;

    internal History(int capacity)
    {
        _capacity = capacity > 0 ? capacity : 1;
        _items = new List<string>(_capacity);
    }

    /// <summary>Push a new entry; drops the oldest when exceeding capacity.</summary>
    internal void Push(string value)
    {
        // most-recent-first
        _items.Insert(0, value ?? string.Empty);
        if (_items.Count > _capacity)
            _items.RemoveAt(_items.Count - 1);
    }

    /// <summary>Returns the most recent item, or empty string if none.</summary>
    internal string PeekOrEmpty()
    {
        return _items.Count == 0 ? string.Empty : _items[0];
    }

    /// <summary>
    /// 1-based indexing from most-recent (1) to oldest (Count).
    /// Returns empty string if out of range.
    /// </summary>
    internal string At(int index1)
    {
        int i0 = index1 - 1;
        return (i0 >= 0 && i0 < _items.Count) ? _items[i0] : string.Empty;
    }

    internal void Clear() => _items.Clear();
}

/// <summary>
/// Per-user session state: predicates, topic, that/input histories.
/// </summary>
internal sealed class UserSession
{
    private string _topic = "*";

    internal string UserId { get; }
    internal Predicates Predicates { get; }
    internal string Topic
    {
        get => _topic;
        set => _topic = string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();
    }

    internal string That
    {
        get
        {
            var t = ThatHistory.PeekOrEmpty();
            return string.IsNullOrEmpty(t) ? "*" : t;
        }
    }

    internal History ThatHistory { get; }
    internal History InputHistory { get; }

    internal UserSession(string userId, Bot botContext, int inputHistoryCapacity = 16)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        if (botContext is null) throw new ArgumentNullException(nameof(botContext));

        Predicates = new Predicates();
        _topic = "*";
        ThatHistory = new History(botContext.ThatHistoryDepth > 0 ? botContext.ThatHistoryDepth : 8);
        InputHistory = new History(inputHistoryCapacity > 0 ? inputHistoryCapacity : 16);
    }

    /// <summary>Push a new THAT (bot reply) into history.</summary>
    internal void PushThat(string that)
    {
        ThatHistory.Push(that ?? string.Empty);
    }

    /// <summary>Push a new INPUT (user utterance) into history.</summary>
    internal void PushInput(string input)
    {
        InputHistory.Push(input ?? string.Empty);
    }

    /// <summary>Returns most recent THAT or "*".</summary>
    internal string GetThatOrStar()
    {
        var t = ThatHistory.PeekOrEmpty();
        return string.IsNullOrEmpty(t) ? "*" : t;
    }
}
