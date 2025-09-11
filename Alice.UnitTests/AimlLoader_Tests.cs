// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text;
using FluentAssertions;

namespace Genova.Alice.UnitTests;

public class AimlLoader_Tests
{
    private static (AimlLoader loader, CapturingListener listener, PreProcessor pre) Make()
    {
        PreProcessor pre = new (SubstitutionTables.CreateClassicDefaults());
        CapturingListener listener = new ();
        AimlLoader loader = new (pre, listener);
        return (loader, listener, pre);
    }

    [Fact]
    public void LoadFromString_emits_single_root_category_without_that_or_topic()
    {
        (AimlLoader loader, CapturingListener listener, PreProcessor _) = Make();

        const string xml = """
        <aiml version="1.0.1">
          <category>
            <pattern>HELLO</pattern>
            <template>Hello.</template>
          </category>
        </aiml>
        """;

        loader.LoadFromString(xml, sourceName: "memory://one.aiml");

        listener.Items.Should().HaveCount(1);
        CapturingListener.Item item = listener.Items[0];

        item.Pattern.Should().Be("HELLO");
        item.That.Should().Be("*");           // no <that> provided -> "*"
        item.Topic.Should().Be("*");          // no <topic> wrapper -> "*"
        item.TemplateXml.Should().Contain("<template>").And.Contain("Hello.");
        item.Source.Should().Be("memory://one.aiml");
    }

    [Fact]
    public void Loader_handles_topic_wrapper_and_that_element()
    {
        (AimlLoader loader, CapturingListener listener, PreProcessor _) = Make();

        const string xml = """
        <aiml>
          <topic name="JOKES">
            <category>
              <pattern>TELL ME A JOKE</pattern>
              <that>OK</that>
              <template>
                <random><li>J1</li></random>
              </template>
            </category>
          </topic>
        </aiml>
        """;

        loader.LoadFromString(xml, sourceName: "memory://jokes.aiml");

        listener.Items.Should().HaveCount(1);
        CapturingListener.Item item = listener.Items[0];

        item.Pattern.Should().Be("TELL ME A JOKE");
        item.That.Should().Be("OK");
        item.Topic.Should().Be("JOKES");
        item.TemplateXml.Should().StartWith("<template>").And.Contain("<random>").And.Contain("<li>J1</li>");
        item.Source.Should().Be("memory://jokes.aiml");
    }

    [Fact]
    public void Normalization_applies_when_enabled()
    {
        (AimlLoader loader, CapturingListener listener, PreProcessor pre) = Make();

        // Pattern contains punctuation and lower-case; should be normalized by loader
        const string xml = """
        <aiml>
          <category>
            <pattern>What is your name?</pattern>
            <template>I am ALICE.</template>
          </category>
        </aiml>
        """;

        loader.LoadFromString(xml, sourceName: "memory://norm.aiml", normalize: true);

        listener.Items.Should().HaveCount(1);
        CapturingListener.Item item = listener.Items[0];

        // Expect PreProcessor.NormalizePattern behavior
        item.Pattern.Should().Be(pre.NormalizePattern("What is your name?"));
        item.That.Should().Be("*");
        item.Topic.Should().Be("*");
    }

    [Fact]
    public void Multiple_categories_across_root_and_topics_are_all_emitted()
    {
        (AimlLoader loader, CapturingListener listener, PreProcessor _) = Make();

        const string xml = """
        <aiml>
          <category>
            <pattern>HELLO</pattern>
            <template>Hi.</template>
          </category>
          <topic name="SMALLTALK">
            <category>
              <pattern>HOW ARE YOU</pattern>
              <template>I'm fine.</template>
            </category>
            <category>
              <pattern>WHAT TIME IS IT</pattern>
              <that>*</that>
              <template><date/></template>
            </category>
          </topic>
        </aiml>
        """;

        loader.LoadFromString(xml, sourceName: "memory://multi.aiml");

        listener.Items.Should().HaveCount(3);

        listener.Items[0].Pattern.Should().Be("HELLO");
        listener.Items[0].Topic.Should().Be("*");

        listener.Items[1].Pattern.Should().Be("HOW ARE YOU");
        listener.Items[1].Topic.Should().Be("SMALLTALK");

        listener.Items[2].Pattern.Should().Be("WHAT TIME IS IT");
        listener.Items[2].That.Should().Be("*");
        listener.Items[2].TemplateXml.Should().Contain("<date/>");
        listener.Items[2].Topic.Should().Be("SMALLTALK");
    }

    [Fact]
    public void LoadFromStream_behaves_like_LoadFromString()
    {
        (AimlLoader loader, CapturingListener listener, PreProcessor _) = Make();

        const string xml = """
        <aiml>
          <category>
            <pattern>BYE</pattern>
            <template>Goodbye.</template>
          </category>
        </aiml>
        """;

        using MemoryStream ms = new (Encoding.UTF8.GetBytes(xml));
        loader.LoadFromStream(ms, sourceName: "memory://stream.aiml");

        listener.Items.Should().HaveCount(1);
        listener.Items[0].Pattern.Should().Be("BYE");
        listener.Items[0].TemplateXml.Should().Contain("Goodbye.");
        listener.Items[0].Source.Should().Be("memory://stream.aiml");
    }
}

internal sealed class CapturingListener : IAimlReaderListener
{
    internal sealed record Item(string Pattern, string That, string Topic, string TemplateXml, string? Source);

    internal List<Item> Items { get; } = [];

    public void OnCategory(string pattern, string that, string topic, string templateXml, string? sourceName = null)
    {
        Items.Add(new Item(pattern, that, topic, templateXml, sourceName));
    }
}
