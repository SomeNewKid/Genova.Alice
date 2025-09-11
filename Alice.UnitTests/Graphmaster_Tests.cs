// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class Graphmaster_Tests
{
    private static (Graphmaster gm, PreProcessor pre) Make()
    {
        PreProcessor pre = new (SubstitutionTables.CreateClassicDefaults());
        Graphmaster gm = new ();
        return (gm, pre);
    }

    [Fact]
    public void Tokenize_splits_by_space_and_drops_empties()
    {
        IReadOnlyList<string> tokens = Graphmaster.Tokenize(" A  B   C ");
        tokens.Should().Equal("A", "B", "C");
    }

    [Fact]
    public void AddCategory_and_exact_literal_match_returns_category_and_no_stars()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        gm.AddCategory(
            pattern: pre.NormalizePattern("HELLO"),
            that: pre.NormalizePattern("*"),
            topic: pre.NormalizePattern("*"),
            template: "Hi."
        );

        Match? m = gm.Match(
            normalizedInput: pre.NormalizeInput("hello"),
            normalizedThat: pre.NormalizeThat("*"),
            normalizedTopic: pre.NormalizeTopic("*"));

        m.Should().NotBeNull();
        m!.Category.Template.Should().Be("Hi.");
        m.Stars.StarCount.Should().Be(0);
        m.Stars.ThatStarCount.Should().Be(0);
        m.Stars.TopicStarCount.Should().Be(0);
    }

    [Fact]
    public void Match_prefers_underscore_over_literal_over_star()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        // Three competing categories for pattern: "<something> HELLO"
        gm.AddCategory(pre.NormalizePattern("_ HELLO"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "UNDER");
        gm.AddCategory(pre.NormalizePattern("GOOD HELLO"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "LITERAL");
        gm.AddCategory(pre.NormalizePattern("* HELLO"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "STAR");

        Match? m = gm.Match(
            normalizedInput: pre.NormalizeInput("good hello"),
            normalizedThat: pre.NormalizeThat("*"),
            normalizedTopic: pre.NormalizeTopic("*"));

        m.Should().NotBeNull();

        // Program D precedence is '_' > literal > '*'
        m!.Category.Template.Should().Be("UNDER");
    }

    [Fact]
    public void Match_respects_segment_boundaries_and_requires_that_to_match()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        // Category requires THAT = OK
        gm.AddCategory(pre.NormalizePattern("*"), pre.NormalizePattern("OK"), pre.NormalizePattern("*"), "GOT_THAT");

        // With THAT = OK -> match
        Match? m1 = gm.Match(
            normalizedInput: pre.NormalizeInput("anything you like"),
            normalizedThat: pre.NormalizeThat("OK"),
            normalizedTopic: pre.NormalizeTopic("*"));

        m1.Should().NotBeNull();
        m1!.Category.Template.Should().Be("GOT_THAT");
        m1.Stars.StarAt(1).Should().Be("ANYTHING YOU LIKE"); // star from pattern segment

        // With THAT = NOT OK -> no match
        Match? m2 = gm.Match(
            normalizedInput: pre.NormalizeInput("anything you like"),
            normalizedThat: pre.NormalizeThat("NOT OK"),
            normalizedTopic: pre.NormalizeTopic("*"));

        m2.Should().BeNull();
    }

    [Fact]
    public void Star_is_greedy_but_backtracks_to_match_rest()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        gm.AddCategory(pre.NormalizePattern("A * B"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "T");

        Match? m = gm.Match(
            normalizedInput: pre.NormalizeInput("a x y b"),
            normalizedThat: pre.NormalizeThat("*"),
            normalizedTopic: pre.NormalizeTopic("*"));

        m.Should().NotBeNull();
        m!.Star(1).Should().Be("X Y"); // greedy capture shrinks to satisfy trailing literal 'B'
    }

    [Fact]
    public void Multi_wildcard_captures_are_recorded_in_order()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        gm.AddCategory(pre.NormalizePattern("A * B * C"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "T");

        Match? m = gm.Match(
            normalizedInput: pre.NormalizeInput("a x b y c"),
            normalizedThat: pre.NormalizeThat("*"),
            normalizedTopic: pre.NormalizeTopic("*"));

        m.Should().NotBeNull();
        m!.Stars.StarCount.Should().Be(2);
        m.Star(1).Should().Be("X");
        m.Star(2).Should().Be("Y");
    }

    [Fact]
    public void That_and_topic_captures_are_separate_from_pattern_captures()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        // Pattern, THAT, and TOPIC each with a '*' to capture
        gm.AddCategory(pre.NormalizePattern("*"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "CAP_ALL");

        Match? m = gm.Match(
            normalizedInput: pre.NormalizeInput("hello world"),
            normalizedThat: pre.NormalizeThat("yeah ok"),
            normalizedTopic: pre.NormalizeTopic("jokes"));

        m.Should().NotBeNull();
        m!.Star(1).Should().Be("HELLO WORLD");
        m.ThatStar(1).Should().Be("YEAH OK");
        m.TopicStar(1).Should().Be("JOKES");
    }

    [Fact]
    public void Topic_specific_category_only_matches_when_topic_matches()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        gm.AddCategory(pre.NormalizePattern("TELL ME A JOKE"),
                       pre.NormalizePattern("*"),
                       pre.NormalizePattern("JOKES"),
                       "JOKE");

        // With topic = JOKES -> match
        Match? m1 = gm.Match(
            normalizedInput: pre.NormalizeInput("tell me a joke"),
            normalizedThat: pre.NormalizeThat("*"),
            normalizedTopic: pre.NormalizeTopic("jokes"));

        m1.Should().NotBeNull();
        m1!.Category.Template.Should().Be("JOKE");

        // With topic = * -> no match
        Match? m2 = gm.Match(
            normalizedInput: pre.NormalizeInput("tell me a joke"),
            normalizedThat: pre.NormalizeThat("*"),
            normalizedTopic: pre.NormalizeTopic("*"));

        m2.Should().BeNull();
    }

    [Fact]
    public void Underscore_consumes_one_or_more_tokens_star_can_be_empty()
    {
        (Graphmaster gm, PreProcessor pre) = Make();

        gm.AddCategory(pre.NormalizePattern("_"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "UNDER");
        gm.AddCategory(pre.NormalizePattern("*"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "STAR");

        // Input with one token: both match; '_' should win
        Match? m1 = gm.Match(pre.NormalizeInput("HELLO"), pre.NormalizeThat("*"), pre.NormalizeTopic("*"));
        m1.Should().NotBeNull();
        m1!.Category.Template.Should().Be("UNDER");

        // Pattern: "A *" should match "A" with empty star
        gm = new Graphmaster();
        gm.AddCategory(pre.NormalizePattern("A *"), pre.NormalizePattern("*"), pre.NormalizePattern("*"), "T");
        Match? m2 = gm.Match(pre.NormalizeInput("A"), pre.NormalizeThat("*"), pre.NormalizeTopic("*"));
        m2.Should().NotBeNull();
        m2!.Star(1).Should().Be(string.Empty);
    }
}
