// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Genova.Alice;

/// <summary>
/// Core template processor for AIML template XML. Supports text nodes and core tags,
/// SRAI recursion, random selection, conditions, casing transforms, <c>&lt;learn&gt;</c>,
/// and date/time formatting via <c>&lt;date/&gt;</c>.
/// </summary>
internal sealed class TemplateProcessor
{
    /// <summary>
    /// The default maximum recursion depth for <c>&lt;srai&gt;</c> calls.
    /// </summary>
    internal const int DefaultMaxDepth = 10;

    private readonly Bot _bot;
    private readonly PreProcessor _pre;
    private readonly SraiInvoker _srai;
    private readonly Random _rng;
    private readonly int _maxDepth;
    private readonly LearnEmitter? _learn;
    private readonly Func<DateTime>? _nowProvider;

    private readonly Dictionary<string, ITemplateTagHandler> _handlers =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateProcessor"/> class.
    /// </summary>
    /// <param name="bot">Bot persona and configuration context.</param>
    /// <param name="preProcessor">Preprocessor used for casing, punctuation, and substitutions.</param>
    /// <param name="sraiInvoker">Delegate used to resolve <c>&lt;srai&gt;</c> recursion.</param>
    /// <param name="rng">Random source for <c>&lt;random&gt;</c>; if <c>null</c>, a new instance is used.</param>
    /// <param name="maxDepth">Maximum recursion depth for <c>&lt;srai&gt;</c> calls.</param>
    /// <param name="learnEmitter">Optional sink for <c>&lt;learn&gt;</c> categories.</param>
    /// <param name="nowProvider">Optional clock provider for <c>&lt;date/&gt;</c>; if <c>null</c>, <see cref="DateTime.Now"/> is used.</param>
    internal TemplateProcessor(
        Bot bot,
        PreProcessor preProcessor,
        SraiInvoker sraiInvoker,
        Random? rng = null,
        int maxDepth = DefaultMaxDepth,
        LearnEmitter? learnEmitter = null,
        Func<DateTime>? nowProvider = null)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _pre = preProcessor ?? throw new ArgumentNullException(nameof(preProcessor));
        _srai = sraiInvoker ?? throw new ArgumentNullException(nameof(sraiInvoker));
        _rng = rng ?? new Random();
        _maxDepth = maxDepth > 0 ? maxDepth : DefaultMaxDepth;

