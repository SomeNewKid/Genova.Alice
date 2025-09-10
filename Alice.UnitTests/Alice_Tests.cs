// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Genova.Alice.Tests;

/// <summary>
/// End-to-end sanity tests that exercise common ALICE paths:
/// reductions (HI→HELLO), greetings with <random>, SRAI chains,
/// predicate set/get via CALL ME / WHAT IS MY NAME, THAT-context,
/// bot properties (WHAT IS YOUR NAME), and a couple of topical/default paths.
/// 
/// Notes:
/// - Greetings use <random>; assertions accept multiple valid outputs.
/// - We avoid brittle exact strings where ALICE data varies by corpus.
/// - Each test creates a fresh Alice instance to avoid state bleed.
/// </summary>
public class Alice_Tests
{
    // --- Greetings & reductions ---

    [Fact]
    public void Respond_to_Hello()
    {
        var alice = new Alice();
        string response = alice.GetResponse("Hello");

        string[] expected =
        [
            "Hi there!",
            "Hi there. What is your name?"
        ];
        response.Should().BeOneOf(expected);
    }

    [Fact]
    public void Respond_to_Hi_reduction_should_behave_like_Hello()
    {
        // reduction2.safe.aiml includes: HI -> <srai>HELLO</srai>
        var alice = new Alice();
        string response = alice.GetResponse("Hi");

        string[] expected =
        [
            "Hi there!",
            "Hi there. What is your name?"
        ];
        response.Should().BeOneOf(expected);
    }

    [Fact]
    public void Respond_to_two_sentences_splits_and_concatenates()
    {
        var alice = new Alice();
        var response = alice.GetResponse("Hello. What is your name?");
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().ContainAny("Hi", "Hello");      // first sentence
        response.Should().MatchRegex(@"(?i)\bALICE\b");   // second sentence
    }

    [Fact]
    public void Respond_to_How_are_you_after_Hello_respects_that()
    {
        var alice = new Alice();
        _ = alice.GetResponse("Hello");
        string response = alice.GetResponse("How are you?");

        string[] expected =
        [
            "I am doing very well. How are you?",
            "I am functioning within normal parameters.",
            "Everything is going extremely well.",
            "Fair to partly cloudy.",
            "My logic and cognitive functions are normal.",
            "I'm doing fine thanks how are you?",
            "Everything is running smoothly.",
            "I am fine, thank you.",
        ];

        response.Should().NotBeNullOrWhiteSpace();
        response.Should().BeOneOf(expected);
    }

    // --- Name / profile predicates via CALL ME * and MY NAME IS * ---

    [Fact]
    public void Respond_to_My_name_is_Earl_sets_predicate_and_greets()
    {
        var alice = new Alice();
        string response = alice.GetResponse("My name is Earl");

        string[] expected =
        [
            "Hey, Earl.",
            "Hi, Earl.",
            "Hi there Earl.",
            "Hi there, Earl.",
            "What's up, Earl?",
            "How are you, Earl.",
            "Glad to see you, Earl.",
            "Nice to meet you, Earl.",
            "Glad to know you, Earl.",
            "How can I help you, Earl.",
            "How are you doing, Earl.",
            "OK I will call you Earl.",
            "Pleased to meet you, Earl.",
            "It's good to see you, Earl.",
            "It's good to meet you, Earl.",
            "That's a very nice name, Earl.",
            "I am very pleased to meet you, Earl.",
            "I am always glad to make new friends, Earl.",
            "I'm pleased to introduce myself to you, Earl.",
            "It is a pleasure to introduce myself to you, Earl.",
        ];
        response.Should().BeOneOf(expected);
    }

    [Fact]
    public void Call_me_sets_name_then_what_is_my_name_reads_it_back()
    {
        var alice = new Alice();

        // Alternate path to set the name (explicit CALL ME *)
        string setReply = alice.GetResponse("Call me Tom");
        setReply.Should().NotBeNullOrWhiteSpace();

        // Now query it back
        string queryReply = alice.GetResponse("What is my name?");
        queryReply.Should().NotBeNullOrWhiteSpace();
        queryReply.Should().Be("You said your name is Tom?");
    }

    [Fact]
    public void My_name_path_sets_name_then_what_is_my_name_reads_it_back()
    {
        var alice = new Alice();

        string setReply = alice.GetResponse("My name is Bob");
        setReply.Should().NotBeNullOrWhiteSpace();

        string queryReply = alice.GetResponse("What is my name?");
        queryReply.Should().NotBeNullOrWhiteSpace();
        queryReply.Should().Be("You said your name is Bob?");
    }

    // --- Bot property & reductions (WHAT IS YOUR NAME / contractions) ---

    [Fact]
    public void What_is_your_name_returns_bot_name()
    {
        var alice = new Alice();
        string response = alice.GetResponse("What is your name?");
        response.Should().NotBeNullOrWhiteSpace();
        // Many ALICE brains respond “ALICE” or “My name is ALICE.”
        response.Should().MatchRegex(@"(?i)\bALICE\b");
    }

    [Fact]
    public void Whats_your_name_contraction_reduces_and_returns_bot_name()
    {
        var alice = new Alice();
        string response = alice.GetResponse("What's your name?");
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().MatchRegex(@"(?i)\bALICE\b");
    }

    // --- Date/time (simple sanity; exact time need not be asserted) ---

    [Fact]
    public void What_time_is_it_returns_non_empty_with_digits()
    {
        var alice = new Alice();
        string response = alice.GetResponse("What time is it?");
        response.Should().NotBeNullOrWhiteSpace();
        // Expect at least some digits in the time string
        Regex.IsMatch(response, @"\d").Should().BeTrue();
    }

    [Fact]
    public void What_is_it_the_date_returns_non_empty_with_digits()
    {
        var alice = new Alice();
        string response = alice.GetResponse("What is the date?");
        response.Should().NotBeNullOrWhiteSpace();
        // Expect at least some digits in the date string
        Regex.IsMatch(response, @"\d").Should().BeTrue();
    }

    // --- Topical / identity small talk ---

    [Fact]
    public void Are_you_a_computer_returns_confident_identity_answer()
    {
        var alice = new Alice();
        string response = alice.GetResponse("Are you a computer?");
        response.Should().NotBeNullOrWhiteSpace();
        // Typically includes “computer” or an affirmative
        response.Should().MatchRegex(@"(?i)(computer|robot|yes|I am)");
    }

    // --- Default / fallback should be non-empty (sanity) ---

    [Fact]
    public void Gibberish_triggers_default_fallback_non_empty()
    {
        var alice = new Alice();
        _ = alice.GetResponse("Hello"); // establish THAT
        string response = alice.GetResponse("asdjkl qweoiu zxcmn");
        response.Should().NotBeNullOrWhiteSpace();
    }
}
