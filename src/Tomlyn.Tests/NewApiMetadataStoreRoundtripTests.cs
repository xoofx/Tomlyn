using System.Collections.Generic;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public class NewApiMetadataStoreRoundtripTests
{
    private sealed class Sample
    {
        public int Value { get; set; }
    }

    private sealed class Config
    {
        public string? Name { get; set; }

        public Server? Server { get; set; }

        public List<Product>? Products { get; set; }
    }

    private sealed class Server
    {
        public string? Host { get; set; }

        public int Port { get; set; }
    }

    private sealed class Product
    {
        public string? Name { get; set; }

        public int Sku { get; set; }
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_PreservesTriviaAndHexIntegers()
    {
        var store = new TomlMetadataStore();
        var options = new TomlSerializerOptions
        {
            MetadataStore = store,
        };

        var sample = TomlSerializer.Deserialize<Sample>(
            """
            # Leading comment
            Value = 0x2A # Trailing comment
            """,
            options);

        var toml = TomlSerializer.Serialize(sample, options);

        Assert.That(toml, Does.Contain("# Leading comment"));
        Assert.That(toml, Does.Contain("# Trailing comment"));
        Assert.That(toml, Does.Contain("Value = 0x"));
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_PreservesTableAndTableArrayComments()
    {
        var store = new TomlMetadataStore();
        var options = new TomlSerializerOptions
        {
            MetadataStore = store,
        };

        // The comments keep the newlines of the input, whose raw string literal follows the line endings
        // of this file (CRLF in a Windows checkout): normalize them so that the assertions hold on every platform.
        var config = TomlSerializer.Deserialize<Config>(AssertHelper.NormalizeEndOfLine(
            """
            # Top comment
            Name = "app"

            # Server comment
            [Server]
            # Host comment
            Host = "localhost"
            # Port comment
            Port = 8080

            # Products comment
            [[Products]]
            # Product name comment
            Name = "Hammer"
            Sku = 1
            """),
            options)!;

        var toml = TomlSerializer.Serialize(config, options);

        Assert.That(toml, Does.Contain("# Top comment"));
        Assert.That(toml, Does.Contain("# Server comment\n[Server]"));
        Assert.That(toml, Does.Contain("# Host comment\nHost = "));
        Assert.That(toml, Does.Contain("# Port comment\nPort = "));
        Assert.That(toml, Does.Contain("# Products comment\n[[Products]]"));
        Assert.That(toml, Does.Contain("# Product name comment\nName = "));
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_PreservesTableHeaderTrailingComments()
    {
        var store = new TomlMetadataStore();
        var options = new TomlSerializerOptions
        {
            MetadataStore = store,
        };

        // Normalized for the same reason as in the test above.
        var config = TomlSerializer.Deserialize<Config>(AssertHelper.NormalizeEndOfLine(
            """
            [Server] # Server trailing comment
            Host = "localhost"

            [[Products]] # Products trailing comment
            Name = "Hammer"

            # Second product comment
            [[Products]]
            Name = "Nail"
            """),
            options)!;

        var toml = TomlSerializer.Serialize(config, options);

        Assert.That(toml, Does.Contain("[Server] # Server trailing comment\nHost = "));
        Assert.That(toml, Does.Contain("[[Products]] # Products trailing comment\nName = "));
        Assert.That(toml, Does.Contain("# Second product comment\n"));
    }

    [TestCase("\r\n", TomlNewLineKind.Lf, "\n")]
    [TestCase("\n", TomlNewLineKind.CrLf, "\r\n")]
    [TestCase("\r\n", TomlNewLineKind.CrLf, "\r\n")]
    public void MetadataStore_DeserializeThenSerialize_WritesTriviaWithConfiguredNewLine(string inputNewLine, TomlNewLineKind newLine, string expectedNewLine)
    {
        var options = new TomlSerializerOptions
        {
            MetadataStore = new TomlMetadataStore(),
            NewLine = newLine,
        };
        var input = string.Join(inputNewLine,
            "# Top comment",
            "",
            "Name = \"app\" # Name comment",
            "",
            "# Server comment",
            "[Server] # Server trailing comment",
            "Host = \"localhost\"",
            "",
            "[[Products]]",
            "Name = \"Hammer\"",
            "");

        var model = TomlSerializer.Deserialize<Tomlyn.Model.TomlTable>(input, options)!;
        var toml = TomlSerializer.Serialize(model, options);

        // Newlines ending comments and blank lines are captured as trivia with the line ending of the input:
        // they must be written with the configured newline like the other lines.
        Assert.That(toml, Is.EqualTo(input.Replace(inputNewLine, expectedNewLine)));
    }

    private const string DocumentWithBlankLines =
        "\n" +
        "# Top comment\n" +
        "\n" +
        "Name = \"app\"\n" +
        "\n" +
        "\n" +
        "# Server comment\n" +
        "\n" +
        "# Server header comment\n" +
        "[Server]\n" +
        "Host = \"localhost\"\n" +
        "\n" +
        "Port = 8080\n" +
        "\n" +
        "[[Products]]\n" +
        "Name = \"Hammer\"\n" +
        "Sku = 1\n";

    [Test]
    public void MetadataStore_DeserializeThenSerialize_TomlTable_PreservesBlankLines()
    {
        var options = new TomlSerializerOptions { MetadataStore = new TomlMetadataStore() };

        var model = TomlSerializer.Deserialize<Tomlyn.Model.TomlTable>(DocumentWithBlankLines, options)!;
        var toml = TomlSerializer.Serialize(model, options);

        Assert.That(toml, Is.EqualTo(DocumentWithBlankLines));
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_Poco_PreservesBlankLines()
    {
        var options = new TomlSerializerOptions { MetadataStore = new TomlMetadataStore() };

        var config = TomlSerializer.Deserialize<Config>(DocumentWithBlankLines, options)!;
        var toml = TomlSerializer.Serialize(config, options);

        Assert.That(toml, Is.EqualTo(DocumentWithBlankLines));
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_WithoutMetadataStore_DoesNotWriteBlankLines()
    {
        var model = TomlSerializer.Deserialize<Tomlyn.Model.TomlTable>(DocumentWithBlankLines)!;
        var toml = TomlSerializer.Serialize(model);

        Assert.That(toml, Is.EqualTo("Name = \"app\"\n[Server]\nHost = \"localhost\"\nPort = 8080\n[[Products]]\nName = \"Hammer\"\nSku = 1\n"));
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_TableArrayElements_DoNotDoubleBlankLines()
    {
        const string input =
            "[[Products]]\n" +
            "Name = \"Hammer\"\n" +
            "\n" +
            "\n" +
            "# Second product comment\n" +
            "[[Products]]\n" +
            "Name = \"Nail\"\n" +
            "\n" +
            "[[Products]]\n" +
            "Name = \"Screw\"\n";
        var options = new TomlSerializerOptions { MetadataStore = new TomlMetadataStore() };

        var model = TomlSerializer.Deserialize<Tomlyn.Model.TomlTable>(input, options)!;
        var toml = TomlSerializer.Serialize(model, options);

        // The trivia of a subsequent array element goes after its header (as before), without the blank lines
        // that preceded the header: the writer already separates the elements with a blank line.
        Assert.That(toml, Is.EqualTo(
            "[[Products]]\n" +
            "Name = \"Hammer\"\n" +
            "\n" +
            "[[Products]]\n" +
            "# Second product comment\n" +
            "Name = \"Nail\"\n" +
            "\n" +
            "[[Products]]\n" +
            "Name = \"Screw\"\n"));
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_BlankLinesInMultilineArray_AreNotTrivia()
    {
        const string input =
            "Values = [\n" +
            "  1,\n" +
            "\n" +
            "  2,\n" +
            "]\n" +
            "\n" +
            "Name = \"app\"\n";
        var options = new TomlSerializerOptions { MetadataStore = new TomlMetadataStore() };

        var model = TomlSerializer.Deserialize<Tomlyn.Model.TomlTable>(input, options)!;
        var toml = TomlSerializer.Serialize(model, options);

        Assert.That(toml, Is.EqualTo("Values = [1, 2]\n\nName = \"app\"\n"));
    }

    [Test]
    public void MetadataStore_BlankLineBeforeTableHeaders_DoesNotDoubleParsedBlankLine()
    {
        const string input = "a = 1\n\n[t]\nb = 2\n[u]\nc = 3\n";
        var options = new TomlSerializerOptions { MetadataStore = new TomlMetadataStore(), BlankLineBeforeTableHeaders = true };

        var model = TomlSerializer.Deserialize<Tomlyn.Model.TomlTable>(input, options)!;
        var toml = TomlSerializer.Serialize(model, options);

        Assert.That(toml, Is.EqualTo("a = 1\n\n[t]\nb = 2\n\n[u]\nc = 3\n"));
    }
}
