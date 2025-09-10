// =============================================================
// Genova.Alice.Tests — Part 8 (Step 2: TDD)
// Unit tests for Engine (chat loop & SRAI) — xUnit + FluentAssertions
// =============================================================

using System;
using FluentAssertions;
using Xunit;
using Genova.Alice;

namespace Genova.Alice.Tests;

public class EngineTests
{
    private static (Engine engine, UserSession session, PreProcessor pre, Graphmaster gm, Bot bot)
        Make()
    {
        var props = new BotProperties();
        props.Set("name", "ALICE");

        var subs = SubstitutionTables.CreateClassicDefaults();
        var bot = new Bot(props, subs, thatHistoryDepth: 4);
        var pre = new PreProcessor(subs);
        var gm = new Graphmaster();

        // Engine with default TemplateProcessor wired to Engine.Srai
        var engine = new Engine(bot, gm, pre, templates: null);
        var session = new UserSession("u1", bot);
        return (engine, session, pre, gm, bot);
    }

    [Fact]
    public void Respond_handles_multi_sentence_and_that_context()
    {
        string debug = "";
        foreach (string file in Directory.GetFiles("C:\\Git\\Genova.Alice\\Alice\\Data", "*.aiml"))
        {
            debug += file + Environment.NewLine;
        }
        File.WriteAllText("C:\\Temp\\debug.txt", debug);

        var (engine, session, pre, gm, _) = Make();

        // First category: literal HELLO -> "Hi."
        gm.AddCategory(
            pattern: pre.NormalizePattern("HELLO"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "Hi.");

        // Second category: requires THAT = "HI" (normalized from "Hi.")
        gm.AddCategory(
            pattern: pre.NormalizePattern("HOW ARE YOU"),
            that: pre.NormalizePattern("HI"),
            topic: pre.NormalizePattern("*"),
            template: "I'm fine.");

        var reply = engine.Respond("hello. how are you?", session);

        reply.Should().Be("Hi. I'm fine.");

        // THAT should now be most recent bot reply
        session.That.Should().Be("I'm fine.");
        session.ThatHistory.At(1).Should().Be("I'm fine.");
        session.ThatHistory.At(2).Should().Be("Hi.");
    }

    [Fact]
    public void Respond_updates_predicates_and_persists_across_sentences()
    {
        var (engine, session, pre, gm, _) = Make();

        gm.AddCategory(
            pattern: pre.NormalizePattern("MY NAME IS *"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "<think><set name=\"name\"><star/></set></think>Nice to meet you, <get name=\"name\"/>.");

        gm.AddCategory(
            pattern: pre.NormalizePattern("WHAT IS MY NAME"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "You told me your name is <get name=\"name\"/>.");

        var reply = engine.Respond("my name is Alice. what is my name", session);

        reply.Should().Be("Nice to meet you, ALICE. You told me your name is ALICE.");
        session.Predicates.GetOrEmpty("name").Should().Be("ALICE");
    }

    [Fact]
    public void Srai_executes_single_cycle_without_affecting_input_history()
    {
        var (engine, session, pre, gm, _) = Make();

        gm.AddCategory(
            pattern: pre.NormalizePattern("WHAT IS YOUR NAME"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "<srai>TELL ME YOUR NAME</srai>");

        gm.AddCategory(
            pattern: pre.NormalizePattern("TELL ME YOUR NAME"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "ALICE");

        // Normal respond
        var r1 = engine.Respond("What is your name?", session);
        r1.Should().Be("ALICE");

        var inputsBefore = session.InputHistory.Count;

        // Direct SRAI call should NOT push a new input
        var r2 = engine.Srai("TELL ME YOUR NAME", session, depth: 0);
        r2.Should().Be("ALICE");

        session.InputHistory.Count.Should().Be(inputsBefore);
    }

    [Fact]
    public void Respond_pushes_original_input_into_history()
    {
        var (engine, session, pre, gm, _) = Make();

        gm.AddCategory(
            pattern: pre.NormalizePattern("HELLO"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "Hi.");

        var input = "hello";
        var reply = engine.Respond(input, session);

        reply.Should().Be("Hi.");
        session.InputHistory.At(1).Should().Be(input);
    }
}
