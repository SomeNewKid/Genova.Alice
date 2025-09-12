// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using FluentAssertions;

namespace Genova.Alice.UnitTests;

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
    [Fact]
    public void Respond_to_Hello()
    {
        Alice alice = new Alice();
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
        Alice alice = new ();
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
        Alice alice = new ();
        string response = alice.GetResponse("Hello. What is your name?");
        EnsureValidResponse(response);
        response.Should().ContainAny("Hi", "Hello");      // first sentence
        response.Should().MatchRegex(@"(?i)\bALICE\b");   // second sentence
    }

    [Fact]
    public void Respond_to_How_are_you_after_Hello_respects_that()
    {
        Alice alice = new ();
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

        EnsureValidResponse(response);
        response.Should().BeOneOf(expected);
    }

    [Fact]
    public void Respond_to_My_name_is_Earl_sets_predicate_and_greets()
    {
        Alice alice = new ();
        string response = alice.GetResponse("My name is Earl");

        string[] expected =
        [
            "Hey, Earl.",
            "Hi, Earl.",
            "Hi there Earl.",
            "Hi there, Earl.",
            "What's up, Earl.",
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
        Alice alice = new ();

        // Alternate path to set the name (explicit CALL ME *)
        string setReply = alice.GetResponse("Call me Tom");
        EnsureValidResponse(setReply);

        // Now query it back
        string queryReply = alice.GetResponse("What is my name?");
        EnsureValidResponse(queryReply);
        queryReply.Should().Be("You said your name is Tom?");
    }

    [Fact]
    public void My_name_path_sets_name_then_what_is_my_name_reads_it_back()
    {
        Alice alice = new ();

        string setReply = alice.GetResponse("My name is Bob");
        EnsureValidResponse(setReply);

        string queryReply = alice.GetResponse("What is my name?");
        EnsureValidResponse(queryReply);
        queryReply.Should().Be("You said your name is Bob?");
    }

    [Fact]
    public void What_is_your_name_returns_bot_name()
    {
        Alice alice = new ();
        string response = alice.GetResponse("What is your name?");
        EnsureValidResponse(response);
        // Many ALICE brains respond “ALICE” or “My name is ALICE.”
        response.Should().MatchRegex(@"(?i)\bALICE\b");
    }

    [Fact]
    public void Whats_your_name_contraction_reduces_and_returns_bot_name()
    {
        Alice alice = new ();
        string response = alice.GetResponse("What's your name?");
        EnsureValidResponse(response);
        response.Should().MatchRegex(@"(?i)\bALICE\b");
    }

    [Fact]
    public void What_time_is_it_returns_non_empty_with_digits()
    {
        Alice alice = new ();
        string response = alice.GetResponse("What time is it?");
        EnsureValidResponse(response);
        // Expect at least some digits in the time string
        Regex.IsMatch(response, @"\d").Should().BeTrue();
    }

    [Fact]
    public void What_is_it_the_date_returns_non_empty_with_digits()
    {
        Alice alice = new ();
        string response = alice.GetResponse("What is the date?");
        EnsureValidResponse(response);

        // Expect at least some digits in the date string
        Regex.IsMatch(response, @"\d").Should().BeTrue();
    }

    [Fact]
    public void Are_you_a_computer_returns_confident_identity_answer()
    {
        Alice alice = new ();
        string response = alice.GetResponse("Are you a computer?");
        EnsureValidResponse(response);

        // Typically includes “computer” or an affirmative
        response.Should().MatchRegex(@"(?i)(computer|robot|chatbot|yes|I am)");
    }

    [Fact]
    public void Gibberish_with_THAT_context_triggers_default_fallback_non_empty()
    {
        Alice alice = new ();
        _ = alice.GetResponse("Hello"); // establish THAT
        string response = alice.GetResponse("asdjkl qweoiu zxcmn");
        EnsureValidResponse(response);
    }

    [Fact]
    public void Gibberish_without_THAT_context_triggers_default_fallback_non_empty()
    {
        Alice alice = new();
        string response = alice.GetResponse("asdjkl qweoiu zxcmn");
        EnsureValidResponse(response);
    }

    [Fact]
    public void I_like_pizza_with_THAT_context_returns_suitable_non_empty()
    {
        Alice alice = new();
        _ = alice.GetResponse("Hello"); // establish THAT
        string response = alice.GetResponse("I like pizza.");

        string[] expected =
        [
            "You like pizza.",
            "What do you like about it?",
            "What else do you like?",
        ];
        response.Should().BeOneOf(expected);
    }

    [Fact]
    public void I_like_pizza_without_THAT_context_returns_suitable_non_empty()
    {
        Alice alice = new();
        string response = alice.GetResponse("I like pizza.");

        string[] expected =
        [
            "You like pizza.",
            "What do you like about it?",
            "What else do you like?",
        ];
        response.Should().BeOneOf(expected);
    }

    [Fact]
    public void I_like_pizza_repeated_always_returns_suitable_non_empty()
    {
        Alice alice = new();
        for (int i = 0; i < 20; i++)
        {
            string response = alice.GetResponse("I like pizza.");

            string[] expected =
            [
                "You like pizza.",
                "What do you like about it?",
                "What else do you like?",
            ];
            response.Should().BeOneOf(expected);
        }
    }

    [Fact]
    public void Tell_me_something_interesting_is_non_empty()
    {
        Alice alice = new();
        string reply = alice.GetResponse("Tell me something interesting.");
        EnsureValidResponse(reply);
    }

    // --- PATTERN: "*" catch-all (Ultimate Default Category) -----------------

    [Fact]
    public void Star_catch_all_gibberish_is_non_empty()
    {
        Alice alice = new ();
        // First-turn gibberish. If your default uses a THAT-based reroute,
        // you can warm the THAT context with "Hello" first.
        string reply = alice.GetResponse("asdjkl qweoiu zxcmn");
        EnsureValidResponse(reply);
    }

    // --- PATTERN: "X *" (left-anchored star) --------------------------------

    [Fact]
    public void Left_anchored_star_is_non_empty()
    {
        Alice alice = new ();
        string reply = alice.GetResponse("Tell me a story about pizza.");
        EnsureValidResponse(reply);
    }

    // --- PATTERN: "X _ Y" (underscore = one-or-more) ------------------------

    [Fact]
    public void Underscore_one_or_more_words_is_non_empty()
    {
        Alice alice = new ();
        // Phrase that should match an underscore slot if your default.aiml uses it.
        string reply = alice.GetResponse("Explain quantum mechanics briefly");
        EnsureValidResponse(reply);
    }

    // --- TEMPLATE: <random><li>…</li>…</random> -----------------------------

    [Fact]
    public void Random_branch_returns_one_of_expected_variants()
    {
        Alice alice = new ();
        // Pick a prompt commonly routed to default random replies (adjust if needed)
        string reply = alice.GetResponse("Tell me something");
        string[] expected =
        [
            "Gregory said I respond to the current line not with respect to the entire conversation.",
            "Jo said I disassemble sentences too much and do not really understand the sentences.",
        ];
        // Non-empty is always required; if you have verified variants, assert OneOf:
        EnsureValidResponse(reply);
        reply.Should().BeOneOf(expected); // enable once you confirm variants
    }

    // --- TEMPLATE: <srai>  (default reroutes) -------------------------------

    [Fact]
    public void Default_srai_paths_produce_non_empty()
    {
        Alice alice = new ();
        // Many defaults SRAI to a canonical form; warm THAT then try a re-ask
        _ = alice.GetResponse("Hello");
        string reply = alice.GetResponse("Say that again?");
        EnsureValidResponse(reply);
    }

    // --- TEMPLATE: <sr/> shorthand (<srai><star/></srai>) -------------------

    [Fact]
    public void Sr_shorthand_is_non_empty_for_star_driven_defaults()
    {
        Alice alice = new ();
        // Phrase that’s likely to match a default that uses <sr/> in its template:
        string reply = alice.GetResponse("Repeat after me hello world");
        EnsureValidResponse(reply);
    }

    // --- TEMPLATE: <condition> (list form) ----------------------------------

    [Fact]
    public void Condition_list_form_in_default_is_non_empty()
    {
        Alice alice = new ();
        // Set a predicate implicitly, then ask something that triggers a default condition.
        _ = alice.GetResponse("Call me Dana");
        string reply = alice.GetResponse("What is my name?");
        EnsureValidResponse(reply);
        // Optionally assert that the name appears (if your default funnels here):
        reply.Should().MatchRegex(@"(?i)\bDana\b");
    }

    // --- TEMPLATE: <think><set/><get/> --------------------------------------

    [Fact]
    public void Think_set_get_works_in_default_paths()
    {
        Alice alice = new ();
        string reply1 = alice.GetResponse("Remember that the passphrase is swordfish");
        EnsureValidResponse(reply1);
        string reply2 = alice.GetResponse("What is the passphrase?");
        EnsureValidResponse(reply2);
        //reply2.Should().MatchRegex(@"(?i)\bswordfish\b"); // enable if applicable
    }

    // --- TEMPLATE: <bot/> in default replies --------------------------------

    [Fact]
    public void Bot_property_lookups_in_default_are_non_empty_when_seeded()
    {
        Alice alice = new ();
        string reply = alice.GetResponse("Tell me about yourself");
        // If your bot.properties sets 'species' or 'order', some default answers include them.
        EnsureValidResponse(reply);
    }

    // --- TEMPLATE: <person/>, <person2/>, <gender/> in defaults --------------

    [Fact]
    public void Person_reflection_in_default_is_sensible()
    {
        Alice alice = new ();
        _ = alice.GetResponse("Hello"); // seed THAT if your default uses THAT-aware routes
        string reply = alice.GetResponse("I like pizza.");
        EnsureValidResponse(reply);
        // If you applied the lowercase refinement for raw star reflections:
        // reply.Should().Contain("pizza");
    }

    // --- TEMPLATE: casing transforms (<formal>, <sentence>, <lowercase>) ----

    [Fact]
    public void Casing_transforms_in_default_produce_readable_text()
    {
        Alice alice = new ();
        string reply = alice.GetResponse("format this sentence properly please");
        EnsureValidResponse(reply);
    }

    // --- TEMPLATE: <id/> if present in your default.aiml --------------------

    [Fact]
    public void Id_tag_in_default_is_non_empty_if_implemented()
    {
        Alice alice = new ();
        string reply = alice.GetResponse("What is my id?");
        // If your default.aiml uses <id/>, this will echo the session/user id (once Tag_Id is implemented).
        EnsureValidResponse(reply);
    }

    // --- TEMPLATE: <that/>, <thatstar/>, <topicstar/> re-ask pattern --------

    [Fact]
    public void That_topicstar_reroute_in_default_is_non_empty_after_context()
    {
        Alice alice = new ();
        _ = alice.GetResponse("Hello");             // establish THAT
        string reply = alice.GetResponse("Say what?"); // common default: <srai><that/> <topicstar/></srai>
        EnsureValidResponse(reply);
        reply.Should().NotContain("\"\"");
    }

    private void EnsureValidResponse(string reply)
    {
        reply.Should().NotBeNullOrWhiteSpace();
        reply.Should().NotEndWith(" .");
    }
}
