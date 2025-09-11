// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Global bot context containing persona properties (used by <c>&lt;bot name="…"/&gt;</c>),
/// substitution tables, and default configuration values (e.g., THAT history depth).
/// </summary>
internal sealed class Bot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Bot"/> class with optional
    /// persona properties and substitution tables.
    /// </summary>
    /// <param name="properties">Optional bot properties; if <c>null</c>, an empty bag is used.</param>
    /// <param name="substitutions">Optional substitution tables; if <c>null</c>, an empty set is used.</param>
    /// <param name="thatHistoryDepth">Maximum size of the per-session <c>&lt;that&gt;</c> history.</param>
    internal Bot(BotProperties? properties = null, SubstitutionTables? substitutions = null, int thatHistoryDepth = 8)
    {
        Properties = properties ?? new BotProperties();
        Substitutions = substitutions ?? SubstitutionTables.CreateEmpty();
        ThatHistoryDepth = thatHistoryDepth > 0 ? thatHistoryDepth : 8;
    }

    /// <summary>
    /// Gets the bot-level property bag used to resolve <c>&lt;bot name="…"/&gt;</c> lookups.
    /// Keys are case-insensitive.
    /// </summary>
    internal BotProperties Properties { get; }

    /// <summary>
    /// Gets the substitution tables used during preprocessing and template transforms.
    /// </summary>
    internal SubstitutionTables Substitutions { get; }

    /// <summary>
    /// Gets the maximum number of prior bot replies retained in the per-session
    /// <c>&lt;that&gt;</c> history (most-recent-first).
    /// </summary>
    internal int ThatHistoryDepth { get; }
}
