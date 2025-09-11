// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Resolves SRAI (symbolic reduction) requests by re-entering the match pipeline
/// with the provided text and current conversational context.
/// </summary>
/// <param name="input">The text to re-ask via SRAI.</param>
/// <param name="session">The session providing THAT/TOPIC/predicate context.</param>
/// <param name="depth">The current recursion depth (used for limiting).</param>
/// <returns>The rendered reply text for the SRAI target, or empty if no match.</returns>
internal delegate string SraiInvoker(string input, UserSession session, int depth);
