using System.Collections.Generic;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class FormattingConfig
{
    public string Name { get; set; } = "foo";

    public int Version { get; set; } = 1;

    public FormattingDatabase Database { get; set; } = new();

    public FormattingLogging Logging { get; set; } = new();

    public List<FormattingPlugin> Plugins { get; set; } = [new() { Name = "a" }, new() { Name = "b" }];
}

public sealed class FormattingDatabase
{
    public string Host { get; set; } = "localhost";
}

public sealed class FormattingLogging
{
    public string Level { get; set; } = "info";
}

public sealed class FormattingPlugin
{
    public string Name { get; set; } = string.Empty;
}

public sealed class FormattingTablesOnly
{
    public FormattingDatabase Database { get; set; } = new();
}

public sealed class FormattingArrays
{
    public string Name { get; set; } = "Example";

    public List<string> Items { get; set; } = ["A", "B", "C"];

    public List<int> Empty { get; set; } = [];

    public List<List<int>> Nested { get; set; } = [[1, 2], [3]];

    [TomlInlineTable(TomlInlineTablePolicy.Always)]
    public FormattingArrayOwner Owner { get; set; } = new();

    [TomlTableArrayStyle(TomlTableArrayStyle.InlineArrayOfTables)]
    public List<FormattingPlugin> Plugins { get; set; } = [new() { Name = "a" }, new() { Name = "b" }];
}

public sealed class FormattingArrayOwner
{
    public List<int> Values { get; set; } = [1, 2];
}

[TomlSourceGenerationOptions(BlankLineBeforeTableHeaders = true)]
[TomlSerializable(typeof(FormattingConfig))]
internal partial class TestBlankLineBeforeTableHeadersContext : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(ArrayLayout = TomlArrayLayout.Multiline, IndentSize = 4)]
[TomlSerializable(typeof(FormattingArrays))]
internal partial class TestMultilineArrayContext : TomlSerializerContext
{
}

public class NewApiFormattingOptionsTests
{
    private const string ExpectedBlankLineBeforeTableHeaders =
        "Name = \"foo\"\n" +
        "Version = 1\n" +
        "\n" +
        "[Database]\n" +
        "Host = \"localhost\"\n" +
        "\n" +
        "[Logging]\n" +
        "Level = \"info\"\n" +
        "\n" +
        "[[Plugins]]\n" +
        "Name = \"a\"\n" +
        "\n" +
        "[[Plugins]]\n" +
        "Name = \"b\"\n";

    [Test]
    public void BlankLineBeforeTableHeaders_Default_DoesNotSeparateTables()
    {
        var toml = TomlSerializer.Serialize(new FormattingConfig());

        Assert.That(toml, Does.Contain("Version = 1\n[Database]\n"));
        Assert.That(toml, Does.Contain("Host = \"localhost\"\n[Logging]\n"));
    }

    [Test]
    public void BlankLineBeforeTableHeaders_Enabled_SeparatesTablesAndTableArrays()
    {
        var options = new TomlSerializerOptions { BlankLineBeforeTableHeaders = true };

        var toml = TomlSerializer.Serialize(new FormattingConfig(), options);

        Assert.That(toml, Is.EqualTo(ExpectedBlankLineBeforeTableHeaders));
    }

    [Test]
    public void BlankLineBeforeTableHeaders_Enabled_DoesNotStartDocumentWithBlankLine()
    {
        var options = new TomlSerializerOptions { BlankLineBeforeTableHeaders = true };

        var toml = TomlSerializer.Serialize(new FormattingTablesOnly(), options);

        Assert.That(toml, Is.EqualTo("[Database]\nHost = \"localhost\"\n"));
    }

