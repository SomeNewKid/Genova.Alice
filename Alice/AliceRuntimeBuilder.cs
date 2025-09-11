// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Genova.Alice;

/// <summary>
/// Composes the ALICE runtime: constructs the <see cref="Bot"/>, <see cref="PreProcessor"/>,
/// and <see cref="Graphmaster"/>, supports loading AIML documents one-at-a-time from
/// disposable streams, and finally creates a ready-to-use <see cref="Engine"/>.
/// </summary>
internal sealed class AliceRuntimeBuilder
{
    private readonly Func<DateTime>? _nowProvider;
    private readonly Random _rng;
    private bool _engineCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="AliceRuntimeBuilder"/> class.
    /// Creates the core runtime components (bot, preprocessor, graph, learn store)
    /// but does not yet create the <see cref="Engine"/>.
    /// </summary>
    /// <param name="botProperties">Optional bot properties; if <c>null</c>, an empty bag is used.</param>
    /// <param name="substitutions">Optional substitution tables; if <c>null</c>, classic defaults are used.</param>
    /// <param name="thatHistoryDepth">Maximum depth for the per-session <c>&lt;that&gt;</c> history.</param>
    /// <param name="rng">Random source for <c>&lt;random&gt;</c> selections; if <c>null</c>, a new instance is used.</param>
    /// <param name="nowProvider">Clock provider for <c>&lt;date/&gt;</c>; if <c>null</c>, <see cref="DateTime.Now"/> is used.</param>
    internal AliceRuntimeBuilder(
        BotProperties? botProperties = null,
        SubstitutionTables? substitutions = null,
        int thatHistoryDepth = 8,
        Random? rng = null,
        Func<DateTime>? nowProvider = null)
    {
        BotProperties props = botProperties ?? new ();
        SubstitutionTables subs = substitutions ?? SubstitutionTables.CreateClassicDefaults();

        Bot = new Bot(props, subs, thatHistoryDepth);
        Pre = new PreProcessor(subs);
        Graph = new Graphmaster();
        LearnStore = new LearnStore();

        _rng = rng ?? new Random();
        _nowProvider = nowProvider;
    }

    /// <summary>
    /// Gets the bot-wide property bag and substitution tables used by <c>&lt;bot name="…"/&gt;</c>
    /// and other persona-dependent templates.
    /// </summary>
    internal Bot Bot { get; }

    /// <summary>
    /// Gets the normalizer used for input and pattern preprocessing (case, punctuation, substitutions).
    /// </summary>
    internal PreProcessor Pre { get; }

    /// <summary>
    /// Gets the Graphmaster structure that stores categories and performs pattern/THAT/TOPIC matching.
    /// </summary>
    internal Graphmaster Graph { get; }

    /// <summary>
    /// Gets the in-memory store of categories learned via <c>&lt;learn&gt;</c>.
    /// </summary>
    internal LearnStore LearnStore { get; }

    /// <summary>
    /// Gets or sets the number of reduction AIML documents successfully loaded.
    /// </summary>
    internal int ReductionDocumentsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the number of core AIML documents successfully loaded.
    /// </summary>
    internal int CoreDocumentsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the total number of categories added to <see cref="Graph"/>.
    /// </summary>
    internal int CategoriesLoaded { get; set; }

