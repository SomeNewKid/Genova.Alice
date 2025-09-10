// =============================================================
// Genova.Alice.Core — Part 10
// ReductionLoader: loads ALICE reduction*.aiml into Graphmaster
// from arbitrary System.IO.Streams (e.g., embedded resources).
//
// Usage example (caller provides streams):
// using Stream? s = typeof(AimlLoader).Assembly.GetManifestResourceStream(resourceName);
// var loader = new ReductionLoader(preProcessor, graphmaster);
// loader.Load(s!, sourceName: resourceName, normalize: true);
//
// Notes:
// - We reuse AimlLoader and a tiny listener that inserts categories
//   directly into Graphmaster.
// - We *do not* dispose the provided streams; the caller owns them.
// - Normalization is delegated to AimlLoader (set normalize=true).
// =============================================================

using System;
using System.Collections.Generic;
using System.IO;

namespace Genova.Alice;

internal sealed class ReductionLoader
{
    private readonly PreProcessor _pre;
    private readonly Graphmaster _graph;

    // Listener that inserts parsed categories into Graphmaster
    private sealed class GraphAddListener : IAimlReaderListener
    {
        // string debug
        internal string? LastHelloSource => _lastSource;
        private string? _lastSource;

        private readonly Graphmaster _graph;
        internal GraphAddListener(Graphmaster graph) => _graph = graph;

        public void OnCategory(string pattern, string that, string topic, string templateXml, string? sourceName = null)
        {
            if (pattern == "HELLO" && that == "*" && topic == "*")
                _lastSource = sourceName ?? "(unknown)";

            if (pattern == "MY NAME" || pattern == "WHAT IS MY NAME")
                File.AppendAllText(@"C:\temp\name-cats.txt",
                    $"{sourceName}: {pattern} <THAT> {that} <TOPIC> {topic}\r\n");

            // Strings are already normalized if AimlLoader was invoked with normalize = true
            _graph.AddCategory(pattern, that, topic, templateXml);
        }
    }

    internal int DocumentsLoaded { get; private set; }
    internal int CategoriesLoaded { get; private set; }

    internal ReductionLoader(PreProcessor preProcessor, Graphmaster graph)
    {
        _pre = preProcessor ?? throw new ArgumentNullException(nameof(preProcessor));
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Load a single reduction AIML document from a stream.
    /// Caller owns the stream's lifetime.
    /// </summary>
    internal void Load(Stream stream, string? sourceName = null, bool normalize = true)
    {
        File.AppendAllText(@"C:\temp\loading.txt", $"ReductionLoader.Load(\"{sourceName}\")" + Environment.NewLine);

        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var listener = new GraphAddListener(_graph);
        var loader = new AimlLoader(_pre, listener);

        loader.LoadFromStream(stream, sourceName: sourceName, normalize: normalize);

        DocumentsLoaded += 1;
        CategoriesLoaded += loader.CategoriesEmitted;

        string? lastHelloSource = listener.LastHelloSource;
        if (!string.IsNullOrEmpty(lastHelloSource))
        {
            string debug = $"ReductionLoader.Load(\"{sourceName}\"): ";
            debug += $"    LastHelloSource: \"{lastHelloSource}\"";
            File.AppendAllText(@"C:\temp\debugging.txt", debug + Environment.NewLine);
        }
    }

    /// <summary>
    /// Load multiple reduction AIML documents from a sequence of (stream, name) pairs.
    /// Caller owns the streams' lifetimes.
    /// </summary>
    internal void LoadMany(IEnumerable<(Stream stream, string? name)> sources, bool normalize = true)
    {
        if (sources is null) throw new ArgumentNullException(nameof(sources));

        foreach (var (stream, name) in sources)
        {
            if (stream is null) continue; // skip null entries defensively
            Load(stream, sourceName: name, normalize: normalize);
        }
    }

    /// <summary>
    /// Convenience overload for when you only have streams (no names).
    /// </summary>
    internal void LoadMany(IEnumerable<Stream> streams, bool normalize = true)
    {
        if (streams is null) throw new ArgumentNullException(nameof(streams));
        foreach (var s in streams)
        {
            if (s is null) continue;
            Load(s, sourceName: null, normalize: normalize);
        }
    }
}
