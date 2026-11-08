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
}
