// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Immutable record describing a single learned category.
/// </summary>
/// <param name="pattern">The normalized INPUT pattern.</param>
/// <param name="that">The normalized THAT qualifier.</param>
/// <param name="topic">The normalized TOPIC qualifier.</param>
/// <param name="templateXml">The raw template XML to render when matched.</param>
/// <param name="sourceName">Optional logical source identifier (e.g., filename) for diagnostics.</param>
#pragma warning disable SA1300 // Lower-case record members preserve AIML field naming used by tests.
internal sealed record LearnedCategory(
    string pattern,
    string that,
    string topic,
    string templateXml,
    string? sourceName);
#pragma warning restore SA1300
