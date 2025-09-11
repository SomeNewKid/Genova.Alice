// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class Match_Tests
{
    [Fact]
    public void Ctor_sets_properties_and_exposes_normalized_segments()
    {
        Category cat = new ("HELLO", "*", "*", "<template/>");
        Path path = Path.FromSegments(["HELLO"], ["*"], ["*"]);

        Stars stars = new ();
        stars.AddStar("WORLD");
        stars.AddThatStar("OK");
        stars.AddTopicStar("JOKES");

        Match m = new (cat, path, stars,
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
        Category cat = new ("I AM *", "*", "*", "<template/>");
        Path path = Path.FromSegments(["I", "AM", "*"], ["*"], ["*"]);

        Stars stars = new ();
        stars.AddStar("SAD");
        stars.AddThatStar("OK");
        stars.AddTopicStar("JOKES");

        Match m = new (cat, path, stars, "I AM *", "*", "*");

        m.Star(1).Should().Be("SAD");
        m.ThatStar(1).Should().Be("OK");
        m.TopicStar(1).Should().Be("JOKES");

        // out-of-range should return empty string (not throw)
        m.Star(2).Should().BeEmpty();
        m.ThatStar(2).Should().BeEmpty();
        m.TopicStar(2).Should().BeEmpty();
    }
}
