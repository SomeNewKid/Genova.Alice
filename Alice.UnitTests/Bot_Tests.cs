// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class Bot_Tests
{
    [Fact]
    public void Ctor_initializes_properties_and_substitutions_and_history_depth()
    {
        BotProperties props = new ();
        SubstitutionTables subs = SubstitutionTables.CreateClassicDefaults();

        Bot bot = new (props, subs, thatHistoryDepth: 5);

        bot.Properties.Should().BeSameAs(props);
        bot.Substitutions.Should().BeSameAs(subs);
        bot.ThatHistoryDepth.Should().Be(5);
    }

    [Fact]
    public void Ctor_uses_defaults_when_nulls_passed()
    {
        Bot bot = new (null, null, thatHistoryDepth: 7);

        bot.Properties.Should().NotBeNull();
        bot.Substitutions.Should().NotBeNull();
        bot.ThatHistoryDepth.Should().Be(7);
    }
}
