using System.Collections.Generic;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Parsing;
using Tomlyn.Serialization;
using Tomlyn.Syntax;

namespace Tomlyn.Tests;

public class NewApiParsingPipelineTests
{
    [Test]
    public void TomlParser_MoveNext_EmitsExpectedEvents()
    {
        var parser = TomlParser.Create(
            """
            name = "Ada"
            age = 37
            """);

        var events = new List<TomlParseEventKind>();
        var names = new List<string>();
        while (parser.MoveNext())
        {
            events.Add(parser.Current.Kind);
            if (parser.Current.Kind == TomlParseEventKind.PropertyName)
            {
                names.Add(parser.GetPropertyName());
            }
        }

        Assert.That(events, Is.EqualTo(new[]
        {
            TomlParseEventKind.StartDocument,
            TomlParseEventKind.StartTable,
            TomlParseEventKind.PropertyName,
            TomlParseEventKind.String,
            TomlParseEventKind.PropertyName,
            TomlParseEventKind.Integer,
            TomlParseEventKind.EndTable,
            TomlParseEventKind.EndDocument,
        }));
        Assert.That(names, Is.EqualTo(new[] { "name", "age" }));
    }

    [Test]
    public void TomlLexer_ExposesTokensInKeyAndValueModes()
    {
        var lexer = TomlLexer.Create("age = 37\n");
        var tokens = new List<TokenKind>();

        while (lexer.MoveNext())
        {
            var token = lexer.Current;
            tokens.Add(token.Kind);

            if (token.Kind == TokenKind.Equal)
            {
                lexer.Mode = TomlLexerMode.Value;
            }
            else if (token.Kind == TokenKind.NewLine)
            {
                lexer.Mode = TomlLexerMode.Key;
            }
        }

        Assert.That(tokens, Does.Contain(TokenKind.BasicKey));
        Assert.That(tokens, Does.Contain(TokenKind.Equal));
        Assert.That(tokens, Does.Contain(TokenKind.Integer));
    }

    [Test]
    public void TomlLexer_Default_DoesNotDecodeStringValues()
    {
        var lexer = TomlLexer.Create("name = \"A\\e\\x41\"\n");
        while (lexer.MoveNext())
        {
            if (lexer.Current.Kind == TokenKind.Equal)
            {
                lexer.Mode = TomlLexerMode.Value;
            }

            if (lexer.Current.Kind == TokenKind.String)
            {
                Assert.That(lexer.Current.StringValue, Is.Null);
                return;
            }
        }

        Assert.Fail("Expected to encounter a string token.");
    }

    [Test]
    public void TomlLexer_DecodeScalars_EmitsDecodedStringValues()
    {
        var lexer = TomlLexer.Create("name = \"A\\e\\x41\"\n", new TomlLexerOptions { DecodeScalars = true });
        while (lexer.MoveNext())
        {
            if (lexer.Current.Kind == TokenKind.Equal)
            {
                lexer.Mode = TomlLexerMode.Value;
            }

            if (lexer.Current.Kind == TokenKind.String)
            {
                Assert.That(lexer.Current.StringValue, Is.EqualTo("A\u001BA"));
                return;
            }
        }

        Assert.Fail("Expected to encounter a string token.");
    }

    [Test]
    public void TomlParser_TolerantMode_RecordsDiagnosticsAndTerminates()
    {
        var parser = TomlParser.Create(
            "a =\n",
            new Tomlyn.Parsing.TomlParserOptions { Mode = Tomlyn.Parsing.TomlParserMode.Tolerant },
            new TomlSerializerOptions { SourceName = "test.toml" });

        var events = new List<TomlParseEventKind>();
        Assert.DoesNotThrow(() =>
        {
            while (parser.MoveNext())
            {
                events.Add(parser.Current.Kind);
            }
        });

        Assert.That(parser.HasErrors, Is.True);
        Assert.That(parser.Diagnostics.HasErrors, Is.True);
        Assert.That(parser.Diagnostics.Count, Is.GreaterThan(0));
        Assert.That(parser.Diagnostics[0].Message, Does.Contain("Missing value"));
        Assert.That(events, Is.EqualTo(new[]
        {
            TomlParseEventKind.StartDocument,
            TomlParseEventKind.StartTable,
            TomlParseEventKind.PropertyName,
            TomlParseEventKind.EndTable,
            TomlParseEventKind.EndDocument,
        }));
    }

    [Test]
    public void Deserialize_Object_ReadsNestedTablesAndArrays()
    {
        var result = TomlSerializer.Deserialize<object>(
            """
            title = "doc"
            values = [1, 2]

            [child]
            enabled = true
            """);

        Assert.That(result, Is.TypeOf<TomlTable>());
        var root = (TomlTable)result!;
        Assert.That(root["title"], Is.EqualTo("doc"));
        Assert.That(root["values"], Is.TypeOf<TomlArray>());
        Assert.That(root["child"], Is.TypeOf<TomlTable>());

        var child = (TomlTable)root["child"];
        Assert.That(child["enabled"], Is.EqualTo(true));
    }
}
