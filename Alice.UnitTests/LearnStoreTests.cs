// =============================================================
// Genova.Alice.Tests — Part 9 (Step 2: TDD)
// Unit tests for LearnStore + TemplateProcessor <learn/> and <date/>
// Frameworks: xUnit + FluentAssertions
// NOTE: These tests assume you've added the skeletal hooks to TemplateProcessor:
//   - new ctor params: LearnEmitter? learnEmitter, Func<DateTime>? nowProvider
//   - new tag handlers: Tag_Learn, Tag_Date
// =============================================================

using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Genova.Alice;

namespace Genova.Alice.Tests;

public class LearnStoreTests
{
    [Fact]
    public void Add_records_are_accessible_and_counted()
    {
        var store = new LearnStore();

        store.Count.Should().Be(0);

        store.Add("HELLO", "*", "*", "<template>Hi.</template>", "mem://1");
        store.Add("WHAT IS YOUR NAME", "*", "*", "<template>ALICE</template>", "mem://2");

        store.Count.Should().Be(2);
        store.All.Should().HaveCount(2);

        var first = store.All[0];
        first.Pattern.Should().Be("HELLO");
        first.That.Should().Be("*");
        first.Topic.Should().Be("*");
        first.TemplateXml.Should().Be("<template>Hi.</template>");
        first.SourceName.Should().Be("mem://1");
    }

    [Fact]
    public void Clear_removes_all_records_and_resets_count()
    {
        var store = new LearnStore();
        store.Add("A", "*", "*", "<template>X</template>");
        store.Add("B", "*", "*", "<template>Y</template>");

        store.Count.Should().Be(2);
        store.Clear();
        store.Count.Should().Be(0);
        store.All.Should().BeEmpty();
    }
}

public class TemplateProcessorLearnTests
{
    private sealed class LearnCapture
    {
        public int Calls => Records.Count;

        public readonly System.Collections.Generic.List<LearnStore.LearnedCategory> Records = new();

        public void Emit(string p, string t, string topic, string tmpl, string? src)
            => Records.Add(new LearnStore.LearnedCategory(p, t, topic, tmpl, src));
    }

    private static (TemplateProcessor tp, UserSession session, Match match, LearnCapture cap, PreProcessor pre, Bot bot)
        Make(LearnCapture? capture = null, DateTime? fixedNow = null)
    {
        var props = new BotProperties();
        var subs = SubstitutionTables.CreateClassicDefaults();
        var bot = new Bot(props, subs, thatHistoryDepth: 4);
        var pre = new PreProcessor(subs);
        var capInst = capture ?? new LearnCapture();

        // Deterministic RNG for <random> (not used here, but keeps consistency)
        var rng = new Random(42);

        // ctor: (bot, pre, sraiInvoker, rng, maxDepth, learnEmitter, nowProvider)
        TemplateProcessor.LearnEmitter learn = capInst.Emit;
        Func<DateTime> clock = () => fixedNow ?? new DateTime(2025, 09, 07, 14, 30, 00);

        var tp = new TemplateProcessor(
            bot,
            pre,
            sraiInvoker: (text, s, d) => string.Empty,
            rng: rng,
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: learn,
            nowProvider: clock);

        var session = new UserSession("u1", bot);

        // Minimal match; stars not required for <learn/>
        var path = Path.FromSegments(new[] { "*" }, new[] { "*" }, new[] { "*" });
        var stars = new Stars();
        var cat = new Category("*", "*", "*", "<template/>");
        var match = new Match(cat, path, stars, "*", "*", "*");

        return (tp, session, match, capInst, pre, bot);
    }

    [Fact]
    public void Learn_emits_single_category_and_returns_no_output_while_surrounding_text_remains()
    {
        var (tp, session, match, cap, pre, _) = Make();

        // One root-level category inside <learn>, followed by literal "OK"
        var xml = """
                  <template>
                    <learn>
                      <category>
                        <pattern>What is your name?</pattern>
                        <template>ALICE</template>
                      </category>
                    </learn>
                    OK
                  </template>
                  """;

        var output = tp.Process(xml, session, match);

        // Emitted exactly once, normalized
        cap.Calls.Should().Be(1);
        var rec = cap.Records.Single();
        rec.Pattern.Should().Be(pre.NormalizePattern("What is your name?"));
        rec.That.Should().Be(pre.NormalizeThat(string.Empty)); // no <that> -> empty normalized to ""
        rec.Topic.Should().Be(pre.NormalizeTopic("*"));
        rec.TemplateXml.Should().Be("<template>ALICE</template>");

        // Output preserves only surrounding text, not the <learn> block
        output.Should().Be("OK");
    }

