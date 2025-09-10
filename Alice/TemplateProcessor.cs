// =============================================================
// Genova.Alice.Core — Part 9 (Implementations for TemplateProcessor additions)
// Implements: <learn/> (in-memory emit via LearnEmitter) and <date/> tag.
// Also extends the constructor to accept learnEmitter and nowProvider.
// NOTE: This is a full replacement for TemplateProcessor to make patching easy.
// =============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Genova.Alice;

internal delegate string SraiInvoker(string input, UserSession session, int depth);

internal interface ITemplateTagHandler
{
    string Evaluate(XElement element, TemplateProcessor.Context ctx);
}

internal sealed class TemplateProcessor
{
    internal const int DefaultMaxDepth = 10;

    private readonly Bot _bot;
    private readonly PreProcessor _pre;
    private readonly SraiInvoker _srai;
    private readonly Random _rng;
    private readonly int _maxDepth;

    // NEW: optional collaborators for Part 9
    internal delegate void LearnEmitter(string pattern, string that, string topic, string templateXml, string? sourceName);
    private readonly LearnEmitter? _learn;
    private readonly Func<DateTime>? _nowProvider;

    private readonly Dictionary<string, ITemplateTagHandler> _handlers =
        new(StringComparer.OrdinalIgnoreCase);

    internal sealed class Context
    {
        internal UserSession Session { get; }
        internal Match Match { get; }
        internal int Depth { get; }
        internal TemplateProcessor Processor { get; }

        internal Context(TemplateProcessor processor, UserSession session, Match match, int depth)
        {
            Processor = processor ?? throw new ArgumentNullException(nameof(processor));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Match = match ?? throw new ArgumentNullException(nameof(match));
            Depth = depth;
        }
    }

    internal TemplateProcessor(Bot bot,
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

    internal string Process(string templateXml, UserSession session, Match match, int depth = 0)
    {
        if (string.IsNullOrWhiteSpace(templateXml)) return string.Empty;

        // 1) Try as-is: if it's a full <template>…</template> doc, evaluate it.
        try
        {
            var xdoc1 = XDocument.Parse(templateXml, LoadOptions.PreserveWhitespace);
            var root1 = xdoc1.Root;
            if (root1 != null && root1.Name.LocalName.Equals("template", StringComparison.OrdinalIgnoreCase))
            {
                var ctx1 = new Context(this, session, match, depth);
                var result1 = EvalChildren(root1, ctx1);
                return result1.Trim(); // <-- trim final output
            }
        }
        catch (System.Xml.XmlException)
        {
            // fall through to wrapper attempt
        }

        // 2) Wrap fragments or plain text in <template>…</template> and try again.
        var wrapped = $"<template>{templateXml}</template>";
        try
        {
            var xdoc2 = XDocument.Parse(wrapped, LoadOptions.PreserveWhitespace);
            var root2 = xdoc2.Root!;
            var ctx2 = new Context(this, session, match, depth);
            var result2 = EvalChildren(root2, ctx2);
            return result2.Trim(); // <-- trim final output
        }
        catch (System.Xml.XmlException)
        {
            // 3) Last resort: return literal (don’t blow up runtime)
            return templateXml.Trim(); // <-- trim literal fallback too
        }
    }


    // -----------------------------
    // Registration & extensibility
    // -----------------------------
    internal void RegisterHandler(string tagName, ITemplateTagHandler handler)
    {
        if (string.IsNullOrWhiteSpace(tagName)) throw new ArgumentNullException(nameof(tagName));
        _handlers[tagName] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    internal bool TryGetHandler(string tagName, out ITemplateTagHandler? handler)
    {
        return _handlers.TryGetValue(tagName, out handler);
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
        var name = el.Name.LocalName.ToLowerInvariant();

        // Handler override?
        if (TryGetHandler(name, out var handler))
            return handler!.Evaluate(el, ctx);

        return name switch
        {
            "srai" => Tag_Srai(el, ctx),
            "sr" => Tag_Sr(el, ctx),

            "star" => Tag_Star(el, ctx),
            "thatstar" => Tag_ThatStar(el, ctx),
            "topicstar" => Tag_TopicStar(el, ctx),

            "think" => Tag_Think(el, ctx),
            "set" => Tag_Set(el, ctx),
            "get" => Tag_Get(el, ctx),
            "bot" => Tag_Bot(el, ctx),

            "random" => Tag_Random(el, ctx),
            "condition" => Tag_Condition(el, ctx),

            "uppercase" => Tag_Uppercase(el, ctx),
            "lowercase" => Tag_Lowercase(el, ctx),
            "formal" => Tag_Formal(el, ctx),
            "sentence" => Tag_Sentence(el, ctx),

            // NEW
            "learn" => Tag_Learn(el, ctx),
            "date" => Tag_Date(el, ctx),

            // Unknown tags: evaluate children (container semantics)
            _ => EvalChildren(el, ctx),
        };
    }

    private string EvalChildren(XContainer parent, Context ctx)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var n in parent.Nodes())
            sb.Append(EvalNode(n, ctx));
        return sb.ToString();
    }

