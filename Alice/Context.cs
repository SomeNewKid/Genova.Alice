// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Template evaluation ambient context (session, match, recursion depth, and processor).
/// </summary>
internal sealed class Context
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Context"/> class with session, match, depth, and processor.
    /// </summary>
    /// <param name="processor">The processor instance.</param>
    /// <param name="session">The user session context.</param>
    /// <param name="match">The current match context.</param>
    /// <param name="depth">The current recursion depth.</param>
    internal Context(TemplateProcessor processor, UserSession session, Match match, int depth)
    {
        Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Match = match ?? throw new ArgumentNullException(nameof(match));
        Depth = depth;
    }

    /// <summary>
    /// Gets the session providing predicates, THAT/TOPIC, and histories.
    /// </summary>
    internal UserSession Session { get; }

    /// <summary>
    /// Gets the current match (category, path, and wildcard captures).
    /// </summary>
    internal Match Match { get; }

    /// <summary>
    /// Gets the current recursion depth used for SRAI limiting.
    /// </summary>
    internal int Depth { get; }

    /// <summary>
    /// Gets the processor instance performing the evaluation.
    /// </summary>
    internal TemplateProcessor Processor { get; }
}
