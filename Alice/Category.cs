// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Represents a single AIML category comprised of a normalized <c>pattern</c>,
/// optional <c>that</c> and <c>topic</c> qualifiers, and the raw template XML.
/// </summary>
internal sealed class Category
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Category"/> class with normalized fields and template XML.
    /// </summary>
    /// <param name="pattern">Normalized INPUT pattern.</param>
    /// <param name="that">Normalized THAT qualifier.</param>
    /// <param name="topic">Normalized TOPIC qualifier.</param>
    /// <param name="template">Raw template XML.</param>
    internal Category(string pattern, string that, string topic, string template)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        That = that ?? throw new ArgumentNullException(nameof(that));
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }

    /// <summary>
    /// Gets the normalized pattern text (INPUT segment) for this category.
    /// </summary>
    internal string Pattern { get; }

    /// <summary>
    /// Gets the normalized THAT qualifier for this category.
    /// </summary>
    internal string That { get; }

    /// <summary>
    /// Gets the normalized TOPIC qualifier for this category.
    /// </summary>
    internal string Topic { get; }

    /// <summary>
    /// Gets the raw (unparsed) template XML associated with the category.
    /// </summary>
    internal string Template { get; }

    /// <summary>
    /// Returns a human-readable signature in the form <c>PATTERN &lt;THAT&gt; THAT &lt;TOPIC&gt; TOPIC</c>.
    /// </summary>
    /// <returns>A signature string for diagnostics.</returns>
    internal string Signature()
    {
        return $"{Pattern} {Path.ThatSeparator} {That} {Path.TopicSeparator} {Topic}";
    }
}
