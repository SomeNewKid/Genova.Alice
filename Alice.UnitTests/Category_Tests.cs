// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class Category_Tests
{
    [Fact]
    public void Ctor_sets_properties()
    {
        Category cat = new (pattern: "HELLO", that: "*", topic: "*", template: "Hi.");

        cat.Pattern.Should().Be("HELLO");
        cat.That.Should().Be("*");
        cat.Topic.Should().Be("*");
        cat.Template.Should().Be("Hi.");
    }

    [Fact]
    public void Signature_returns_pattern_THAT_that_TOPIC_topic()
    {
        Category cat = new ("HELLO", "HOW ARE YOU", "JOKES", "<template/>");

        cat.Signature().Should().Be("HELLO <THAT> HOW ARE YOU <TOPIC> JOKES");
    }
}

public class PathTests
{
    [Fact]
    public void FromSegments_builds_tokens_and_indices_correctly()
    {
        string[] input = ["HELLO"];
        string[] that = ["HOW", "ARE", "YOU"];
        string[] topic = ["JOKES"];

        Path p = Path.FromSegments(input, that, topic);

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
        Path p = Path.FromSegments(
            inputTokens: ["WHAT", "IS", "YOUR", "NAME"],
            thatTokens: ["*"],
            topicTokens: ["*"]);

        p.Tokens.Should().Equal("WHAT", "IS", "YOUR", "NAME", Path.ThatSeparator, "*", Path.TopicSeparator, "*");
        p.InputCount.Should().Be(4);
        p.ThatCount.Should().Be(1);
        p.TopicCount.Should().Be(1);
    }
}
