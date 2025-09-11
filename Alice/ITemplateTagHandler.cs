// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;

namespace Genova.Alice;

/// <summary>
/// Contract for pluggable template tag handlers. Implementations can intercept specific
/// element names and produce output text given the current evaluation context.
/// </summary>
internal interface ITemplateTagHandler
{
    /// <summary>
    /// Evaluates the specified element within the template processing context.
    /// </summary>
    /// <param name="element">The XML element to evaluate.</param>
    /// <param name="ctx">The current evaluation context (session, match, depth, processor).</param>
    /// <returns>Rendered text for the element (may be empty).</returns>
    string Evaluate(XElement element, Context ctx);
}