    [Fact]
    public void Learn_supports_topic_wrapper_and_multiple_categories()
    {
        var (tp, session, match, cap, pre, _) = Make();

        var xml = """
                  <template>
                    <learn>
                      <topic name="Jokes">
                        <category>
                          <pattern>Tell me a joke</pattern>
                          <template>Knock knock.</template>
                        </category>
                        <category>
                          <pattern>Another joke</pattern>
                          <that>OK!</that>
                          <template>Banana.</template>
                        </category>
                      </topic>
                    </learn>
                    LEARNED
                  </template>
                  """;

        var output = tp.Process(xml, session, match);

        cap.Calls.Should().Be(2);

        var r1 = cap.Records[0];
        r1.Pattern.Should().Be(pre.NormalizePattern("Tell me a joke"));
        r1.That.Should().Be(pre.NormalizeThat(string.Empty));
        r1.Topic.Should().Be(pre.NormalizeTopic("Jokes"));
        r1.TemplateXml.Should().Be("<template>Knock knock.</template>");

        var r2 = cap.Records[1];
        r2.Pattern.Should().Be(pre.NormalizePattern("Another joke"));
        r2.That.Should().Be(pre.NormalizeThat("OK!"));
        r2.Topic.Should().Be(pre.NormalizeTopic("Jokes"));
        r2.TemplateXml.Should().Be("<template>Banana.</template>");

        output.Should().Be("LEARNED");
    }
}

public class TemplateProcessorDateTests
{
    [Fact]
    public void Date_without_format_uses_default_pattern()
    {
        // Fixed clock for determinism: 2025-09-07 14:30:00
        var fixedNow = new DateTime(2025, 09, 07, 14, 30, 00);

        var props = new BotProperties();
        var subs = SubstitutionTables.CreateClassicDefaults();
        var bot = new Bot(props, subs, thatHistoryDepth: 4);
        var pre = new PreProcessor(subs);

        var tp = new TemplateProcessor(
            bot,
            pre,
            sraiInvoker: (text, s, d) => string.Empty,
            rng: new Random(1),
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: null,
            nowProvider: () => fixedNow
        );

        var session = new UserSession("u1", bot);
        var path = Path.FromSegments(new[] { "*" }, new[] { "*" }, new[] { "*" });
        var stars = new Stars();
        var cat = new Category("*", "*", "*", "<template/>");
        var match = new Match(cat, path, stars, "*", "*", "*");

        var xml = "<template>The time is <date/></template>";
        var result = tp.Process(xml, session, match);

        // Default we will implement as "yyyy-MM-dd HH:mm"
        result.Should().Be("The time is 2025-09-07 14:30");
    }

    [Fact]
    public void Date_with_format_attribute_honors_dotnet_format_string()
    {
        var fixedNow = new DateTime(2025, 09, 07, 14, 30, 00);

        var props = new BotProperties();
        var subs = SubstitutionTables.CreateClassicDefaults();
        var bot = new Bot(props, subs, thatHistoryDepth: 4);
        var pre = new PreProcessor(subs);

        var tp = new TemplateProcessor(
            bot,
            pre,
            sraiInvoker: (text, s, d) => string.Empty,
            rng: new Random(1),
            maxDepth: TemplateProcessor.DefaultMaxDepth,
            learnEmitter: null,
            nowProvider: () => fixedNow
        );

        var session = new UserSession("u1", bot);
        var path = Path.FromSegments(new[] { "*" }, new[] { "*" }, new[] { "*" });
        var stars = new Stars();
        var cat = new Category("*", "*", "*", "<template/>");
        var match = new Match(cat, path, stars, "*", "*", "*");

        var xml = "<template>Today is <date format=\"yyyy/MM/dd\"/></template>";
        var result = tp.Process(xml, session, match);

        result.Should().Be("Today is 2025/09/07");
    }
}
