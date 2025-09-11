// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class LearnStore_Tests
{
    [Fact]
    public void Add_records_are_accessible_and_counted()
    {
        LearnStore store = new ();

        store.Count.Should().Be(0);

        store.Add("HELLO", "*", "*", "<template>Hi.</template>", "mem://1");
        store.Add("WHAT IS YOUR NAME", "*", "*", "<template>ALICE</template>", "mem://2");

        store.Count.Should().Be(2);
        store.All.Should().HaveCount(2);

        LearnedCategory first = store.All[0];
        first.pattern.Should().Be("HELLO");
        first.that.Should().Be("*");
        first.topic.Should().Be("*");
        first.templateXml.Should().Be("<template>Hi.</template>");
        first.sourceName.Should().Be("mem://1");
    }

    [Fact]
    public void Clear_removes_all_records_and_resets_count()
    {
        LearnStore store = new ();
        store.Add("A", "*", "*", "<template>X</template>");
        store.Add("B", "*", "*", "<template>Y</template>");

        store.Count.Should().Be(2);
        store.Clear();
        store.Count.Should().Be(0);
        store.All.Should().BeEmpty();
    }
}
