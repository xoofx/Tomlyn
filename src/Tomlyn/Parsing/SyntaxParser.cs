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
    /// <param name="lexer">A TOML lexer positioned before the first token.</param>
    /// <param name="validate">When <c>true</c>, runs semantic validation after parsing.</param>
    /// <returns>The parsed syntax tree, possibly containing diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lexer"/> is <c>null</c>.</exception>
    public static DocumentSyntax Parse(TomlLexer lexer, bool validate = true)
    {
        if (lexer is null)
        {
            throw new ArgumentNullException(nameof(lexer));
        }

        var parser = new Parser<ISourceView>(lexer.CreateTokenProvider());
        var doc = parser.Run();
        if (validate && !doc.HasErrors)
        {
            var validator = new SyntaxValidator(doc.Diagnostics);
            validator.Visit(doc);
        }

        return doc;
    }

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

        var lexer = TomlLexer.Create(toml, new TomlLexerOptions { DecodeScalars = true }, sourceName);
        return Parse(lexer, validate);
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
        var lexer = TomlLexer.Create(utf8Toml, new TomlLexerOptions { DecodeScalars = true }, sourceName);
        return Parse(lexer, validate);
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

    /// <summary>
    /// Parses a TOML payload into a syntax tree and throws on any parse/validation error.
    /// </summary>
    /// <param name="lexer">A TOML lexer positioned before the first token.</param>
    /// <param name="validate">When <c>true</c>, runs semantic validation after parsing.</param>
    /// <returns>The parsed syntax tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lexer"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static DocumentSyntax ParseStrict(TomlLexer lexer, bool validate = true)
    {
        var doc = Parse(lexer, validate);
        if (doc.HasErrors)
        {
            throw new TomlException(doc.Diagnostics);
        }

        return doc;
    }
}