    [Test]
    public void BlankLineBeforeTableHeaders_Enabled_DoesNotDoubleBlankLinesFromMetadata()
    {
        const string input = "a = 1\n\n# Table comment\n[t]\nb = 2\n[u]\nc = 3\n";
        var store = new TomlMetadataStore();
        var options = new TomlSerializerOptions { BlankLineBeforeTableHeaders = true, MetadataStore = store };

        var model = TomlSerializer.Deserialize<TomlTable>(input, options)!;
        var toml = TomlSerializer.Serialize(model, options);

        Assert.That(toml, Is.EqualTo("a = 1\n\n# Table comment\n[t]\nb = 2\n\n[u]\nc = 3\n"));
    }

    [Test]
    public void ArrayLayout_Default_WritesInlineArrays()
    {
        var toml = TomlSerializer.Serialize(new FormattingArrays());

        Assert.That(toml, Does.Contain("Items = [\"A\", \"B\", \"C\"]\n"));
    }

    [Test]
    public void ArrayLayout_Multiline_WritesOneElementPerLine()
    {
        var options = new TomlSerializerOptions { ArrayLayout = TomlArrayLayout.Multiline };

        var toml = TomlSerializer.Serialize(new FormattingArrays(), options);

        Assert.That(toml, Is.EqualTo(
            "Name = \"Example\"\n" +
            "Items = [\n" +
            "  \"A\",\n" +
            "  \"B\",\n" +
            "  \"C\",\n" +
            "]\n" +
            "Empty = []\n" +
            "Nested = [\n" +
            "  [1, 2],\n" +
            "  [3],\n" +
            "]\n" +
            "Owner = {Values = [1, 2]}\n" +
            "Plugins = [\n" +
            "  {Name = \"a\"},\n" +
            "  {Name = \"b\"},\n" +
            "]\n"));
    }

    [Test]
    public void ArrayLayout_Multiline_Roundtrips()
    {
        var options = new TomlSerializerOptions { ArrayLayout = TomlArrayLayout.Multiline, NewLine = TomlNewLineKind.CrLf };

        var toml = TomlSerializer.Serialize(new FormattingArrays { Items = ["x", "y"] }, options);
        var roundtrip = TomlSerializer.Deserialize<FormattingArrays>(toml, options)!;

        Assert.That(toml, Does.Contain("Items = [\r\n  \"x\",\r\n  \"y\",\r\n]\r\n"));
        Assert.That(roundtrip.Items, Is.EqualTo(new[] { "x", "y" }));
        Assert.That(roundtrip.Nested, Is.EqualTo(new[] { new[] { 1, 2 }, new[] { 3 } }));
        Assert.That(roundtrip.Plugins.ConvertAll(static plugin => plugin.Name), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void ArrayLayout_Multiline_WithoutIndentation()
    {
        var options = new TomlSerializerOptions { ArrayLayout = TomlArrayLayout.Multiline, WriteIndented = false };

        var toml = TomlSerializer.Serialize(new FormattingArrays(), options);

        Assert.That(toml, Does.Contain("Items = [\n\"A\",\n\"B\",\n\"C\",\n]\n"));
    }

    [Test]
    public void ArrayLayout_Multiline_AppliesToTomlTableModel()
    {
        var model = new TomlTable { ["values"] = new TomlArray { 1, 2 } };
        var options = new TomlSerializerOptions { ArrayLayout = TomlArrayLayout.Multiline };

        var toml = TomlSerializer.Serialize(model, options);

        Assert.That(toml, Is.EqualTo("values = [\n  1,\n  2,\n]\n"));
    }

    [Test]
    public void ArrayLayout_FromSourceGenerationOptions()
    {
        var toml = TomlSerializer.Serialize(new FormattingArrays(), TestMultilineArrayContext.Default.FormattingArrays);

        Assert.That(toml, Does.Contain("Items = [\n    \"A\",\n    \"B\",\n    \"C\",\n]\n"));
    }

    [Test]
    public void BlankLineBeforeTableHeaders_FromSourceGenerationOptions()
    {
        var toml = TomlSerializer.Serialize(new FormattingConfig(), TestBlankLineBeforeTableHeadersContext.Default.FormattingConfig);

        Assert.That(toml, Is.EqualTo(ExpectedBlankLineBeforeTableHeaders));
    }
}
