using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Parsing;
using Tomlyn.Serialization;
using Tomlyn.Syntax;

namespace Tomlyn.Tests;

public sealed class Toml11ParsingTests
{
    [Test]
    public void InlineTable_MultilineWithCommentsAndTrailingComma_IsAccepted()
    {
        var toml =
            """
            a = {
              b = 1, # comment
              c = 2,
            }
            """;

        var result = TomlSerializer.Deserialize<object>(toml);
        Assert.That(result, Is.TypeOf<TomlTable>());

        var table = (TomlTable)result!;
        Assert.That(table["a"], Is.TypeOf<TomlTable>());

        var inline = (TomlTable)table["a"];
        Assert.That(inline["b"], Is.EqualTo(1L));
        Assert.That(inline["c"], Is.EqualTo(2L));
    }

    [Test]
    public void LocalTime_MinuteOnly_IsAccepted()
    {
        var reader = TomlReader.Create("t = 07:32\n");
        Assert.That(reader.Read(), Is.True); // StartDocument
        Assert.That(reader.Read(), Is.True); // StartTable
        Assert.That(reader.Read(), Is.True); // PropertyName
        Assert.That(reader.PropertyName, Is.EqualTo("t"));
        Assert.That(reader.Read(), Is.True); // DateTime
        Assert.That(reader.TokenType, Is.EqualTo(TomlTokenType.DateTime));
        var value = reader.GetTomlDateTime();
        Assert.That(value.Kind, Is.EqualTo(TomlDateTimeKind.LocalTime));
    }

    [Test]
    public void LocalDateTime_MinuteOnly_IsAccepted()
    {
        var reader = TomlReader.Create("dt = 1979-05-27T07:32\n");
        Assert.That(reader.Read(), Is.True); // StartDocument
        Assert.That(reader.Read(), Is.True); // StartTable
        Assert.That(reader.Read(), Is.True); // PropertyName
        Assert.That(reader.PropertyName, Is.EqualTo("dt"));
        Assert.That(reader.Read(), Is.True); // DateTime
        var value = reader.GetTomlDateTime();
        Assert.That(value.Kind, Is.EqualTo(TomlDateTimeKind.LocalDateTime));
    }

    [Test]
    public void BasicString_ByteAndEscapeEscapes_AreDecoded()
    {
        var reader = TomlReader.Create("s = \"A\\e\\x41\"\n");
        reader.Read(); // StartDocument
        reader.Read(); // StartTable
        reader.Read(); // PropertyName
        reader.Read(); // String
        Assert.That(reader.GetString(), Is.EqualTo("A\u001BA"));
    }

    [Test]
    public void BasicString_InvalidByteEscape_ThrowsWithLocation()
    {
        var options = new TomlSerializerOptions { RootValueHandling = TomlRootValueHandling.WrapInRootKey };
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<string>("value = \"\\x0\"\n", options));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Message, Does.Contain("(1,"));
    }

    [Test]
    public void TomlLexer_Default_DoesNotDecode_ButStillLexes()
    {
        var lexer = TomlLexer.Create("s = \"A\\e\\x41\"\n");
        while (lexer.MoveNext())
        {
            if (lexer.Current.Kind == TokenKind.String)
            {
                Assert.That(lexer.Current.StringValue, Is.Null);
                return;
            }
        }

        Assert.Fail("Expected a string token.");
    }

    [Test]
    public void TomlParser_DecodeScalars_ProducesMaterializedStringValue()
    {
        var parser = TomlParser.Create(
            "s = \"A\\e\\x41\"\n",
            new Tomlyn.Parsing.TomlParserOptions { DecodeScalars = true });

        while (parser.MoveNext())
        {
            if (parser.Current.Kind == TomlParseEventKind.String)
            {
                Assert.That(parser.Current.StringValue, Is.EqualTo("A\u001BA"));
                Assert.That(parser.GetString(), Is.EqualTo("A\u001BA"));
                return;
            }
        }

        Assert.Fail("Expected a string parse event.");
    }
}
