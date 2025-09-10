// =============================================================
// Genova.Alice.Tests — Part 2 (Step 2: TDD)
// Unit tests for Category, Path, Stars, Match (xUnit + FluentAssertions)
// Note: InternalsVisibleTo("Genova.Alice.Tests") must be set in the main assembly.
// =============================================================

using System;
using System.IO;
using FluentAssertions;
using Genova.Alice;
using Path = Genova.Alice.Path;

namespace Genova.Alice.Tests;

public class CategoryTests
{
    [Fact]
    public void Ctor_sets_properties()
    {
        var cat = new Category(pattern: "HELLO", that: "*", topic: "*", template: "Hi.");

        cat.Pattern.Should().Be("HELLO");
        cat.That.Should().Be("*");
        cat.Topic.Should().Be("*");
        cat.Template.Should().Be("Hi.");
    }

    [Fact]
    public void Signature_returns_pattern_THAT_that_TOPIC_topic()
    {
        var cat = new Category("HELLO", "HOW ARE YOU", "JOKES", "<template/>");

        cat.Signature().Should().Be("HELLO <THAT> HOW ARE YOU <TOPIC> JOKES");
    }
}

public class PathTests
{
    [Fact]
    public void FromSegments_builds_tokens_and_indices_correctly()
    {
        var input = new[] { "HELLO" };
        var that = new[] { "HOW", "ARE", "YOU" };
        var topic = new[] { "JOKES" };

        var p = Path.FromSegments(input, that, topic);

        p.Tokens.Should().Equal("HELLO", Path.ThatSeparator, "HOW", "ARE", "YOU", Path.TopicSeparator, "JOKES");

        p.ThatIndex.Should().Be(1);   // index of <THAT>
        p.TopicIndex.Should().Be(5);  // index of <TOPIC>

        p.InputCount.Should().Be(1);
        p.ThatCount.Should().Be(3);
        p.TopicCount.Should().Be(1);

        p.GetInputTokens().Should().Equal("HELLO");
        p.GetThatTokens().Should().Equal("HOW", "ARE", "YOU");
        p.GetTopicTokens().Should().Equal("JOKES");

        p.ToString().Should().Be("HELLO <THAT> HOW ARE YOU <TOPIC> JOKES");
    }

    [Fact]
    public void FromSegments_with_wildcard_segments_keeps_markers_and_counts()
    {
        var p = Path.FromSegments(
            inputTokens: new[] { "WHAT", "IS", "YOUR", "NAME" },
            thatTokens: new[] { "*" },
            topicTokens: new[] { "*" });

        p.Tokens.Should().Equal("WHAT", "IS", "YOUR", "NAME", Path.ThatSeparator, "*", Path.TopicSeparator, "*");
        p.InputCount.Should().Be(4);
        p.ThatCount.Should().Be(1);
        p.TopicCount.Should().Be(1);
    }
}

public class StarsTests
{
    [Fact]
    public void AddStar_and_retrieve_preserves_order_and_allows_1_based_access()
    {
        var s = new Stars();
        s.AddStar("A");
        s.AddStar("B");

        s.StarCount.Should().Be(2);
        s.StarAt(1).Should().Be("A");
        s.StarAt(2).Should().Be("B");
        s.StarAt(3).Should().BeEmpty(); // out of range returns empty
    }

    [Fact]
    public void AddThatStar_and_AddTopicStar_behave_like_Star()
    {
        var s = new Stars();
        s.AddThatStar("X");
        s.AddThatStar("Y");
        s.AddTopicStar("M");

        s.ThatStarCount.Should().Be(2);
        s.TopicStarCount.Should().Be(1);

        s.ThatStarAt(1).Should().Be("X");
        s.ThatStarAt(2).Should().Be("Y");
        s.ThatStarAt(3).Should().BeEmpty();

        s.TopicStarAt(1).Should().Be("M");
        s.TopicStarAt(2).Should().BeEmpty();
    }

    [Fact]
    public void Zero_based_accessors_throw_on_out_of_range()
    {
        var s = new Stars();
        s.AddStar("A");
        s.AddThatStar("B");
        s.AddTopicStar("C");

        s.Invoking(_ => _.StarAtZero(1)).Should().Throw<ArgumentOutOfRangeException>();
        s.Invoking(_ => _.ThatStarAtZero(1)).Should().Throw<ArgumentOutOfRangeException>();
        s.Invoking(_ => _.TopicStarAtZero(1)).Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class MatchTests
{
    [Fact]
    public void Ctor_sets_properties_and_exposes_normalized_segments()
    {
        var cat = new Category("HELLO", "*", "*", "<template/>");
        var path = Path.FromSegments(
            new[] { "HELLO" },
            new[] { "*" },
            new[] { "*" });

        var stars = new Stars();
        stars.AddStar("WORLD");
        stars.AddThatStar("OK");
        stars.AddTopicStar("JOKES");

        var m = new Match(cat, path, stars,
                          normalizedInput: "HELLO",
                          normalizedThat: "*",
                          normalizedTopic: "*");

        m.Category.Should().BeSameAs(cat);
        m.Path.Should().BeSameAs(path);
        m.Stars.Should().BeSameAs(stars);

        m.NormalizedInput.Should().Be("HELLO");
        m.NormalizedThat.Should().Be("*");
        m.NormalizedTopic.Should().Be("*");
    }

    [Fact]
    public void Convenience_accessors_return_star_values()
    {
        var cat = new Category("I AM *", "*", "*", "<template/>");
        var path = Path.FromSegments(new[] { "I", "AM", "*" }, new[] { "*" }, new[] { "*" });

        var stars = new Stars();
        stars.AddStar("SAD");
        stars.AddThatStar("OK");
        stars.AddTopicStar("JOKES");

        var m = new Match(cat, path, stars, "I AM *", "*", "*");

        m.Star(1).Should().Be("SAD");
        m.ThatStar(1).Should().Be("OK");
        m.TopicStar(1).Should().Be("JOKES");

        // out-of-range should return empty string (not throw)
        m.Star(2).Should().BeEmpty();
        m.ThatStar(2).Should().BeEmpty();
        m.TopicStar(2).Should().BeEmpty();
    }
}
