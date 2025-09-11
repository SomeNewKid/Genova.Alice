// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;


public class Stars_Tests
{
    [Fact]
    public void AddStar_and_retrieve_preserves_order_and_allows_1_based_access()
    {
        Stars s = new ();
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
        Stars s = new ();
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
        Stars s = new ();
        s.AddStar("A");
        s.AddThatStar("B");
        s.AddTopicStar("C");

        s.Invoking(_ => _.StarAtZero(1)).Should().Throw<ArgumentOutOfRangeException>();
        s.Invoking(_ => _.ThatStarAtZero(1)).Should().Throw<ArgumentOutOfRangeException>();
        s.Invoking(_ => _.TopicStarAtZero(1)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
