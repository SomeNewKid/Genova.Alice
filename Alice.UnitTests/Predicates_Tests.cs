// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class Predicates_Tests
{
    [Fact]
    public void Set_and_GetOrEmpty_are_case_insensitive_and_persist_values()
    {
        Predicates p = new ();

        p.Set("NaMe", "Alice");
        p.Count.Should().Be(1);

        p.GetOrEmpty("name").Should().Be("Alice");
        p.TryGet("NAME", out string? v).Should().BeTrue();
        v.Should().Be("Alice");
    }

    [Fact]
    public void GetOrEmpty_returns_empty_when_missing()
    {
        Predicates p = new ();
        p.GetOrEmpty("missing").Should().BeEmpty();
        p.TryGet("missing", out string? v).Should().BeFalse();
        v.Should().BeNull();
    }

    [Fact]
    public void Clear_empties_all_entries_and_resets_count()
    {
        Predicates p = new ();
        p.Set("x", "1");
        p.Set("y", "2");

        p.Count.Should().Be(2);
        p.Clear();
        p.Count.Should().Be(0);
        p.GetOrEmpty("x").Should().BeEmpty();
    }
}