    /// <summary>
    /// Loads bot properties from a factory that returns a fresh readable <see cref="Stream"/>.
    /// The stream is opened and disposed within this method.
    /// </summary>
    /// <param name="streamFactory">A factory that returns a new readable stream for the properties content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="streamFactory"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The factory returned <c>null</c> or a non-readable stream.</exception>
    internal void LoadBotProperties(Func<Stream> streamFactory)
    {
        ArgumentNullException.ThrowIfNull(streamFactory);

        using Stream? stream = streamFactory();
        if (stream is null)
        {
            throw new InvalidOperationException("bot.properties streamFactory returned null.");
        }

        UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        using StreamReader reader = new (stream, encoding);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#"))
            {
                continue;
            }

            int eq = t.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            string key = t[..eq].Trim();
            string val = t[(eq + 1) ..].Trim();

            if (key.Length > 0)
            {
                Bot.Properties.Set(key, val);
            }
        }
    }

    /// <summary>
    /// Loads a reduction AIML document (e.g., <c>reduction*.safe.aiml</c>) from a stream factory.
    /// The stream is opened and disposed within this call, and all categories are added to <see cref="Graph"/>.
    /// </summary>
    /// <param name="streamFactory">A factory that returns a new readable stream for the AIML document.</param>
    /// <param name="sourceName">Optional logical name for diagnostics.</param>
    /// <param name="normalize">Whether to normalize pattern/that/topic fields during load.</param>
    /// <exception cref="ArgumentNullException"><paramref name="streamFactory"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The factory returned <c>null</c> or a non-readable stream.</exception>
    internal void LoadReduction(Func<Stream> streamFactory, string? sourceName = null, bool normalize = true)
    {
        EnsureNotFinalized();
        ArgumentNullException.ThrowIfNull(streamFactory);

        int added = 0;
        GraphAddListener listener = new (Graph, c => added += c);
        AimlLoader loader = new (Pre, listener);

        using (Stream? stream = streamFactory())
        {
            if (stream is null)
            {
                throw new InvalidOperationException("streamFactory returned null.");
            }

            loader.LoadFromStream(stream, sourceName, normalize);
        }

        ReductionDocumentsLoaded += 1;
        CategoriesLoaded += added;
    }

    /// <summary>
    /// Loads a core (non-reduction) AIML document from a stream factory.
    /// The stream is opened and disposed within this call, and all categories are added to <see cref="Graph"/>.
    /// </summary>
    /// <param name="streamFactory">A factory that returns a new readable stream for the AIML document.</param>
    /// <param name="sourceName">Optional logical name for diagnostics.</param>
    /// <param name="normalize">Whether to normalize pattern/that/topic fields during load.</param>
    /// <exception cref="ArgumentNullException"><paramref name="streamFactory"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The factory returned <c>null</c> or a non-readable stream.</exception>
    internal void LoadCoreAiml(Func<Stream> streamFactory, string? sourceName = null, bool normalize = true)
    {
        EnsureNotFinalized();
        ArgumentNullException.ThrowIfNull(streamFactory);

        int added = 0;
        GraphAddListener listener = new (Graph, c => added += c);
        AimlLoader loader = new (Pre, listener);

        using (Stream? stream = streamFactory())
        {
            if (stream is null)
            {
                throw new InvalidOperationException("streamFactory returned null.");
            }

            loader.LoadFromStream(stream, sourceName, normalize);
        }

        CoreDocumentsLoaded += 1;
        CategoriesLoaded += added;
    }

    /// <summary>
    /// Finalizes the runtime by creating a <see cref="TemplateProcessor"/> (with <c>&lt;learn/&gt;</c> and <c>&lt;date/&gt;</c> support)
    /// and a fully wired <see cref="Engine"/>. After calling this method, no further AIML can be loaded.
    /// </summary>
    /// <returns>A constructed <see cref="Engine"/> ready to handle user input.</returns>
    /// <exception cref="InvalidOperationException">The engine has already been created.</exception>
    internal Engine CreateEngine()
    {
        EnsureNotFinalized();

        // Wire <learn/> to add to the bot-wide graph and record in LearnStore
        LearnEmitter learn = (pattern, that, topic, templateXml, source) =>
        {
            Graph.AddCategory(pattern, that, topic, templateXml);
            LearnStore.Add(pattern, that, topic, templateXml, source);
        };

        // SRAI delegate that will call back into the Engine created below
        Engine? engine = null;
        string Srai(string text, UserSession session, int depth) => engine!.Srai(text, session, depth);

        TemplateProcessor templates = new (
            Bot,
            Pre,
            sraiInvoker: Srai,
            rng: _rng,
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: learn,
            nowProvider: _nowProvider ?? (() => DateTime.Now));

        engine = new Engine(Bot, Graph, Pre, templates);
        _engineCreated = true;
        return engine;
    }

    private void EnsureNotFinalized()
    {
        if (_engineCreated)
        {
            throw new InvalidOperationException("Engine has already been created; no further AIML can be loaded.");
        }
    }

    /// <summary>
    /// Minimal listener that inserts parsed AIML categories directly into the runtime
    /// <see cref="Graphmaster"/> and increments a supplied counter. Also records the
    /// last source that provided a <c>HELLO &lt;THAT&gt; * &lt;TOPIC&gt; *</c> category
    /// for diagnostics.
    /// </summary>
    private sealed class GraphAddListener : IAimlReaderListener
    {
        private readonly Graphmaster _graph;
        private readonly Action<int> _countSink;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphAddListener"/> class.
        /// </summary>
        /// <param name="graph">The target <see cref="Graphmaster"/> to receive categories.</param>
        /// <param name="countSink">
        /// A callback invoked with <c>+1</c> for each category added (used for load metrics).
        /// </param>
        internal GraphAddListener(Graphmaster graph, Action<int> countSink)
        {
            _graph = graph;
            _countSink = countSink;
        }

        /// <summary>
        /// Gets the logical source name (e.g., filename) of the most recently loaded
        /// <c>HELLO &lt;THAT&gt; * &lt;TOPIC&gt; *</c> category, or <c>null</c> if none.
        /// </summary>
        internal string? LastHelloSource { get; private set; }

        /// <inheritdoc/>
        public void OnCategory(
            string pattern, string that, string topic, string templateXml, string? sourceName = null)
        {
            if (pattern == "HELLO" && that == "*" && topic == "*")
            {
                LastHelloSource = sourceName ?? "(unknown)";
            }

            _graph.AddCategory(pattern, that, topic, templateXml);
            _countSink(1);
        }
    }
}
