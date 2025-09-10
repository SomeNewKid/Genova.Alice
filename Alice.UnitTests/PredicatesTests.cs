// =============================================================
// Genova.Alice.Tests — Part 5 (Step 2: TDD)
// Unit tests for Bot, Predicates, BotProperties, UserSession, History
// Frameworks: xUnit + FluentAssertions
// =============================================================

using System;
using FluentAssertions;
using Xunit;
using Genova.Alice;

namespace Genova.Alice.Tests;

public class PredicatesTests
{
    [Fact]
    public void Set_and_GetOrEmpty_are_case_insensitive_and_persist_values()
    {
        var p = new Predicates();

        p.Set("NaMe", "Alice");
        p.Count.Should().Be(1);

        p.GetOrEmpty("name").Should().Be("Alice");
        p.TryGet("NAME", out var v).Should().BeTrue();
        v.Should().Be("Alice");
    }

    [Fact]
    public void GetOrEmpty_returns_empty_when_missing()
    {
        var p = new Predicates();
        p.GetOrEmpty("missing").Should().BeEmpty();
        p.TryGet("missing", out var v).Should().BeFalse();
        v.Should().BeNull();
    }

    [Fact]
    public void Clear_empties_all_entries_and_resets_count()
    {
        var p = new Predicates();
        p.Set("x", "1");
        p.Set("y", "2");

        p.Count.Should().Be(2);
        p.Clear();
        p.Count.Should().Be(0);
        p.GetOrEmpty("x").Should().BeEmpty();
    }
}

public class BotPropertiesTests
{
    [Fact]
    public void Set_and_GetOrEmpty_are_case_insensitive_and_persist_values()
    {
        var props = new BotProperties();

        props.Set("NaMe", "ALICE");
        props.Count.Should().Be(1);

        props.GetOrEmpty("name").Should().Be("ALICE");
        props.TryGet("NAME", out var v).Should().BeTrue();
        v.Should().Be("ALICE");
    }

    [Fact]
    public void Clear_empties_all_entries_and_resets_count()
    {
        var props = new BotProperties();
        props.Set("a", "1");
        props.Set("b", "2");

        props.Count.Should().Be(2);
        props.Clear();
        props.Count.Should().Be(0);
        props.GetOrEmpty("a").Should().BeEmpty();
    }
}

public class HistoryTests
{
    [Fact]
    public void Push_preserves_most_recent_first_and_peek_returns_latest()
    {
        var h = new History(capacity: 3);

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
        var h = new History(capacity: 2);
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
        var h = new History(2);
        h.Push("X");
        h.Push("Y");

        h.Clear();

        h.Count.Should().Be(0);
        h.PeekOrEmpty().Should().BeEmpty();
        h.At(1).Should().BeEmpty();
    }
}

public class BotTests
{
    [Fact]
    public void Ctor_initializes_properties_and_substitutions_and_history_depth()
    {
        var props = new BotProperties();
        var subs = SubstitutionTables.CreateClassicDefaults();

        var bot = new Bot(props, subs, thatHistoryDepth: 5);

        bot.Properties.Should().BeSameAs(props);
        bot.Substitutions.Should().BeSameAs(subs);
        bot.ThatHistoryDepth.Should().Be(5);
    }

    [Fact]
    public void Ctor_uses_defaults_when_nulls_passed()
    {
        var bot = new Bot(null, null, thatHistoryDepth: 7);

        bot.Properties.Should().NotBeNull();
        bot.Substitutions.Should().NotBeNull();
        bot.ThatHistoryDepth.Should().Be(7);
    }
}

public class UserSessionTests
{
    private static Bot MakeBot(int thatDepth = 3) =>
        new Bot(new BotProperties(), SubstitutionTables.CreateClassicDefaults(), thatDepth);

    [Fact]
    public void Ctor_sets_user_id_initial_topic_star_and_histories_with_bot_capacity()
    {
        var bot = MakeBot(thatDepth: 4);
        var s = new UserSession("u1", bot, inputHistoryCapacity: 5);

        s.UserId.Should().Be("u1");
        s.Topic.Should().Be("*");
        s.That.Should().Be("*");
        s.ThatHistory.Should().NotBeNull();
        s.InputHistory.Should().NotBeNull();
    }

    [Fact]
    public void Topic_can_be_switched_and_persisted()
    {
        var s = new UserSession("u1", MakeBot());

        s.Topic.Should().Be("*");
        s.Topic = "JOKES";
        s.Topic.Should().Be("JOKES");

        s.Topic = "*";
        s.Topic.Should().Be("*");
    }

    [Fact]
    public void PushThat_updates_that_and_respects_history_capacity()
    {
        var s = new UserSession("u1", MakeBot(thatDepth: 2));

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
        var s = new UserSession("u1", MakeBot());

        s.GetThatOrStar().Should().Be("*");
        s.PushThat("Hello.");
        s.GetThatOrStar().Should().Be("Hello.");
    }

    [Fact]
    public void PushInput_tracks_user_inputs_in_most_recent_first_order()
    {
        var s = new UserSession("u1", MakeBot(), inputHistoryCapacity: 2);
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
        var s = new UserSession("u1", MakeBot());
        s.Predicates.Set("NaMe", "Bob");
        s.Predicates.GetOrEmpty("name").Should().Be("Bob");
    }
}
