// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Receives callbacks for each AIML <c>&lt;category&gt;</c> discovered by the loader.
/// Implementations typically add categories into a runtime graph or collect diagnostics.
/// </summary>
internal interface IAimlReaderListener
{
    /// <summary>
    /// Invoked for every parsed AIML category.
    /// </summary>
    /// <param name="pattern">The (optionally normalized) <c>&lt;pattern&gt;</c> text.</param>
    /// <param name="that">The (optionally normalized) <c>&lt;that&gt;</c> text; <c>"*"</c> when unspecified.</param>
    /// <param name="topic">The (optionally normalized) topic name from a surrounding <c>&lt;topic name="…"/&gt;</c>, or <c>"*"</c>.</param>
    /// <param name="templateXml">The raw (unparsed) <c>&lt;template&gt;</c> XML for the category.</param>
    /// <param name="sourceName">An optional logical source identifier (e.g., filename) for diagnostics.</param>
    void OnCategory(string pattern, string that, string topic, string templateXml, string? sourceName = null);
}
