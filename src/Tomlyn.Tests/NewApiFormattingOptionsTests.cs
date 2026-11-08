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

[TomlSourceGenerationOptions(BlankLineBeforeTableHeaders = true)]
[TomlSerializable(typeof(FormattingConfig))]
internal partial class TestBlankLineBeforeTableHeadersContext : TomlSerializerContext
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
    public void BlankLineBeforeTableHeaders_FromSourceGenerationOptions()
    {
        var toml = TomlSerializer.Serialize(new FormattingConfig(), TestBlankLineBeforeTableHeadersContext.Default.FormattingConfig);

        Assert.That(toml, Is.EqualTo(ExpectedBlankLineBeforeTableHeaders));
    }
}
