// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class BotProperties_Tests
{
    [Fact]
    public void Set_and_GetOrEmpty_are_case_insensitive_and_persist_values()
    {
        BotProperties props = new ();

        props.Set("NaMe", "ALICE");
        props.Count.Should().Be(1);

        props.GetOrEmpty("name").Should().Be("ALICE");
        props.TryGet("NAME", out string? v).Should().BeTrue();
        v.Should().Be("ALICE");
    }

    [Fact]
    public void Clear_empties_all_entries_and_resets_count()
    {
        BotProperties props = new ();
        props.Set("a", "1");
        props.Set("b", "2");

        props.Count.Should().Be(2);
        props.Clear();
        props.Count.Should().Be(0);
        props.GetOrEmpty("a").Should().BeEmpty();
    }
}
