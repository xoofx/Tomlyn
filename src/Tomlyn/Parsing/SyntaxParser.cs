using System;
using System.IO;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Parsing;

/// <summary>
/// Provides a full-fidelity TOML syntax parser producing a round-trippable <see cref="DocumentSyntax"/>.
/// </summary>
public static class SyntaxParser
{
    /// <summary>
    /// Parses a TOML payload into a syntax tree.
    /// </summary>
    /// <param name="toml">The TOML payload.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <param name="validate">When <c>true</c>, runs semantic validation after parsing.</param>
    /// <returns>The parsed syntax tree, possibly containing diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toml"/> is <c>null</c>.</exception>
    public static DocumentSyntax Parse(string toml, string? sourceName = null, bool validate = true)
    {
        if (toml is null)
        {
            throw new ArgumentNullException(nameof(toml));
        }

        var sourceView = new StringSourceView(toml, sourceName ?? string.Empty);
        var lexer = new Lexer<StringSourceView, StringCharacterIterator>(sourceView);
        var parser = new Parser<StringSourceView>(lexer);
        var doc = parser.Run();
        if (validate && !doc.HasErrors)
        {
            Toml.Validate(doc);
        }

        return doc;
    }

    /// <summary>
    /// Parses a TOML payload from a <see cref="TextReader"/> into a syntax tree.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <param name="validate">When <c>true</c>, runs semantic validation after parsing.</param>
    /// <returns>The parsed syntax tree, possibly containing diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
    public static DocumentSyntax Parse(TextReader reader, string? sourceName = null, bool validate = true)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return Parse(reader.ReadToEnd(), sourceName, validate);
    }

    /// <summary>
    /// Parses a UTF-8 TOML payload into a syntax tree.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML payload.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <param name="validate">When <c>true</c>, runs semantic validation after parsing.</param>
    /// <returns>The parsed syntax tree, possibly containing diagnostics.</returns>
    public static DocumentSyntax Parse(ReadOnlySpan<byte> utf8Toml, string? sourceName = null, bool validate = true)
    {
        var bytes = utf8Toml.ToArray();
        var sourceView = new StringUtf8SourceView(bytes, sourceName ?? string.Empty);
        var lexer = new Lexer<StringUtf8SourceView, StringCharacterUtf8Iterator>(sourceView);
        var parser = new Parser<StringUtf8SourceView>(lexer);
        var doc = parser.Run();
        if (validate && !doc.HasErrors)
        {
            Toml.Validate(doc);
        }

        return doc;
    }

    /// <summary>
    /// Parses a TOML payload into a syntax tree and throws on any parse/validation error.
    /// </summary>
    /// <param name="toml">The TOML payload.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <param name="validate">When <c>true</c>, runs semantic validation after parsing.</param>
    /// <returns>The parsed syntax tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toml"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static DocumentSyntax ParseStrict(string toml, string? sourceName = null, bool validate = true)
    {
        var doc = Parse(toml, sourceName, validate);
        if (doc.HasErrors)
        {
            throw new TomlException(doc.Diagnostics);
        }

        return doc;
    }
}