    // -----------------------------
    // Core tags
    // -----------------------------
    private string Tag_Srai(XElement el, Context ctx)
    {
        var inner = EvalChildren(el, ctx);
        File.AppendAllText(@"C:\temp\srai-trace.txt",
            $"SRAI(\"{inner}\") with THAT=\"{ctx.Session.GetThatOrStar()}\" TOPIC=\"{ctx.Session.Topic}\"\r\n");

        if (ctx.Depth >= _maxDepth) return string.Empty;
        return _srai(inner, ctx.Session, ctx.Depth + 1) ?? string.Empty;
    }

    private string Tag_Sr(XElement el, Context ctx)
    {
        if (ctx.Depth >= _maxDepth) return string.Empty;
        var inner = ctx.Match.Star(1);
        return _srai(inner, ctx.Session, ctx.Depth + 1) ?? string.Empty;
    }

    private static int ReadIndex1(XElement el, int defaultValue = 1)
    {
        var attr = el.Attribute("index")?.Value;
        if (string.IsNullOrWhiteSpace(attr)) return defaultValue;
        return int.TryParse(attr, out var i) && i > 0 ? i : defaultValue;
    }

    private string Tag_Star(XElement el, Context ctx)
    {
        var i = ReadIndex1(el, 1);
        return ctx.Match.Star(i) ?? string.Empty;
    }

    private string Tag_ThatStar(XElement el, Context ctx)
    {
        var i = ReadIndex1(el, 1);
        return ctx.Match.ThatStar(i) ?? string.Empty;
    }

    private string Tag_TopicStar(XElement el, Context ctx)
    {
        var i = ReadIndex1(el, 1);
        return ctx.Match.TopicStar(i) ?? string.Empty;
    }

    private string Tag_Think(XElement el, Context ctx)
    {
        _ = EvalChildren(el, ctx); // side-effects only
        return string.Empty;
    }

    private string Tag_Set(XElement el, Context ctx)
    {
        var name = el.Attribute("name")?.Value ?? string.Empty;
        var value = EvalChildren(el, ctx);

        if (string.IsNullOrWhiteSpace(name)) return value;

        if (name.Equals("topic", StringComparison.OrdinalIgnoreCase))
            ctx.Session.Topic = string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();
        else
            ctx.Session.Predicates.Set(name, value ?? string.Empty);

        return value ?? string.Empty;
    }

    private string Tag_Get(XElement el, Context ctx)
    {
        var name = el.Attribute("name")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        if (name.Equals("topic", StringComparison.OrdinalIgnoreCase))
            return ctx.Session.Topic;

        return ctx.Session.Predicates.GetOrEmpty(name);
    }

    private string Tag_Bot(XElement el, Context ctx)
    {
        var name = el.Attribute("name")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        return _bot.Properties.GetOrEmpty(name);
    }

