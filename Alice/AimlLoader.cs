// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Genova.Alice;

/// <summary>
/// SAX-style AIML loader that walks <c>&lt;aiml&gt;</c> documents and raises
/// each <c>&lt;category&gt;</c> to an <see cref="IAimlReaderListener"/>.
/// Supports normalization via the supplied <see cref="PreProcessor"/>.
/// </summary>
internal sealed partial class AimlLoader
{
    private readonly IAimlReaderListener _listener;

    /// <summary>
    /// Initializes a new instance of the <see cref="AimlLoader"/> class.
    /// </summary>
    /// <param name="preProcessor">The preprocessor used to normalize AIML text.</param>
    /// <param name="listener">The listener to receive discovered categories.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="preProcessor"/> or <paramref name="listener"/> is <c>null</c>.</exception>
    internal AimlLoader(PreProcessor preProcessor, IAimlReaderListener listener)
    {
        PreProcessor = preProcessor ?? throw new ArgumentNullException(nameof(preProcessor));
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
    }

    /// <summary>
    /// Gets the preprocessor used for normalization of pattern/that/topic fields.
    /// </summary>
    internal PreProcessor PreProcessor { get; }

    /// <summary>
    /// Gets the number of categories emitted to the listener during the last load operation.
    /// </summary>
    internal int CategoriesEmitted { get; private set; }

    /// <summary>
    /// Loads an AIML document from a readable <see cref="Stream"/> and emits categories to the listener.
    /// The stream is not owned by the loader and should be disposed by the caller.
    /// </summary>
    /// <param name="stream">A readable stream containing an AIML document.</param>
    /// <param name="sourceName">Optional logical name for diagnostics (e.g., the resource or file name).</param>
    /// <param name="normalize">When <c>true</c>, applies <see cref="PreProcessor"/> normalization to pattern/that/topic.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the root element is not <c>&lt;aiml&gt;</c>.</exception>
    internal void LoadFromStream(Stream stream, string? sourceName = null, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(stream);
        XDocument xdoc = XDocument.Load(stream, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        ParseDocument(xdoc, sourceName, normalize);
    }

    /// <summary>
    /// Loads an AIML document from a string and emits categories to the listener.
    /// </summary>
    /// <param name="xml">A string containing an AIML document.</param>
    /// <param name="sourceName">Optional logical name for diagnostics (e.g., the resource or file name).</param>
    /// <param name="normalize">When <c>true</c>, applies <see cref="PreProcessor"/> normalization to pattern/that/topic.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xml"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the root element is not <c>&lt;aiml&gt;</c>.</exception>
    internal void LoadFromString(string xml, string? sourceName = null, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(xml);
        XDocument xdoc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        ParseDocument(xdoc, sourceName, normalize);
    }

    // Convert "<date />" -> "<date/>", "<foo attr=\"v\" />" -> "<foo attr=\"v\"/>"
    private static string CanonicalizeEmptyElementClosures(string xml)
    {
        return EmptyElementClosure().Replace(xml, "/>");
    }

    [GeneratedRegex(@"\s+/>")]
    private static partial Regex EmptyElementClosure();

    private void ParseDocument(XDocument xdoc, string? sourceName, bool normalize)
    {
        XElement root = xdoc.Root ?? throw new InvalidOperationException("AIML root element not found.");
        if (!root.Name.LocalName.Equals("aiml", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Root element must be <aiml>.");
        }

        foreach (XElement child in root.Elements())
        {
            string name = child.Name.LocalName.ToLowerInvariant();
            if (name == "category")
            {
                EmitCategory(child, "*", sourceName, normalize);
            }
            else if (name == "topic")
            {
                string topicName = (child.Attribute("name")?.Value ?? "*").Trim();
                foreach (var cat in child.Elements())
                {
                    if (cat.Name.LocalName.Equals("category", StringComparison.OrdinalIgnoreCase))
                    {
                        EmitCategory(cat, topicName, sourceName, normalize);
                    }
                }
            }
        }
    }

    private void EmitCategory(XElement categoryEl, string topicFromWrapper, string? sourceName, bool normalize)
    {
        string patternText = (categoryEl.Element(XName.Get("pattern"))?.Value ?? string.Empty).Trim();
        XElement? thatEl = categoryEl.Element(XName.Get("that"));
        string thatText = thatEl is null ? "*" : (thatEl.Value ?? string.Empty).Trim();

        XElement? templateEl = categoryEl.Element(XName.Get("template"));
        string templateXml = templateEl is null
            ? "<template/>"
            : CanonicalizeEmptyElementClosures(templateEl.ToString(SaveOptions.DisableFormatting));

        string topicText = topicFromWrapper?.Trim().Length > 0 ? topicFromWrapper.Trim() : "*";

        if (normalize)
        {
            patternText = PreProcessor.NormalizePattern(patternText);
            thatText = PreProcessor.NormalizeThat(thatText);
            topicText = PreProcessor.NormalizeTopic(topicText);
        }

        _listener.OnCategory(patternText, thatText, topicText, templateXml, sourceName);
        CategoriesEmitted++;
    }
}
