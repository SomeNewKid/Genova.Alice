// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Emits newly learned categories (via <c>&lt;learn&gt;</c>) into the runtime.
/// Implementations typically add the category to the graph and record it in a learn store.
/// </summary>
/// <param name="pattern">Normalized INPUT pattern.</param>
/// <param name="that">Normalized THAT qualifier.</param>
/// <param name="topic">Normalized TOPIC qualifier.</param>
/// <param name="templateXml">Raw template XML for the learned category.</param>
/// <param name="sourceName">Optional logical source (e.g., filename) for diagnostics.</param>
internal delegate void LearnEmitter(string pattern, string that, string topic, string templateXml, string? sourceName);
