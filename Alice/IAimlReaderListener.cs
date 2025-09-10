// =============================================================
// Genova.Alice.Core — Part 4 (Fix: canonicalize empty-element tags)
// Ensures <date/> style (no space) so tests expecting "<date/>" pass.
// =============================================================

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Genova.Alice;

internal interface IAimlReaderListener
{
    void OnCategory(string pattern, string that, string topic, string templateXml, string? sourceName = null);
}

internal sealed class AimlLoader
{
    private readonly IAimlReaderListener _listener;

    internal PreProcessor PreProcessor { get; }
    internal int CategoriesEmitted { get; private set; }

    internal AimlLoader(PreProcessor preProcessor, IAimlReaderListener listener)
    {
        PreProcessor = preProcessor ?? throw new ArgumentNullException(nameof(preProcessor));
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
    }

    internal void LoadFromStream(Stream stream, string? sourceName = null, bool normalize = true)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var xdoc = XDocument.Load(stream, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        ParseDocument(xdoc, sourceName, normalize);
    }

    internal void LoadFromString(string xml, string? sourceName = null, bool normalize = true)
    {
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        var xdoc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        ParseDocument(xdoc, sourceName, normalize);
    }

    private void ParseDocument(XDocument xdoc, string? sourceName, bool normalize)
    {
        var root = xdoc.Root ?? throw new InvalidOperationException("AIML root element not found.");
        if (!root.Name.LocalName.Equals("aiml", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Root element must be <aiml>.");

        foreach (var child in root.Elements())
        {
            var name = child.Name.LocalName.ToLowerInvariant();
            if (name == "category")
            {
                EmitCategory(child, "*", sourceName, normalize);
            }
            else if (name == "topic")
            {
                var topicName = (child.Attribute("name")?.Value ?? "*").Trim();
                foreach (var cat in child.Elements())
                {
                    if (cat.Name.LocalName.Equals("category", StringComparison.OrdinalIgnoreCase))
                        EmitCategory(cat, topicName, sourceName, normalize);
                }
            }
        }
    }

    private void EmitCategory(XElement categoryEl, string topicFromWrapper, string? sourceName, bool normalize)
    {
        var patternText = (categoryEl.Element(XName.Get("pattern"))?.Value ?? string.Empty).Trim();
        var thatEl = categoryEl.Element(XName.Get("that"));
        var thatText = thatEl is null ? "*" : (thatEl.Value ?? string.Empty).Trim();

        var templateEl = categoryEl.Element(XName.Get("template"));
        var templateXml = templateEl is null
            ? "<template/>"
            : CanonicalizeEmptyElementClosures(templateEl.ToString(SaveOptions.DisableFormatting));

        var topicText = (topicFromWrapper?.Trim().Length > 0 ? topicFromWrapper.Trim() : "*");

        if (normalize)
        {
            patternText = PreProcessor.NormalizePattern(patternText);
            thatText = PreProcessor.NormalizeThat(thatText);
            topicText = PreProcessor.NormalizeTopic(topicText);
        }

        _listener.OnCategory(patternText, thatText, topicText, templateXml, sourceName);
        CategoriesEmitted++;
    }

    // Convert "<date />" -> "<date/>", "<foo attr=\"v\" />" -> "<foo attr=\"v\"/>"
    private static string CanonicalizeEmptyElementClosures(string xml)
    {
        return Regex.Replace(xml, @"\s+/>", "/>");
    }
}
