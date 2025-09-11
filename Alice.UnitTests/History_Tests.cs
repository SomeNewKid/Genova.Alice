// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class History_Tests
{
    [Fact]
    public void Push_preserves_most_recent_first_and_peek_returns_latest()
    {
        History h = new (capacity: 3);

        h.PeekOrEmpty().Should().BeEmpty();
        h.Count.Should().Be(0);

        h.Push("A");
        h.PeekOrEmpty().Should().Be("A");

        h.Push("B");
        h.PeekOrEmpty().Should().Be("B");

        h.Push("C");
        h.Count.Should().Be(3);

        h.At(1).Should().Be("C"); // most recent
        h.At(2).Should().Be("B");
        h.At(3).Should().Be("A");
        h.At(4).Should().BeEmpty(); // out of range
    }

    [Fact]
    public void Push_drops_oldest_when_capacity_exceeded()
    {
        History h = new (capacity: 2);
        h.Push("A");
        h.Push("B");
        h.Push("C"); // drops A

        h.Count.Should().Be(2);
        h.At(1).Should().Be("C");
        h.At(2).Should().Be("B");
        h.At(3).Should().BeEmpty();
    }

    [Fact]
    public void Clear_resets_history()
    {
        History h = new (2);
        h.Push("X");
        h.Push("Y");

        h.Clear();

        h.Count.Should().Be(0);
        h.PeekOrEmpty().Should().BeEmpty();
        h.At(1).Should().BeEmpty();
    }
}
