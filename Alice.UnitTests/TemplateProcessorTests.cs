// =============================================================
// Genova.Alice.Tests — Part 6 (Step 2: TDD)
// Unit tests for TemplateProcessor (xUnit + FluentAssertions)
// Scope covered: text nodes, <srai>, <sr>, <star/thatstar/topicstar>, <think>,
//                <set>/<get>, <bot>, <random>/<li>, <condition> (both forms),
//                casing transforms, recursion depth limit.
// =============================================================

using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Genova.Alice;

namespace Genova.Alice.Tests;

public class TemplateProcessorTests
{
    private sealed class SraiProbe
    {
        public int Calls { get; private set; }
        public string? LastInput { get; private set; }
        public int LastDepth { get; private set; }
        public string ReturnValue { get; set; } = string.Empty;

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
        var props = new BotProperties();
        var subs = SubstitutionTables.CreateClassicDefaults();
        var theBot = bot ?? new Bot(props, subs, thatHistoryDepth: 4);

        var pre = new PreProcessor(subs);
        var sraiProbe = new SraiProbe();

        var rng = rngSeed.HasValue ? new Random(rngSeed.Value) : new Random(0);
        var tp = new TemplateProcessor(theBot, pre, sraiProbe.Invoke, rng);

        // Session
        var session = new UserSession("u1", theBot);

        // Match with optional captures
        var path = Path.FromSegments(
            new[] { "I", "AM", "*" },
            new[] { "*" },
            new[] { "*" });

        var stars = new Stars();
        if (star1 != null) stars.AddStar(star1);
        if (that1 != null) stars.AddThatStar(that1);
        if (topic1 != null) stars.AddTopicStar(topic1);

        var cat = new Category("I AM *", "*", "*", "<template/>");
        var match = new Match(cat, path, stars, "I AM *", "*", "*");

        return (tp, session, match, sraiProbe);
    }

    [Fact]
    public void Text_nodes_and_literal_XML_are_preserved()
    {
        var (tp, session, match, _) = MakeProc();

        var xml = "<template>Hello world.</template>";
        tp.Process(xml, session, match).Should().Be("Hello world.");
    }

    [Fact]
    public void Srai_invoker_is_called_with_inner_text_and_depth_incremented()
    {
        var (tp, session, match, probe) = MakeProc();
        probe.ReturnValue = "I AM ALICE.";

        var xml = "<template><srai>WHAT IS YOUR NAME</srai></template>";
        var result = tp.Process(xml, session, match, depth: 1);

        result.Should().Be("I AM ALICE.");
        probe.Calls.Should().Be(1);
        probe.LastInput.Should().Be("WHAT IS YOUR NAME");
        probe.LastDepth.Should().Be(2); // incremented
    }

    [Fact]
    public void Sr_shorthand_uses_first_star_capture()
    {
        var (tp, session, match, probe) = MakeProc(star1: "HELLO");
        probe.ReturnValue = "OK";

        var xml = "<template><sr/></template>";
        var result = tp.Process(xml, session, match);

        probe.Calls.Should().Be(1);
        probe.LastInput.Should().Be("HELLO");
        result.Should().Be("OK");
    }

    [Fact]
    public void Star_thatstar_topicstar_return_expected_captures()
    {
        var (tp, session, match, _) = MakeProc(star1: "SAD", that1: "OK", topic1: "JOKES");

        var xml = "<template><star/>|<thatstar/>|<topicstar/></template>";
        var result = tp.Process(xml, session, match);

        result.Should().Be("SAD|OK|JOKES");

        // Index attribute
        xml = "<template><star index=\"2\"/>-<thatstar index=\"2\"/>-<topicstar index=\"2\"/></template>";
        result = tp.Process(xml, session, match);
        result.Should().Be("--"); // all empty (no index 2)
    }

    [Fact]
    public void Think_executes_side_effects_without_output()
    {
        var (tp, session, match, _) = MakeProc();

        var xml = "<template>Hi<think><set name=\"name\">ALICE</set></think>!</template>";
        var result = tp.Process(xml, session, match);

        result.Should().Be("Hi!");
        session.Predicates.GetOrEmpty("name").Should().Be("ALICE");
    }

    [Fact]
    public void Set_returns_value_and_get_reads_it()
    {
        var (tp, session, match, _) = MakeProc();

        var xml = "<template><set name=\"name\">ALICE</set> is <get name=\"name\"/></template>";
        var result = tp.Process(xml, session, match);

        result.Should().Be("ALICE is ALICE");
    }

    [Fact]
    public void Bot_returns_property_values_case_insensitive()
    {
        var props = new BotProperties();
        props.Set("Name", "ALICE");
        var bot = new Bot(props, SubstitutionTables.CreateClassicDefaults(), 4);
        var (tp, session, match, _) = MakeProc(bot: bot);

        var xml = "<template><bot name=\"name\"/></template>";
        var result = tp.Process(xml, session, match);

        result.Should().Be("ALICE");
    }

    [Fact]
    public void Random_picks_one_of_list_items()
    {
        var (tp, session, match, _) = MakeProc(rngSeed: 123);

        var xml = "<template><random><li>A</li><li>B</li><li>C</li></random></template>";
        var result = tp.Process(xml, session, match);

        new[] { "A", "B", "C" }.Should().Contain(result);
    }

    [Fact]
    public void Condition_attribute_form_matches_value_or_returns_empty()
    {
        var (tp, session, match, _) = MakeProc();

        // Not set -> empty
        var xml = "<template><condition name=\"mood\" value=\"HAPPY\">Yay</condition></template>";
        tp.Process(xml, session, match).Should().BeEmpty();

        // Set then match
        session.Predicates.Set("mood", "HAPPY");
        tp.Process(xml, session, match).Should().Be("Yay");
    }

    [Fact]
    public void Condition_list_form_supports_named_and_default_li()
    {
        var (tp, session, match, _) = MakeProc();

        var xml = """
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
        var (tp, session, match, _) = MakeProc();

        var xmlUpper = "<template><uppercase>hELLo wORLD</uppercase></template>";
        tp.Process(xmlUpper, session, match).Should().Be("HELLO WORLD");

        var xmlLower = "<template><lowercase>HeLLo WoRLD</lowercase></template>";
        tp.Process(xmlLower, session, match).Should().Be("hello world");

        var xmlFormal = "<template><formal>hELLo wORLD</formal></template>";
        tp.Process(xmlFormal, session, match).Should().Be("Hello World");

        var xmlSentence = "<template><sentence>hELLo wORLD</sentence></template>";
        tp.Process(xmlSentence, session, match).Should().Be("Hello world");
    }

    [Fact]
    public void Srai_is_suppressed_when_depth_limit_reached()
    {
        var (tp, session, match, probe) = MakeProc();
        probe.ReturnValue = "SHOULD_NOT_APPEAR";

        // Invoke with depth already at limit; processor should not call SRAI and return empty
        var xml = "<template><srai>LOOP</srai></template>";
        var result = tp.Process(xml, session, match, depth: TemplateProcessor.DefaultMaxDepth);

        result.Should().BeEmpty();
        probe.Calls.Should().Be(0);
    }
}
