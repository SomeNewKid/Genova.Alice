// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class Engine_Tests
{
    private static (Engine engine, UserSession session, PreProcessor pre, Graphmaster gm, Bot bot)
        Make()
    {
        BotProperties props = new BotProperties();
        props.Set("name", "ALICE");

        SubstitutionTables subs = SubstitutionTables.CreateClassicDefaults();
        Bot bot = new (props, subs, thatHistoryDepth: 4);
        PreProcessor pre = new (subs);
        Graphmaster gm = new ();

        // Engine with default TemplateProcessor wired to Engine.Srai
        Engine engine = new (bot, gm, pre, templates: null);
        UserSession session = new ("u1", bot);
        return (engine, session, pre, gm, bot);
    }

    [Fact]
    public void Respond_handles_multi_sentence_and_that_context()
    {
        (Engine engine, UserSession session, PreProcessor pre, Graphmaster gm, Bot _) = Make();

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

        string reply = engine.Respond("hello. how are you?", session);

        reply.Should().Be("Hi. I'm fine.");

        // THAT should now be most recent bot reply
        session.That.Should().Be("I'm fine.");
        session.ThatHistory.At(1).Should().Be("I'm fine.");
        session.ThatHistory.At(2).Should().Be("Hi.");
    }

    [Fact]
    public void Respond_updates_predicates_and_persists_across_sentences()
    {
        (Engine engine, UserSession session, PreProcessor pre, Graphmaster gm, Bot _) = Make();

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

        string reply = engine.Respond("my name is Alice. what is my name", session);

        reply.Should().Be("Nice to meet you, ALICE. You told me your name is ALICE.");
        session.Predicates.GetOrEmpty("name").Should().Be("ALICE");
    }

    [Fact]
    public void Srai_executes_single_cycle_without_affecting_input_history()
    {
        (Engine engine, UserSession session, PreProcessor pre, Graphmaster gm, Bot _) = Make();

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
        string r1 = engine.Respond("What is your name?", session);
        r1.Should().Be("ALICE");

        int inputsBefore = session.InputHistory.Count;

        // Direct SRAI call should NOT push a new input
        string r2 = engine.Srai("TELL ME YOUR NAME", session, depth: 0);
        r2.Should().Be("ALICE");

        session.InputHistory.Count.Should().Be(inputsBefore);
    }

    [Fact]
    public void Respond_pushes_original_input_into_history()
    {
        (Engine engine, UserSession session, PreProcessor pre, Graphmaster gm, Bot _) = Make();

        gm.AddCategory(
            pattern: pre.NormalizePattern("HELLO"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "Hi.");

        string input = "hello";
        string reply = engine.Respond(input, session);

        reply.Should().Be("Hi.");
        session.InputHistory.At(1).Should().Be(input);
    }


    [Theory]
    // Empty / whitespace-only
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("\t\r\n  \n", "")]

    // Collapse runs, trim ends
    [InlineData("Hello   world", "Hello world")]
    [InlineData("  Hello   world  ", "Hello world")]
    [InlineData("Line1\n\nLine2\t\tEnd", "Line1 Line2 End")]

    // Remove spaces before punctuation ( ?, !, ., ,, :, ; )
    [InlineData("Hello ?", "Hello?")]
    [InlineData("Hi , there", "Hi, there")]
    [InlineData("He said , \"wow\"  !", "He said, \"wow\"!")]

    // Remove dangling commas directly before terminal punctuation
    [InlineData("What makes you so sad,?", "What makes you so sad?")]
    [InlineData("That’s interesting , .", "That’s interesting.")]
    [InlineData("Yes, !", "Yes!")]

    // Quotation + punctuation spacing
    [InlineData("I said, \"\" .", "I said, \"\".")]
    [InlineData("He replied , \"okay\" ?", "He replied, \"okay\"?")]

    // Remove trailing commas
    [InlineData("What makes you so sad,", "What makes you so sad")]
    [InlineData("What makes you so sad,,", "What makes you so sad")]

    public void CorrectPunctuation_normalizes(string input, string expected)
    {
        Engine.CorrectPunctuation(input).Should().Be(expected);
    }
}