    private string Tag_Random(XElement el, Context ctx)
    {
        var items = new List<string>();
        foreach (var li in el.Elements())
            if (li.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
                items.Add(EvalChildren(li, ctx));

        // Drop empty results to avoid blank replies
        var nonEmpty = items.FindAll(s => !string.IsNullOrWhiteSpace(s));
        if (nonEmpty.Count == 0) return string.Empty;

        var idx = _rng.Next(nonEmpty.Count);
        return nonEmpty[idx];
    }

    private string Tag_Condition(XElement e, Context ctx)
    {
        // If there are any <li> children, it's the LIST FORM, even if the parent has name="…"
        var liElements = new List<XElement>();
        foreach (var child in e.Elements())
        {
            if (child.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
                liElements.Add(child);
        }

        if (liElements.Count > 0)
        {
            // LIST FORM
            var parentName = e.Attribute("name")?.Value; // may be null; used as default li name
            XElement? defaultLi = null;

            foreach (var li in liElements)
            {
                var effName = li.Attribute("name")?.Value ?? parentName;   // inherit from parent if missing
                var value = li.Attribute("value")?.Value;                // may be null

                // True default branch: neither name nor value on this li
                if (string.IsNullOrEmpty(effName) && value is null)
                {
                    defaultLi ??= li;
                    continue;
                }

                var cur = string.IsNullOrEmpty(effName)
                    ? string.Empty
                    : ctx.Session.Predicates.GetOrEmpty(effName);

                if (value is null)
                {
                    // "exists" branch: match if predicate is non-empty
                    if (!string.IsNullOrEmpty(cur))
                        return EvalChildren(li, ctx);
                }
                else
                {
                    // value="*" means wildcard (any non-empty)
                    if (value == "*")
                    {
                        if (!string.IsNullOrEmpty(cur))
                            return EvalChildren(li, ctx);
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
        var nameAttr = e.Attribute("name")?.Value;
        var valueAttr = e.Attribute("value")?.Value;

        if (string.IsNullOrEmpty(nameAttr) || valueAttr is null)
            return string.Empty; // malformed single-form; nothing to do

        var current = ctx.Session.Predicates.GetOrEmpty(nameAttr);
        return string.Equals(current, valueAttr, StringComparison.OrdinalIgnoreCase)
            ? EvalChildren(e, ctx)
            : string.Empty;
    }

    private string Tag_Uppercase(XElement el, Context ctx)
    {
        var inner = EvalChildren(el, ctx);
        return inner.ToUpperInvariant();
    }

    private string Tag_Lowercase(XElement el, Context ctx)
    {
        var inner = EvalChildren(el, ctx);
        return inner.ToLowerInvariant();
    }

    private string Tag_Formal(XElement el, Context ctx)
    {
        var inner = EvalChildren(el, ctx);
        return _pre.ToTitleCase(inner);
    }

    private string Tag_Sentence(XElement el, Context ctx)
    {
        var inner = EvalChildren(el, ctx);
        return _pre.ToSentenceCase(inner);
    }

    // -----------------------------
    // NEW: <learn/> (Part 9)
    // -----------------------------
    private string Tag_Learn(XElement el, Context ctx)
    {
        // If learning is disabled, just ignore the block.
        if (_learn is null) return string.Empty;

        // Grab raw inner XML and wrap to ensure a single root for parsing.
        var innerXml = GetInnerXml(el);
        if (string.IsNullOrWhiteSpace(innerXml)) return string.Empty;

        var wrapped = $"<learnwrap>{innerXml}</learnwrap>";
        XDocument xdoc;
        try
        {
            xdoc = XDocument.Parse(wrapped, LoadOptions.PreserveWhitespace);
        }
        catch
        {
            return string.Empty;
        }

        var root = xdoc.Root!;
        foreach (var node in root.Elements())
        {
            var local = node.Name.LocalName.ToLowerInvariant();
            if (local == "category")
            {
                EmitLearnFromCategory(node, "*");
            }
            else if (local == "topic")
            {
                var topicName = (node.Attribute("name")?.Value ?? "*").Trim();
                foreach (var cat in node.Elements())
                {
                    if (cat.Name.LocalName.Equals("category", StringComparison.OrdinalIgnoreCase))
                        EmitLearnFromCategory(cat, topicName);
                }
            }
        }

        // <learn> itself produces no output; surrounding text remains.
        return string.Empty;

        void EmitLearnFromCategory(XElement categoryEl, string topicWrapper)
        {
            var patternText = (categoryEl.Element(XName.Get("pattern"))?.Value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(patternText)) return; // skip malformed

            var thatText = (categoryEl.Element(XName.Get("that"))?.Value ?? string.Empty).Trim();
            var templateEl = categoryEl.Element(XName.Get("template"));
            if (templateEl is null) return; // skip malformed

            var templateXml = templateEl.ToString(SaveOptions.DisableFormatting);
            var topicText = string.IsNullOrWhiteSpace(topicWrapper) ? "*" : topicWrapper;

            // Normalize via PreProcessor (Program D style)
            var normPattern = _pre.NormalizePattern(patternText);
            var normThat = _pre.NormalizeThat(thatText);
            var normTopic = _pre.NormalizeTopic(topicText);

            // Emit to runtime
            _learn(normPattern, normThat, normTopic, templateXml, null);
        }
    }

    private static string GetInnerXml(XElement el)
    {
        if (!el.HasElements && el.Value.Length == 0) return string.Empty;
        return string.Concat(el.Nodes().Select(n => n.ToString(SaveOptions.DisableFormatting)));
    }

    // -----------------------------
    // NEW: <date/> (Part 9)
    // -----------------------------
    private string Tag_Date(XElement el, Context ctx)
    {
        var now = _nowProvider?.Invoke() ?? DateTime.Now;
        var fmt = el.Attribute("format")?.Value;

        // Default format expected by tests
        var format = string.IsNullOrWhiteSpace(fmt) ? "yyyy-MM-dd HH:mm" : fmt;
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
