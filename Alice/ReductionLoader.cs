// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Loads ALICE <c>reduction*.aiml</c> documents into a shared <see cref="Graphmaster"/>.
/// This loader raises categories via <see cref="AimlLoader"/> into the runtime graph.
/// <para>
/// Note: This loader does <b>not</b> dispose the provided streams; the caller owns
/// the lifetime of each stream.
/// </para>
/// </summary>
internal sealed class ReductionLoader
{
    private readonly PreProcessor _pre;
    private readonly Graphmaster _graph;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReductionLoader"/> class.
    /// </summary>
    /// <param name="preProcessor">The preprocessor used to normalize pattern/that/topic fields.</param>
    /// <param name="graph">The target <see cref="Graphmaster"/> into which categories will be added.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="preProcessor"/> or <paramref name="graph"/> is <c>null</c>.
    /// </exception>
    internal ReductionLoader(PreProcessor preProcessor, Graphmaster graph)
    {
        _pre = preProcessor ?? throw new ArgumentNullException(nameof(preProcessor));
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Gets the number of reduction AIML documents successfully loaded by this instance.
    /// </summary>
    internal int DocumentsLoaded { get; private set; }

    /// <summary>
    /// Gets the total number of categories emitted to the graph across all loaded documents.
    /// </summary>
    internal int CategoriesLoaded { get; private set; }

    /// <summary>
    /// Loads a single reduction AIML document from a readable stream and adds all categories
    /// to the target <see cref="Graphmaster"/>.
    /// </summary>
    /// <param name="stream">A readable stream positioned at the start of an AIML document.</param>
    /// <param name="sourceName">Optional logical name for diagnostics (e.g., the resource or file name).</param>
    /// <param name="normalize">When <c>true</c>, applies normalization to pattern/that/topic.</param>
    /// <remarks>
    /// The stream is not disposed by this method; the caller is responsible for its lifetime.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    internal void Load(Stream stream, string? sourceName = null, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        GraphAddListener listener = new (_graph);
        AimlLoader loader = new (_pre, listener);

        loader.LoadFromStream(stream, sourceName: sourceName, normalize: normalize);

        DocumentsLoaded += 1;
        CategoriesLoaded += loader.CategoriesEmitted;
    }

    /// <summary>
    /// Loads multiple reduction AIML documents from a sequence of (stream, name) pairs and
    /// adds all categories to the target <see cref="Graphmaster"/>.
    /// </summary>
    /// <param name="sources">
    /// A sequence of tuples containing a readable AIML <see cref="Stream"/> and an optional source name.
    /// </param>
    /// <param name="normalize">When <c>true</c>, applies normalization to pattern/that/topic.</param>
    /// <remarks>
    /// Streams are not disposed by this method; the caller is responsible for their lifetimes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources"/> is <c>null</c>.</exception>
    internal void LoadMany(IEnumerable<(Stream stream, string? name)> sources, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(sources);

        foreach ((Stream stream, string? name) in sources)
        {
            if (stream is null)
            {
                continue; // skip null entries defensively
            }

            Load(stream, sourceName: name, normalize: normalize);
        }
    }

    /// <summary>
    /// Loads multiple reduction AIML documents from a sequence of streams and adds all categories
    /// to the target <see cref="Graphmaster"/>.
    /// </summary>
    /// <param name="streams">A sequence of readable AIML streams.</param>
    /// <param name="normalize">When <c>true</c>, applies normalization to pattern/that/topic.</param>
    /// <remarks>
    /// Streams are not disposed by this method; the caller is responsible for their lifetimes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="streams"/> is <c>null</c>.</exception>
    internal void LoadMany(IEnumerable<Stream> streams, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(streams);
        foreach (Stream? s in streams)
        {
            if (s is null)
            {
                continue;
            }

            Load(s, sourceName: null, normalize: normalize);
        }
    }

    /// <summary>
    /// Listener that inserts parsed AIML categories into the target
    /// <see cref="Graphmaster"/> and records diagnostics such as the last
    /// source that provided a <c>HELLO &lt;THAT&gt; * &lt;TOPIC&gt; *</c> category.
    /// </summary>
    private sealed class GraphAddListener : IAimlReaderListener
    {
        private readonly Graphmaster _graph;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphAddListener"/> class
        /// that will add categories into the provided <see cref="Graphmaster"/>.
        /// </summary>
        /// <param name="graph">The graph that receives inserted categories.</param>
        internal GraphAddListener(Graphmaster graph) => _graph = graph;

        /// <inheritdoc/>
        public void OnCategory(string pattern, string that, string topic, string templateXml, string? sourceName = null)
        {
            _graph.AddCategory(pattern, that, topic, templateXml);
        }
    }
}
