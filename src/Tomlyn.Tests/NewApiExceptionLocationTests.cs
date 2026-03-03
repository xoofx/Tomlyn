using NUnit.Framework;
using Tomlyn.Parsing;
using Tomlyn.Serialization;
using System.Text.Json.Serialization;

namespace Tomlyn.Tests;

public class NewApiExceptionLocationTests
{
    [Test]
    public void Deserialize_InvalidScalarType_IncludesLocation()
    {
        var options = new TomlSerializerOptions
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
            SourceName = "test.toml",
        };

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<long>("value = \"abc\"\n", options));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(9));
        Assert.That(ex.Message, Does.Contain("test.toml(1,9)"));
    }

    [Test]
    public void Parse_SyntaxError_IncludesLocation()
    {
        var options = new TomlSerializerOptions { SourceName = "test.toml" };

        var reader = TomlReader.Create("a =\n", options);
        reader.Read(); // StartDocument
        reader.Read(); // StartTable
        reader.Read(); // PropertyName
        var ex = Assert.Throws<TomlException>(() => reader.Read()); // missing value
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Line, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Column, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Message, Does.Contain("test.toml("));
    }

    [Test]
    public void Parse_InvalidUtf8_IncludesByteOffset()
    {
        // "a = " followed by an invalid UTF-8 sequence: 0xC3 0x28
        var bytes = new byte[] { (byte)'a', (byte)' ', (byte)'=', (byte)' ', 0xC3, 0x28, (byte)'\n' };
        var options = new TomlSerializerOptions { SourceName = "utf8.toml" };

        var parser = TomlParser.Create(bytes, options);
        var ex = Assert.Throws<TomlException>(() =>
        {
            while (parser.MoveNext())
            {
            }
        });

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Offset, Is.EqualTo(4));
        Assert.That(ex.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(5));
        Assert.That(ex.Message, Does.Contain("utf8.toml("));
    }

    [Test]
    public void Parse_InvalidKeyHexEscape_IncludesLocation()
    {
        var options = new TomlSerializerOptions { SourceName = "keys.toml" };
        var reader = TomlReader.Create("\"a\\xG0\" = 1\n", options);

        var ex = Assert.Throws<TomlException>(() =>
        {
            while (reader.Read())
            {
            }
        });

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Line, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Column, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Message, Does.Contain("keys.toml("));
    }

    [Test]
    public void Deserialize_RootValueKeyNotFound_IncludesLocation()
    {
        var options = new TomlSerializerOptions
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
            RootValueKeyName = "value",
            SourceName = "root.toml",
        };

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<long>("other = 1\n", options));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Line, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Column, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Message, Does.Contain("root.toml("));
    }

    public sealed class MissingRequiredModel
    {
        [JsonRequired]
        public string Name { get; set; } = string.Empty;
    }

    [Test]
    public void Deserialize_ReflectionMissingRequiredKey_IncludesLocation()
    {
        var options = new TomlSerializerOptions { SourceName = "required.toml" };

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<MissingRequiredModel>("other = 1\n", options));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Line, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Column, Is.Not.Null.And.GreaterThan(0));
        Assert.That(ex.Message, Does.Contain("required.toml("));
    }
}