        _learn = learnEmitter;
        _nowProvider = nowProvider;
    }

    /// <summary>
    /// Processes a template document or fragment into rendered text.
    /// Accepts either a full <c>&lt;template&gt;…&lt;/template&gt;</c> or a fragment,
    /// and returns the trimmed final output.
    /// </summary>
    /// <param name="templateXml">Raw template XML (document or fragment).</param>
    /// <param name="session">The session providing conversational state.</param>
    /// <param name="match">The current match (category, captures, normalized context).</param>
    /// <param name="depth">Current recursion depth for <c>&lt;srai&gt;</c>.</param>
    /// <returns>Rendered, whitespace-trimmed reply text (possibly empty).</returns>
    internal string Process(string templateXml, UserSession session, Match match, int depth = 0)
    {
        if (string.IsNullOrWhiteSpace(templateXml))
        {
            return string.Empty;
        }

        // 1) Try as-is: if it's a full <template>…</template> doc, evaluate it.
        try
        {
            XDocument xdoc1 = XDocument.Parse(templateXml, LoadOptions.PreserveWhitespace);
            XElement? root1 = xdoc1.Root;
            if (root1 != null && root1.Name.LocalName.Equals("template", StringComparison.OrdinalIgnoreCase))
            {
                Context ctx1 = new (this, session, match, depth);
                string result1 = EvalChildren(root1, ctx1);
                return result1.Trim();
            }
        }
        catch (XmlException)
        {
            // fall through to wrapper attempt
        }

        // 2) Wrap fragments or plain text in <template>…</template> and try again.
        string wrapped = $"<template>{templateXml}</template>";
        try
        {
            XDocument xdoc2 = XDocument.Parse(wrapped, LoadOptions.PreserveWhitespace);
            XElement root2 = xdoc2.Root!;
            Context ctx2 = new (this, session, match, depth);
            string result2 = EvalChildren(root2, ctx2);
            return result2.Trim();
        }
        catch (XmlException)
        {
            // 3) Last resort: return literal (don’t blow up runtime)
            return templateXml.Trim();
        }
    }

    /// <summary>
    /// Registers a custom tag handler for the specified element name (case-insensitive).
    /// </summary>
    /// <param name="tagName">The element name to handle.</param>
    /// <param name="handler">The handler implementation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tagName"/> or <paramref name="handler"/> is <c>null</c>.</exception>
    internal void RegisterHandler(string tagName, ITemplateTagHandler handler)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            throw new ArgumentNullException(nameof(tagName));
        }

        _handlers[tagName] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Attempts to retrieve a previously registered tag handler.
    /// </summary>
    /// <param name="tagName">The element name.</param>
    /// <param name="handler">When this method returns, contains the handler if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a handler is registered for the element name; otherwise <c>false</c>.</returns>
    internal bool TryGetHandler(string tagName, out ITemplateTagHandler? handler)
    {
        return _handlers.TryGetValue(tagName, out handler);
    }

    private static int ReadIndex1(XElement el, int defaultValue = 1)
    {
        string? attr = el.Attribute("index")?.Value;
        if (string.IsNullOrWhiteSpace(attr))
        {
            return defaultValue;
        }

        return int.TryParse(attr, out int i) && i > 0 ? i : defaultValue;
    }

    private static string Tag_Star(XElement el, Context ctx)
    {
        int i = ReadIndex1(el, 1);
        return ctx.Match.Star(i) ?? string.Empty;
    }

    private static string Tag_ThatStar(XElement el, Context ctx)
    {
        int i = ReadIndex1(el, 1);
        return ctx.Match.ThatStar(i) ?? string.Empty;
    }

    private static string Tag_TopicStar(XElement el, Context ctx)
    {
        int i = ReadIndex1(el, 1);
        return ctx.Match.TopicStar(i) ?? string.Empty;
    }

    private static string Tag_Get(XElement el, Context ctx)
    {
        string name = el.Attribute("name")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        if (name.Equals("topic", StringComparison.OrdinalIgnoreCase))
        {
            return ctx.Session.Topic;
        }

        return ctx.Session.Predicates.GetOrEmpty(name);
    }

    private static string GetInnerXml(XElement el)
    {
        if (!el.HasElements && el.Value.Length == 0)
        {
            return string.Empty;
        }

        return string.Concat(el.Nodes().Select(n => n.ToString(SaveOptions.DisableFormatting)));
    }

    private static string Tag_That(XElement el, Context ctx)
    {
        // Defaults per AIML spec: index="1,1" (most-recent reply, first sentence)
        int replyIdx = 1;   // which prior reply (1 = most recent)
        int sentIdx = 1;   // sentence within that reply (1-based)

        string? idxAttr = el.Attribute("index")?.Value?.Trim();
        if (!string.IsNullOrEmpty(idxAttr))
        {
            // Accept "n" or "m,n"
            string[] parts = idxAttr.Split(',');
            if (parts.Length == 1)
            {
                int.TryParse(parts[0].Trim(), out replyIdx);
                sentIdx = 1;
            }
            else
            {
                int.TryParse(parts[0].Trim(), out replyIdx);
                int.TryParse(parts[1].Trim(), out sentIdx);
            }

            replyIdx = replyIdx <= 0 ? 1 : replyIdx;
            sentIdx = sentIdx <= 0 ? 1 : sentIdx;
        }

        // Get the m-th most-recent reply from the history.
        var replyText = ctx.Session.ThatHistory.At(replyIdx);
        if (string.IsNullOrEmpty(replyText))
        {
            return string.Empty;
        }

        // If a sentence index is requested (always is, default 1), split and select.
        var sentences = ctx.Processor._pre.SplitSentences(replyText); // using the same splitter
        if (sentences.Count == 0)
        {
            return replyText; // no split -> return the whole reply
        }

        return (sentIdx <= sentences.Count) ? sentences[sentIdx - 1] : string.Empty;
    }

    // -----------------------------
    // Core evaluation
    // -----------------------------
    private string EvalNode(XNode node, Context ctx)
    {
        return node switch
        {
            // Drop whitespace-only text nodes to avoid leaking indentation/newlines
            XText t => string.IsNullOrWhiteSpace(t.Value) ? " " : t.Value,
            XElement e => EvalElement(e, ctx),
            _ => string.Empty
        };
    }

    private string EvalElement(XElement el, Context ctx)
    {
        string name = el.Name.LocalName.ToLowerInvariant();

        // Handler override?
        if (TryGetHandler(name, out ITemplateTagHandler? handler))
        {
            return handler!.Evaluate(el, ctx);
        }

        return name switch
        {
            "srai" => Tag_Srai(el, ctx),
            "sr" => Tag_Sr(ctx),

            "star" => Tag_Star(el, ctx),
            "thatstar" => Tag_ThatStar(el, ctx),
            "topicstar" => Tag_TopicStar(el, ctx),

            "think" => Tag_Think(el, ctx),
            "set" => Tag_Set(el, ctx),
            "get" => Tag_Get(el, ctx),
            "bot" => Tag_Bot(el),

            "random" => Tag_Random(el, ctx),
            "condition" => Tag_Condition(el, ctx),

            "that" => Tag_That(el, ctx),

            "uppercase" => Tag_Uppercase(el, ctx),
            "lowercase" => Tag_Lowercase(el, ctx),
            "formal" => Tag_Formal(el, ctx),
            "sentence" => Tag_Sentence(el, ctx),

            "person" => Tag_Person(el, ctx),
            "person2" => Tag_Person2(el, ctx),
            "gender" => Tag_Gender(el, ctx),

            // NEW
            "learn" => Tag_Learn(el),
            "date" => Tag_Date(el),

            // Unknown tags: evaluate children (container semantics)
            _ => EvalChildren(el, ctx),
        };
    }

    private string EvalChildren(XContainer parent, Context ctx)
    {
        StringBuilder sb = new ();
        foreach (XNode n in parent.Nodes())
        {
            sb.Append(EvalNode(n, ctx));
        }

        return sb.ToString();
    }

    // -----------------------------
    // Core tags
    // -----------------------------
    private string Tag_Srai(XElement el, Context ctx)
    {
        if (ctx.Depth >= _maxDepth)
        {
            return string.Empty;
        }

        string inner = EvalChildren(el, ctx);
        return _srai(inner, ctx.Session, ctx.Depth + 1) ?? string.Empty;
    }

    private string Tag_Sr(Context ctx)
    {
        if (ctx.Depth >= _maxDepth)
        {
            return string.Empty;
        }

        string inner = ctx.Match.Star(1);
        return _srai(inner, ctx.Session, ctx.Depth + 1) ?? string.Empty;
    }

    private string Tag_Think(XElement el, Context ctx)
    {
        _ = EvalChildren(el, ctx); // side-effects only
        return string.Empty;
    }

    private string Tag_Set(XElement el, Context ctx)
    {
        string name = el.Attribute("name")?.Value ?? string.Empty;
        string? value = EvalChildren(el, ctx);

        if (string.IsNullOrWhiteSpace(name))
        {
            return value;
        }

        if (name.Equals("topic", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Session.Topic = string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();
        }
        else
        {
            ctx.Session.Predicates.Set(name, value ?? string.Empty);
        }

        return value ?? string.Empty;
    }

    private string Tag_Bot(XElement el)
    {
        string name = el.Attribute("name")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return _bot.Properties.GetOrEmpty(name);
    }

    private string Tag_Random(XElement el, Context ctx)
    {
        List<string> items = [];
        foreach (XElement li in el.Elements())
        {
            if (li.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
            {
                items.Add(EvalChildren(li, ctx));
            }
        }

        // Drop empty results to avoid blank replies
        List<string> nonEmpty = items.FindAll(s => !string.IsNullOrWhiteSpace(s));
        if (nonEmpty.Count == 0)
        {
            return string.Empty;
        }

        int idx = _rng.Next(nonEmpty.Count);
        return nonEmpty[idx];
    }

    private string Tag_Condition(XElement e, Context ctx)
    {
        // If there are any <li> children, it's the LIST FORM, even if the parent has name="…"
        List<XElement> liElements = [];
        foreach (XElement child in e.Elements())
        {
            if (child.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
            {
                liElements.Add(child);
            }
        }

        if (liElements.Count > 0)
        {
            // LIST FORM
            string? parentName = e.Attribute("name")?.Value; // may be null; used as default li name
            XElement? defaultLi = null;

            foreach (XElement li in liElements)
            {
                string? effName = li.Attribute("name")?.Value ?? parentName;   // inherit from parent if missing
                string? value = li.Attribute("value")?.Value;                // may be null

                // True default branch: neither name nor value on this li
                if (string.IsNullOrEmpty(effName) && value is null)
                {
                    defaultLi ??= li;
                    continue;
                }

                string? cur = string.IsNullOrEmpty(effName)
                    ? string.Empty
                    : ctx.Session.Predicates.GetOrEmpty(effName);

                if (value is null)
                {
                    // "exists" branch: match if predicate is non-empty
                    if (!string.IsNullOrEmpty(cur))
                    {
                        return EvalChildren(li, ctx);
                    }
                }
                else
                {
                    // value="*" means wildcard (any non-empty)
                    if (value == "*")
                    {
                        if (!string.IsNullOrEmpty(cur))
                        {
                            return EvalChildren(li, ctx);
                        }
                    }
                    else if (string.Equals(cur, value, StringComparison.OrdinalIgnoreCase))
                    {
                        return EvalChildren(li, ctx);
                    }
                }
            }

            return defaultLi is null ? string.Empty : EvalChildren(defaultLi, ctx);
        }

        // SINGLE FORM: <condition name="x" value="y">…</condition>
        string? nameAttr = e.Attribute("name")?.Value;
        string? valueAttr = e.Attribute("value")?.Value;

        if (string.IsNullOrEmpty(nameAttr) || valueAttr is null)
        {
            return string.Empty; // malformed single-form; nothing to do
        }

        string current = ctx.Session.Predicates.GetOrEmpty(nameAttr);
        return string.Equals(current, valueAttr, StringComparison.OrdinalIgnoreCase)
            ? EvalChildren(e, ctx)
            : string.Empty;
    }

    private string Tag_Uppercase(XElement el, Context ctx)
    {
        string inner = EvalChildren(el, ctx);
        return inner.ToUpperInvariant();
    }

    private string Tag_Lowercase(XElement el, Context ctx)
    {
        string? inner = EvalChildren(el, ctx);
        return inner.ToLowerInvariant();
    }

    private string Tag_Formal(XElement el, Context ctx)
    {
        string inner = EvalChildren(el, ctx);
        return _pre.ToTitleCase(inner);
    }

    private string Tag_Sentence(XElement el, Context ctx)
    {
        string? inner = EvalChildren(el, ctx);
        return _pre.ToSentenceCase(inner);
    }

    // -----------------------------
    // NEW: <learn/> (Part 9)
    // -----------------------------
    private string Tag_Learn(XElement el)
    {
        // If learning is disabled, just ignore the block.
        if (_learn is null)
        {
            return string.Empty;
        }

        // Grab raw inner XML and wrap to ensure a single root for parsing.
        string innerXml = GetInnerXml(el);
        if (string.IsNullOrWhiteSpace(innerXml))
        {
            return string.Empty;
        }

        string wrapped = $"<learnwrap>{innerXml}</learnwrap>";
        XDocument xdoc;
        try
        {
            xdoc = XDocument.Parse(wrapped, LoadOptions.PreserveWhitespace);
        }
        catch
        {
            return string.Empty;
        }

        XElement root = xdoc.Root!;
        foreach (XElement node in root.Elements())
        {
            string local = node.Name.LocalName.ToLowerInvariant();
            if (local == "category")
            {
                EmitLearnFromCategory(node, "*");
            }
            else if (local == "topic")
            {
                string topicName = (node.Attribute("name")?.Value ?? "*").Trim();
                foreach (XElement cat in node.Elements())
                {
                    if (cat.Name.LocalName.Equals("category", StringComparison.OrdinalIgnoreCase))
                    {
                        EmitLearnFromCategory(cat, topicName);
                    }
                }
            }
        }

        // <learn> itself produces no output; surrounding text remains.
        return string.Empty;

        void EmitLearnFromCategory(XElement categoryEl, string topicWrapper)
        {
            string patternText = (categoryEl.Element(XName.Get("pattern"))?.Value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(patternText))
            {
                return; // skip malformed
            }

            string thatText = (categoryEl.Element(XName.Get("that"))?.Value ?? string.Empty).Trim();
            XElement? templateEl = categoryEl.Element(XName.Get("template"));
            if (templateEl is null)
            {
                return; // skip malformed
            }

            string templateXml = templateEl.ToString(SaveOptions.DisableFormatting);
            string topicText = string.IsNullOrWhiteSpace(topicWrapper) ? "*" : topicWrapper;

            // Normalize via PreProcessor (Program D style)
            string normPattern = _pre.NormalizePattern(patternText);
            string normThat = _pre.NormalizeThat(thatText);
            string normTopic = _pre.NormalizeTopic(topicText);

            // Emit to runtime
            _learn(normPattern, normThat, normTopic, templateXml, null);
        }
    }

    /// <summary>
    /// Applies first→second person swaps to the element's content,
    /// or to the first star capture when the element is empty.
    /// If the element is empty (<person/>) and no substitution occurs
    /// (e.g., the star is a noun like "PIZZA"), the reflected text is
    /// returned in lowercase (e.g., "pizza") to improve readability.
    /// </summary>
    private string Tag_Person(XElement el, Context ctx)
    {
        string inner = el.IsEmpty ? ctx.Match.Star(1) : EvalChildren(el, ctx);

        // Apply person transform first
        string transformed = PersonTransform.ApplyPerson(inner, _bot.Substitutions.Person);

        // Only lower-case when:
        //  - the tag is empty (<person/> uses <star/>), and
        //  - no substitution actually occurred (transformed == inner)
        if (el.IsEmpty && string.Equals(transformed, inner, StringComparison.Ordinal))
        {
            return transformed.ToLowerInvariant();
        }

        return transformed;
    }

    /// <summary>
    /// Applies second→first person swaps to the element's content,
    /// or to the first star capture when the element is empty.
    /// </summary>
    private string Tag_Person2(XElement el, Context ctx)
    {
        string inner = el.IsEmpty ? ctx.Match.Star(1) : EvalChildren(el, ctx);
        return PersonTransform.ApplyPerson2(inner, _bot.Substitutions.Person2);
    }

    /// <summary>
    /// Applies gender swaps to the element's content,
    /// or to the first star capture when the element is empty.
    /// </summary>
    private string Tag_Gender(XElement el, Context ctx)
    {
        string inner = el.IsEmpty ? ctx.Match.Star(1) : EvalChildren(el, ctx);
        return PersonTransform.ApplyGender(inner, _bot.Substitutions.Gender);
    }

    // -----------------------------
    // NEW: <date/> (Part 9)
    // -----------------------------
    private string Tag_Date(XElement el)
    {
        DateTime now = _nowProvider?.Invoke() ?? DateTime.Now;
        string? fmt = el.Attribute("format")?.Value;

        // Default format expected by tests
        string format = string.IsNullOrWhiteSpace(fmt) ? "yyyy-MM-dd HH:mm" : fmt;
        try
        {
            return now.ToString(format, CultureInfo.InvariantCulture);
        }
        catch
        {
            // If an invalid format is given, fall back to default
            return now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
