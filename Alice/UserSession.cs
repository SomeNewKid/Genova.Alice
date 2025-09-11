// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Per-user session state: user predicates, current topic, and THAT/INPUT histories.
/// </summary>
internal sealed class UserSession
{
    private string _topic = "*";

    /// <summary>
    /// Initializes a new  instance of the <see cref="UserSession"/> class for the specified user and bot context.
    /// </summary>
    /// <param name="userId">Stable user identifier for this session.</param>
    /// <param name="botContext">The bot context providing configuration such as history depth.</param>
    /// <param name="inputHistoryCapacity">Maximum number of prior inputs to retain.</param>
    internal UserSession(string userId, Bot botContext, int inputHistoryCapacity = 16)
    {
        ArgumentNullException.ThrowIfNull(botContext);
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));

        Predicates = new Predicates();
        _topic = "*";
        ThatHistory = new History(botContext.ThatHistoryDepth > 0 ? botContext.ThatHistoryDepth : 8);
        InputHistory = new History(inputHistoryCapacity > 0 ? inputHistoryCapacity : 16);
    }

    /// <summary>
    /// Gets the stable identifier of the user associated with this session.
    /// </summary>
    internal string UserId { get; }

    /// <summary>
    /// Gets the session's predicate collection (for <c>&lt;set&gt;</c> / <c>&lt;get&gt;</c>).
    /// </summary>
    internal Predicates Predicates { get; }

    /// <summary>
    /// Gets or sets the current topic name (normalized). When set to blank, it becomes <c>"*"</c>.
    /// </summary>
    internal string Topic
    {
        get => _topic;
        set => _topic = string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();
    }

    /// <summary>
    /// Gets the most recent bot reply (THAT), or <c>"*"</c> if none exists.
    /// </summary>
    internal string That
    {
        get
        {
            string t = ThatHistory.PeekOrEmpty();
            return string.IsNullOrEmpty(t) ? "*" : t;
        }
    }

    /// <summary>
    /// Gets the history of prior bot replies (THAT), most-recent-first.
    /// </summary>
    internal History ThatHistory { get; }

    /// <summary>
    /// Gets the history of prior user inputs, most-recent-first.
    /// </summary>
    internal History InputHistory { get; }

    /// <summary>
    /// Appends a new bot reply (THAT) to the session history.
    /// </summary>
    /// <param name="that">The reply text to store.</param>
    internal void PushThat(string that)
    {
        ThatHistory.Push(that ?? string.Empty);
    }

    /// <summary>
    /// Appends a new user input to the session history.
    /// </summary>
    /// <param name="input">The input text to store.</param>
    internal void PushInput(string input)
    {
        InputHistory.Push(input ?? string.Empty);
    }

    /// <summary>
    /// Gets the most recent <c>&lt;that&gt;</c> value or <c>"*"</c> when no replies exist.
    /// </summary>
    /// <returns>The latest THAT value, or <c>"*"</c> if none.</returns>
    internal string GetThatOrStar()
    {
        string t = ThatHistory.PeekOrEmpty();
        return string.IsNullOrEmpty(t) ? "*" : t;
    }
}
