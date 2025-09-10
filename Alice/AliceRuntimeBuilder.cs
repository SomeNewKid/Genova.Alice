// =============================================================
// Genova.Alice.Core — AliceRuntimeBuilder
// Build Bot + PreProcessor + Graphmaster (+ LearnStore), load AIML
// one stream at a time (disposed immediately), then create Engine.
// =============================================================

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Genova.Alice;

/// <summary>
/// Builder that composes the ALICE runtime, lets you load AIML files
/// one-at-a-time (each stream is opened and disposed immediately),
/// and finally creates a ready Engine.
/// </summary>
internal sealed class AliceRuntimeBuilder
{
    // Core components built up front
    internal Bot Bot { get; }
    internal PreProcessor Pre { get; }
    internal Graphmaster Graph { get; }
    internal LearnStore LearnStore { get; }

    // Optional collaborators for tags
    private readonly Func<DateTime>? _nowProvider;
    private readonly Random _rng;

    // Instrumentation
    internal int ReductionDocumentsLoaded { get; private set; }
    internal int CoreDocumentsLoaded { get; private set; }
    internal int CategoriesLoaded { get; private set; }

    private bool _engineCreated;

    /// <summary>
    /// Create a new runtime builder. Everything except the Engine is created here.
    /// </summary>
    /// <param name="botProperties">Optional bot properties; empty if null.</param>
    /// <param name="substitutions">Optional substitutions; classic defaults if null.</param>
    /// <param name="thatHistoryDepth">THAT history depth (default 8).</param>
    /// <param name="rng">Random source for &lt;random&gt; (default new Random()).</param>
    /// <param name="nowProvider">Clock for &lt;date/&gt; (default DateTime.Now).</param>
    internal AliceRuntimeBuilder(
        BotProperties? botProperties = null,
        SubstitutionTables? substitutions = null,
        int thatHistoryDepth = 8,
        Random? rng = null,
        Func<DateTime>? nowProvider = null)
    {
        var props = botProperties ?? new BotProperties();
        var subs = substitutions ?? SubstitutionTables.CreateClassicDefaults();

        Bot = new Bot(props, subs, thatHistoryDepth);
        Pre = new PreProcessor(subs);
        Graph = new Graphmaster();
        LearnStore = new LearnStore();

        _rng = rng ?? new Random();
        _nowProvider = nowProvider;
    }

