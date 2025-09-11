// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Captures the result of a successful match: the <see cref="Category"/>,
/// the <see cref="Path"/>, and all wildcard captures (<see cref="Stars"/>).
/// Also carries the normalized INPUT/THAT/TOPIC used to obtain the match.
/// </summary>
internal sealed class Match
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Match"/> class with the matched category, path, captures,
    /// and normalized context.
    /// </summary>
    /// <param name="category">The matched category.</param>
    /// <param name="path">The token path used for matching.</param>
    /// <param name="stars">The wildcard captures.</param>
    /// <param name="normalizedInput">Normalized INPUT sentence.</param>
    /// <param name="normalizedThat">Normalized THAT segment.</param>
    /// <param name="normalizedTopic">Normalized TOPIC segment.</param>
    internal Match(
        Category category,
        Path path,
        Stars stars,
        string normalizedInput,
        string normalizedThat,
        string normalizedTopic)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Stars = stars ?? throw new ArgumentNullException(nameof(stars));

        NormalizedInput = normalizedInput ?? string.Empty;
        NormalizedThat = normalizedThat ?? string.Empty;
        NormalizedTopic = normalizedTopic ?? string.Empty;
    }

    /// <summary>
    /// Gets the matched category.
    /// </summary>
    internal Category Category { get; }

    /// <summary>
    /// Gets the token path used to match this category.
    /// </summary>
    internal Path Path { get; }

    /// <summary>
    /// Gets the wildcard captures for this match.
    /// </summary>
    internal Stars Stars { get; }

    /// <summary>
    /// Gets the normalized INPUT used to match (for diagnostics).
    /// </summary>
    internal string NormalizedInput { get; }

    /// <summary>
    /// Gets the normalized THAT used to match (for diagnostics).
    /// </summary>
    internal string NormalizedThat { get; }

    /// <summary>
    /// Gets the normalized TOPIC used to match (for diagnostics).
    /// </summary>
    internal string NormalizedTopic { get; }

    /// <summary>
    /// Gets the 1-based INPUT <c>star</c> capture.
    /// </summary>
    /// <param name="index1">1-based capture index.</param>
    /// <returns>The capture text or empty.</returns>
    internal string Star(int index1) => Stars.StarAt(index1);

    /// <summary>
    /// Gets the 1-based THAT <c>thatstar</c> capture.
    /// </summary>
    /// <param name="index1">1-based capture index.</param>
    /// <returns>The capture text or empty.</returns>
    internal string ThatStar(int index1) => Stars.ThatStarAt(index1);

    /// <summary>
    /// Gets the 1-based TOPIC <c>topicstar</c> capture.
    /// </summary>
    /// <param name="index1">1-based capture index.</param>
    /// <returns>The capture text or empty.</returns>
    internal string TopicStar(int index1) => Stars.TopicStarAt(index1);
}
