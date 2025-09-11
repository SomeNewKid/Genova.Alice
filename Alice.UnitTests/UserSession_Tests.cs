// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class UserSession_Tests
{
    private static Bot MakeBot(int thatDepth = 3) =>
        new (new BotProperties(), SubstitutionTables.CreateClassicDefaults(), thatDepth);

    [Fact]
    public void Ctor_sets_user_id_initial_topic_star_and_histories_with_bot_capacity()
    {
        Bot bot = MakeBot(thatDepth: 4);
        UserSession s = new ("u1", bot, inputHistoryCapacity: 5);

        s.UserId.Should().Be("u1");
        s.Topic.Should().Be("*");
        s.That.Should().Be("*");
        s.ThatHistory.Should().NotBeNull();
        s.InputHistory.Should().NotBeNull();
    }

    [Fact]
    public void Topic_can_be_switched_and_persisted()
    {
        UserSession s = new ("u1", MakeBot());

        s.Topic.Should().Be("*");
        s.Topic = "JOKES";
        s.Topic.Should().Be("JOKES");

        s.Topic = "*";
        s.Topic.Should().Be("*");
    }

    [Fact]
    public void PushThat_updates_that_and_respects_history_capacity()
    {
        UserSession s = new ("u1", MakeBot(thatDepth: 2));

        s.That.Should().Be("*");
        s.PushThat("A");
        s.That.Should().Be("A");
        s.ThatHistory.At(1).Should().Be("A");

        s.PushThat("B");
        s.That.Should().Be("B");
        s.ThatHistory.At(1).Should().Be("B");
        s.ThatHistory.At(2).Should().Be("A");

        s.PushThat("C"); // drops A
        s.That.Should().Be("C");
        s.ThatHistory.At(1).Should().Be("C");
        s.ThatHistory.At(2).Should().Be("B");
        s.ThatHistory.At(3).Should().BeEmpty();
    }

    [Fact]
    public void GetThatOrStar_returns_star_if_no_history_else_most_recent()
    {
        UserSession s = new ("u1", MakeBot());

        s.GetThatOrStar().Should().Be("*");
        s.PushThat("Hello.");
        s.GetThatOrStar().Should().Be("Hello.");
    }

    [Fact]
    public void PushInput_tracks_user_inputs_in_most_recent_first_order()
    {
        UserSession s = new ("u1", MakeBot(), inputHistoryCapacity: 2);
        s.PushInput("hi");
        s.PushInput("how are you?");
        s.InputHistory.At(1).Should().Be("how are you?");
        s.InputHistory.At(2).Should().Be("hi");

        s.PushInput("tell me a joke"); // drops "hi"
        s.InputHistory.At(1).Should().Be("tell me a joke");
        s.InputHistory.At(2).Should().Be("how are you?");
        s.InputHistory.At(3).Should().BeEmpty();
    }

    [Fact]
    public void Predicates_are_available_and_case_insensitive()
    {
        UserSession s = new ("u1", MakeBot());
        s.Predicates.Set("NaMe", "Bob");
        s.Predicates.GetOrEmpty("name").Should().Be("Bob");
    }
}
