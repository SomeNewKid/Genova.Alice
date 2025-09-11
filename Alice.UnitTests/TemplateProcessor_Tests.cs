// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class TemplateProcessor_Tests
{
    private sealed class SraiProbe
    {
        public int Calls { get; private set; }
        public string? LastInput { get; private set; }
        public int LastDepth { get; private set; }
        public string ReturnValue { get; set; } = string.Empty;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Expected parameter")]
        public string Invoke(string input, UserSession session, int depth)
        {
            Calls++;
            LastInput = input;
            LastDepth = depth;
            return ReturnValue;
        }
    }

    private static (TemplateProcessor tp, UserSession session, Match match, SraiProbe srai)
        MakeProc(Bot? bot = null, string? star1 = null, string? that1 = null, string? topic1 = null, int? rngSeed = null)
    {
        BotProperties props = new ();
        SubstitutionTables subs = SubstitutionTables.CreateClassicDefaults();
        Bot theBot = bot ?? new (props, subs, thatHistoryDepth: 4);

        PreProcessor pre = new (subs);
        SraiProbe sraiProbe = new ();

        Random rng = rngSeed.HasValue ? new (rngSeed.Value) : new (0);
        TemplateProcessor tp = new (theBot, pre, sraiProbe.Invoke, rng);

        // Session
        UserSession session = new ("u1", theBot);

        // Match with optional captures
        Path path = Path.FromSegments(["I", "AM", "*"], ["*"], ["*"]);

        Stars stars = new ();
        if (star1 != null) stars.AddStar(star1);
        if (that1 != null) stars.AddThatStar(that1);
        if (topic1 != null) stars.AddTopicStar(topic1);

        Category cat = new ("I AM *", "*", "*", "<template/>");
        Match match = new (cat, path, stars, "I AM *", "*", "*");

        return (tp, session, match, sraiProbe);
    }

    [Fact]
    public void Text_nodes_and_literal_XML_are_preserved()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc();

        string xml = "<template>Hello world.</template>";
        tp.Process(xml, session, match).Should().Be("Hello world.");
    }

    [Fact]
    public void Srai_invoker_is_called_with_inner_text_and_depth_incremented()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe probe) = MakeProc();
        probe.ReturnValue = "I AM ALICE.";

        string xml = "<template><srai>WHAT IS YOUR NAME</srai></template>";
        string result = tp.Process(xml, session, match, depth: 1);

        result.Should().Be("I AM ALICE.");
        probe.Calls.Should().Be(1);
        probe.LastInput.Should().Be("WHAT IS YOUR NAME");
        probe.LastDepth.Should().Be(2); // incremented
    }

    [Fact]
    public void Sr_shorthand_uses_first_star_capture()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe probe) = MakeProc(star1: "HELLO");
        probe.ReturnValue = "OK";

        string xml = "<template><sr/></template>";
        string result = tp.Process(xml, session, match);

        probe.Calls.Should().Be(1);
        probe.LastInput.Should().Be("HELLO");
        result.Should().Be("OK");
    }

    [Fact]
    public void Star_thatstar_topicstar_return_expected_captures()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) =
            MakeProc(star1: "SAD", that1: "OK", topic1: "JOKES");

        string xml = "<template><star/>|<thatstar/>|<topicstar/></template>";
        string result = tp.Process(xml, session, match);

        result.Should().Be("SAD|OK|JOKES");

        // Index attribute
        xml = "<template><star index=\"2\"/>-<thatstar index=\"2\"/>-<topicstar index=\"2\"/></template>";
        result = tp.Process(xml, session, match);
        result.Should().Be("--"); // all empty (no index 2)
    }

    [Fact]
    public void Think_executes_side_effects_without_output()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc();

        string xml = "<template>Hi<think><set name=\"name\">ALICE</set></think>!</template>";
        string result = tp.Process(xml, session, match);

        result.Should().Be("Hi!");
        session.Predicates.GetOrEmpty("name").Should().Be("ALICE");
    }

    [Fact]
    public void Set_returns_value_and_get_reads_it()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc();

        string xml = "<template><set name=\"name\">ALICE</set> is <get name=\"name\"/></template>";
        string result = tp.Process(xml, session, match);

        result.Should().Be("ALICE is ALICE");
    }

    [Fact]
    public void Bot_returns_property_values_case_insensitive()
    {
        BotProperties props = new ();
        props.Set("Name", "ALICE");
        Bot bot = new (props, SubstitutionTables.CreateClassicDefaults(), 4);
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc(bot: bot);

        string xml = "<template><bot name=\"name\"/></template>";
        string result = tp.Process(xml, session, match);

        result.Should().Be("ALICE");
    }

    [Fact]
    public void Random_picks_one_of_list_items()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc(rngSeed: 123);

        string xml = "<template><random><li>A</li><li>B</li><li>C</li></random></template>";
        string result = tp.Process(xml, session, match);

        string[] abc = ["A", "B", "C"];
        abc.Should().Contain(result);
    }

    [Fact]
    public void Condition_attribute_form_matches_value_or_returns_empty()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc();

        // Not set -> empty
        string xml = "<template><condition name=\"mood\" value=\"HAPPY\">Yay</condition></template>";
        tp.Process(xml, session, match).Should().BeEmpty();

        // Set then match
        session.Predicates.Set("mood", "HAPPY");
        tp.Process(xml, session, match).Should().Be("Yay");
    }

    [Fact]
    public void Condition_list_form_supports_named_and_default_li()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc();

        string xml = """
                  <template>
                    <condition>
                      <li name="mood" value="SAD">Oh no</li>
                      <li>OK</li>
                    </condition>
                  </template>
                  """;

        // Default branch because mood not set
        tp.Process(xml, session, match).Should().Be("OK");

        // Named branch when predicate matches
        session.Predicates.Set("mood", "SAD");
        tp.Process(xml, session, match).Should().Be("Oh no");
    }

    [Fact]
    public void Casing_transforms_apply_expected_output()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe _) = MakeProc();

        string xmlUpper = "<template><uppercase>hELLo wORLD</uppercase></template>";
        tp.Process(xmlUpper, session, match).Should().Be("HELLO WORLD");

        string xmlLower = "<template><lowercase>HeLLo WoRLD</lowercase></template>";
        tp.Process(xmlLower, session, match).Should().Be("hello world");

        string xmlFormal = "<template><formal>hELLo wORLD</formal></template>";
        tp.Process(xmlFormal, session, match).Should().Be("Hello World");

        string xmlSentence = "<template><sentence>hELLo wORLD</sentence></template>";
        tp.Process(xmlSentence, session, match).Should().Be("Hello world");
    }

    [Fact]
    public void Srai_is_suppressed_when_depth_limit_reached()
    {
        (TemplateProcessor tp, UserSession session, Match match, SraiProbe probe) = MakeProc();
        probe.ReturnValue = "SHOULD_NOT_APPEAR";

        // Invoke with depth already at limit; processor should not call SRAI and return empty
        string xml = "<template><srai>LOOP</srai></template>";
        string result = tp.Process(xml, session, match, depth: TemplateProcessor.DefaultMaxDepth);

        result.Should().BeEmpty();
        probe.Calls.Should().Be(0);
    }

    [Fact]
    public void Date_without_format_uses_default_pattern()
    {
        // Fixed clock for determinism: 2025-09-07 14:30:00
        DateTime fixedNow = new(2025, 09, 07, 14, 30, 00);

        BotProperties props = new();
        SubstitutionTables subs = SubstitutionTables.CreateClassicDefaults();
        Bot bot = new(props, subs, thatHistoryDepth: 4);
        PreProcessor pre = new(subs);

        TemplateProcessor tp = new(
            bot,
            pre,
            sraiInvoker: (text, s, d) => string.Empty,
            rng: new Random(1),
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: null,
            nowProvider: () => fixedNow);

        UserSession session = new("u1", bot);
        Path path = Path.FromSegments(["*"], ["*"], ["*"]);
        Stars stars = new();
        Category cat = new("*", "*", "*", "<template/>");
        Match match = new(cat, path, stars, "*", "*", "*");

        string xml = "<template>The time is <date/></template>";
        string result = tp.Process(xml, session, match);

        // Default we will implement as "yyyy-MM-dd HH:mm"
        result.Should().Be("The time is 2025-09-07 14:30");
    }

    [Fact]
    public void Date_with_format_attribute_honors_dotnet_format_string()
    {
        DateTime fixedNow = new(2025, 09, 07, 14, 30, 00);

        BotProperties props = new();
        SubstitutionTables subs = SubstitutionTables.CreateClassicDefaults();
        Bot bot = new(props, subs, thatHistoryDepth: 4);
        PreProcessor pre = new(subs);

        TemplateProcessor tp = new(
            bot,
            pre,
            sraiInvoker: (text, s, d) => string.Empty,
            rng: new Random(1),
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: null,
            nowProvider: () => fixedNow);

        UserSession session = new("u1", bot);
        Path path = Path.FromSegments(["*"], ["*"], ["*"]);
        Stars stars = new();
        Category cat = new("*", "*", "*", "<template/>");
        Match match = new(cat, path, stars, "*", "*", "*");

        string xml = "<template>Today is <date format=\"yyyy/MM/dd\"/></template>";
        string result = tp.Process(xml, session, match);

        result.Should().Be("Today is 2025/09/07");
    }

    private static (TemplateProcessor tp, UserSession session, Match match, LearnCapture cap, PreProcessor pre, Bot bot)
        Make(LearnCapture? capture = null, DateTime? fixedNow = null)
    {
        BotProperties props = new();
        SubstitutionTables subs = SubstitutionTables.CreateClassicDefaults();
        Bot bot = new(props, subs, thatHistoryDepth: 4);
        PreProcessor pre = new(subs);
        LearnCapture capInst = capture ?? new();

        // Deterministic RNG for <random> (not used here, but keeps consistency)
        Random rng = new(42);

        // ctor: (bot, pre, sraiInvoker, rng, maxDepth, learnEmitter, nowProvider)
        LearnEmitter learn = capInst.Emit;
        DateTime clock() => fixedNow ?? new DateTime(2025, 09, 07, 14, 30, 00);

        TemplateProcessor tp = new(
            bot,
            pre,
            sraiInvoker: (text, s, d) => string.Empty,
            rng: rng,
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: learn,
            nowProvider: clock);

        UserSession session = new("u1", bot);

        // Minimal match; stars not required for <learn/>
        Path path = Path.FromSegments(["*"], ["*"], ["*"]);
        Stars stars = new();
        Category cat = new("*", "*", "*", "<template/>");
        Match match = new(cat, path, stars, "*", "*", "*");

        return (tp, session, match, capInst, pre, bot);
    }

    [Fact]
    public void Learn_emits_single_category_and_returns_no_output_while_surrounding_text_remains()
    {
        (TemplateProcessor tp, UserSession session, Match match, LearnCapture cap, PreProcessor pre, Bot _) = Make();

        // One root-level category inside <learn>, followed by literal "OK"
        string xml = """
                  <template>
                    <learn>
                      <category>
                        <pattern>What is your name?</pattern>
                        <template>ALICE</template>
                      </category>
                    </learn>
                    OK
                  </template>
                  """;

        string output = tp.Process(xml, session, match);

        // Emitted exactly once, normalized
        cap.Calls.Should().Be(1);
        LearnedCategory rec = cap.Records.Single();
        rec.pattern.Should().Be(pre.NormalizePattern("What is your name?"));
        rec.that.Should().Be(pre.NormalizeThat(string.Empty)); // no <that> -> empty normalized to ""
        rec.topic.Should().Be(pre.NormalizeTopic("*"));
        rec.templateXml.Should().Be("<template>ALICE</template>");

        // Output preserves only surrounding text, not the <learn> block
        output.Should().Be("OK");
    }

    [Fact]
    public void Learn_supports_topic_wrapper_and_multiple_categories()
    {
        (TemplateProcessor tp, UserSession session, Match match, LearnCapture cap, PreProcessor pre, Bot _) = Make();

        string xml = """
                  <template>
                    <learn>
                      <topic name="Jokes">
                        <category>
                          <pattern>Tell me a joke</pattern>
                          <template>Knock knock.</template>
                        </category>
                        <category>
                          <pattern>Another joke</pattern>
                          <that>OK!</that>
                          <template>Banana.</template>
                        </category>
                      </topic>
                    </learn>
                    LEARNED
                  </template>
                  """;

        string output = tp.Process(xml, session, match);

        cap.Calls.Should().Be(2);

        LearnedCategory r1 = cap.Records[0];
        r1.pattern.Should().Be(pre.NormalizePattern("Tell me a joke"));
        r1.that.Should().Be(pre.NormalizeThat(string.Empty));
        r1.topic.Should().Be(pre.NormalizeTopic("Jokes"));
        r1.templateXml.Should().Be("<template>Knock knock.</template>");

        LearnedCategory r2 = cap.Records[1];
        r2.pattern.Should().Be(pre.NormalizePattern("Another joke"));
        r2.that.Should().Be(pre.NormalizeThat("OK!"));
        r2.topic.Should().Be(pre.NormalizeTopic("Jokes"));
        r2.templateXml.Should().Be("<template>Banana.</template>");

        output.Should().Be("LEARNED");
    }

    private sealed class LearnCapture
    {
        public int Calls => Records.Count;

        public readonly List<LearnedCategory> Records = [];

        public void Emit(string p, string t, string topic, string tmpl, string? src)
            => Records.Add(new LearnedCategory(p, t, topic, tmpl, src));
    }
}