    internal void LoadBotProperties(Func<Stream> streamFactory)
    {
        if (streamFactory is null) throw new ArgumentNullException(nameof(streamFactory));

        using var stream = streamFactory();
        if (stream is null) throw new InvalidOperationException("bot.properties streamFactory returned null.");
        using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#")) continue;

            var eq = t.IndexOf('=');
            if (eq <= 0) continue;

            var key = t[..eq].Trim();
            var val = t[(eq + 1)..].Trim();

            if (key.Length > 0)
                Bot.Properties.Set(key, val);
        }
    }

    // Minimal listener that inserts categories directly into Graphmaster
    private sealed class GraphAddListener : IAimlReaderListener
    {
        private readonly Graphmaster _graph;
        private readonly Action<int> _countSink;

        // string debug
        internal string? LastHelloSource => _lastSource;
        private string? _lastSource;

        internal GraphAddListener(Graphmaster graph, Action<int> countSink)
        {
            _graph = graph;
            _countSink = countSink;
        }

        public void OnCategory(string pattern, string that, string topic, string templateXml, string? sourceName = null)
        {
            if (pattern == "HELLO" && that == "*" && topic == "*")
                _lastSource = sourceName ?? "(unknown)";

            if (pattern == "MY NAME" || pattern == "WHAT IS MY NAME")
                File.AppendAllText(@"C:\temp\name-cats.txt",
                    $"{sourceName}: {pattern} <THAT> {that} <TOPIC> {topic}\r\n");

            // In your AimlReaderListener.OnCategory
            if (pattern == "*" && that == "*" && topic == "*")
                File.AppendAllText(@"C:\temp\star-fallbacks.txt",
                    $"{sourceName}: * <THAT> * <TOPIC> *  -> template starts with: {templateXml[..Math.Min(60, templateXml.Length)]}\r\n");

            _graph.AddCategory(pattern, that, topic, templateXml);
            _countSink(1);
        }
    }

    /// <summary>
    /// Load a *reduction* AIML document from a factory that returns a fresh stream.
    /// The stream is opened, parsed, and disposed before this call returns.
    /// </summary>
    /// <param name="streamFactory">Factory that returns a new readable Stream.</param>
    /// <param name="sourceName">Optional identifier for diagnostics.</param>
    /// <param name="normalize">Whether to normalize pattern/that/topic (recommended true).</param>
    internal void LoadReduction(Func<Stream> streamFactory, string? sourceName = null, bool normalize = true)
    {
        File.AppendAllText(@"C:\temp\loading.txt", $"AliceRuntimeBuilder.LoadReduction(\"{sourceName}\")" + Environment.NewLine);

        EnsureNotFinalized();
        if (streamFactory is null) throw new ArgumentNullException(nameof(streamFactory));

        int added = 0;
        var listener = new GraphAddListener(Graph, c => added += c);
        var loader = new AimlLoader(Pre, listener);

        using (var stream = streamFactory())
        {
            if (stream is null) throw new InvalidOperationException("streamFactory returned null.");
            loader.LoadFromStream(stream, sourceName, normalize);
        }

        ReductionDocumentsLoaded += 1;
        CategoriesLoaded += added;

        string? lastHelloSource = listener.LastHelloSource;
        if (!string.IsNullOrEmpty(lastHelloSource))
        {
            string debug = $"AliceRuntimeBuilder.LoadReduction(\"{sourceName}\"): ";
            debug += $"    LastHelloSource: \"{lastHelloSource}\"";
            File.AppendAllText(@"C:\temp\debugging.txt", debug + Environment.NewLine);
        }
    }

    /// <summary>
    /// Load a *core* (non-reduction) AIML document from a factory that returns a fresh stream.
    /// The stream is opened, parsed, and disposed before this call returns.
    /// </summary>
    /// <param name="streamFactory">Factory that returns a new readable Stream.</param>
    /// <param name="sourceName">Optional identifier for diagnostics.</param>
    /// <param name="normalize">Whether to normalize pattern/that/topic (recommended true).</param>
    internal void LoadCoreAiml(Func<Stream> streamFactory, string? sourceName = null, bool normalize = true)
    {
        File.AppendAllText(@"C:\temp\loading.txt", $"AliceRuntimeBuilder.LoadCoreAiml(\"{sourceName}\")" + Environment.NewLine);

        EnsureNotFinalized();
        if (streamFactory is null) throw new ArgumentNullException(nameof(streamFactory));

        int added = 0;
        var listener = new GraphAddListener(Graph, c => added += c);
        var loader = new AimlLoader(Pre, listener);

        using (var stream = streamFactory())
        {
            if (stream is null) throw new InvalidOperationException("streamFactory returned null.");
            loader.LoadFromStream(stream, sourceName, normalize);
        }

        CoreDocumentsLoaded += 1;
        CategoriesLoaded += added;

        string? lastHelloSource = listener.LastHelloSource;
        if (!string.IsNullOrEmpty(lastHelloSource))
        {
            string debug = $"AliceRuntimeBuilder.LoadCoreAiml(\"{sourceName}\"): ";
            debug += $"    LastHelloSource: \"{lastHelloSource}\"";
            File.AppendAllText(@"C:\temp\debugging.txt", debug + Environment.NewLine);
        }
    }

    /// <summary>
    /// Finalize the runtime by creating a TemplateProcessor (with &lt;learn/&gt; and &lt;date/&gt;)
    /// and an Engine wired to SRAI through that processor. Returns the Engine instance.
    /// </summary>
    internal Engine CreateEngine()
    {
        EnsureNotFinalized();

        // Wire <learn/> to add to the bot-wide graph and record in LearnStore
        TemplateProcessor.LearnEmitter learn = (pattern, that, topic, templateXml, source) =>
        {
            Graph.AddCategory(pattern, that, topic, templateXml);
            LearnStore.Add(pattern, that, topic, templateXml, source);
            // We don't increment CategoriesLoaded here because it's a runtime mutation,
            // but you could track learned category count separately if desired.
        };

        // SRAI delegate that will call back into the Engine created below
        Engine? engine = null;
        SraiInvoker srai = (text, session, depth) => engine!.Srai(text, session, depth);

        var templates = new TemplateProcessor(
            Bot,
            Pre,
            sraiInvoker: srai,
            rng: _rng,
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: learn,
            nowProvider: _nowProvider ?? (() => DateTime.Now)
        );

        engine = new Engine(Bot, Graph, Pre, templates);
        _engineCreated = true;
        return engine;
    }

    private void EnsureNotFinalized()
    {
        if (_engineCreated)
            throw new InvalidOperationException("Engine has already been created; no further AIML can be loaded.");
    }
}
