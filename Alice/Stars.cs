// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Holds wildcard captures for the current match: <c>&lt;star/&gt;</c>,
/// <c>&lt;thatstar/&gt;</c>, and <c>&lt;topicstar/&gt;</c>.
/// Provides 1-based getters matching AIML semantics.
/// </summary>
internal sealed class Stars
{
    private readonly List<string> _star = [];
    private readonly List<string> _thatStar = [];
    private readonly List<string> _topicStar = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Stars"/> class.
    /// </summary>
    internal Stars() { }

    /// <summary>
    /// Gets the number of INPUT <c>star</c> captures.
    /// </summary>
    internal int StarCount => _star.Count;

    /// <summary>
    /// Gets the number of THAT <c>thatstar</c> captures.
    /// </summary>
    internal int ThatStarCount => _thatStar.Count;

    /// <summary>
    /// Gets the number of TOPIC <c>topicstar</c> captures.
    /// </summary>
    internal int TopicStarCount => _topicStar.Count;

    /// <summary>
    /// Appends an INPUT <c>star</c> capture value (may be empty).
    /// </summary>
    /// <param name="value">The captured text.</param>
    internal void AddStar(string value) => _star.Add(value ?? string.Empty);

    /// <summary>
    /// Appends a THAT <c>thatstar</c> capture value (may be empty).
    /// </summary>
    /// <param name="value">The captured text.</param>
    internal void AddThatStar(string value) => _thatStar.Add(value ?? string.Empty);

    /// <summary>
    /// Appends a TOPIC <c>topicstar</c> capture value (may be empty).
    /// </summary>
    /// <param name="value">The captured text.</param>
    internal void AddTopicStar(string value) => _topicStar.Add(value ?? string.Empty);

    /// <summary>
    /// Gets the 1-based INPUT <c>star</c> capture at the specified index; returns empty if out of range.
    /// </summary>
    /// <param name="index1">1-based capture index.</param>
    /// <returns>The capture text or empty.</returns>
    internal string StarAt(int index1)
    {
        if (index1 <= 0 || index1 > _star.Count)
        {
            return string.Empty;
        }

        return _star[index1 - 1];
    }

    /// <summary>
    /// Gets the 1-based THAT <c>thatstar</c> capture at the specified index; returns empty if out of range.
    /// </summary>
    /// <param name="index1">1-based capture index.</param>
    /// <returns>The capture text or empty.</returns>
    internal string ThatStarAt(int index1)
    {
        if (index1 <= 0 || index1 > _thatStar.Count)
        {
            return string.Empty;
        }

        return _thatStar[index1 - 1];
    }

    /// <summary>
    /// Gets the 1-based TOPIC <c>topicstar</c> capture at the specified index; returns empty if out of range.
    /// </summary>
    /// <param name="index1">1-based capture index.</param>
    /// <returns>The capture text or empty.</returns>
    internal string TopicStarAt(int index1)
    {
        if (index1 <= 0 || index1 > _topicStar.Count)
        {
            return string.Empty;
        }

        return _topicStar[index1 - 1];
    }

    /// <summary>
    /// Gets the 0-based INPUT <c>star</c> capture; throws if out of range.
    /// </summary>
    /// <param name="index0">0-based capture index.</param>
    /// <returns>The capture text.</returns>
    internal string StarAtZero(int index0)
    {
        if ((uint)index0 >= (uint)_star.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index0));
        }

        return _star[index0];
    }

    /// <summary>
    /// Gets the 0-based THAT <c>thatstar</c> capture; throws if out of range.
    /// </summary>
    /// <param name="index0">0-based capture index.</param>
    /// <returns>The capture text.</returns>
    internal string ThatStarAtZero(int index0)
    {
        if ((uint)index0 >= (uint)_thatStar.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index0));
        }

        return _thatStar[index0];
    }

    /// <summary>
    /// Gets the 0-based TOPIC <c>topicstar</c> capture; throws if out of range.
    /// </summary>
    /// <param name="index0">0-based capture index.</param>
    /// <returns>The capture text.</returns>
    internal string TopicStarAtZero(int index0)
    {
        if ((uint)index0 >= (uint)_topicStar.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index0));
        }

        return _topicStar[index0];
    }
}
