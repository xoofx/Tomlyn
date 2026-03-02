using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Parsing;

/// <summary>
/// Provides incremental lexical tokenization for TOML text.
/// </summary>
public sealed class TomlLexer
{
    private readonly ILexerAdapter _adapter;
    private SyntaxTokenValue _current;

    private TomlLexer(ILexerAdapter adapter)
    {
        _adapter = adapter;
        _current = default;
    }

    /// <summary>
    /// Creates a lexer over TOML text.
    /// </summary>
    /// <param name="toml">The TOML payload.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <returns>A lexer instance positioned before the first token.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toml"/> is <c>null</c>.</exception>
    public static TomlLexer Create(string toml, string? sourceName = null)
    {
        if (toml is null)
        {
            throw new ArgumentNullException(nameof(toml));
        }

        var sourceView = new StringSourceView(toml, sourceName ?? string.Empty);
        var lexer = new Lexer<StringSourceView, StringCharacterIterator>(sourceView)
        {
            DecodeScalars = false,
        };
        return new TomlLexer(new LexerAdapter<StringSourceView, StringCharacterIterator>(lexer));
    }

    /// <summary>
    /// Creates a lexer over TOML text.
    /// </summary>
    /// <param name="toml">The TOML payload.</param>
    /// <param name="lexerOptions">Options controlling lexer behavior.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <returns>A lexer instance positioned before the first token.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toml"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="lexerOptions"/> is <c>null</c>.</exception>
    public static TomlLexer Create(string toml, TomlLexerOptions lexerOptions, string? sourceName = null)
    {
        if (toml is null)
        {
            throw new ArgumentNullException(nameof(toml));
        }

        if (lexerOptions is null)
        {
            throw new ArgumentNullException(nameof(lexerOptions));
        }

        var sourceView = new StringSourceView(toml, sourceName ?? string.Empty);
        var lexer = new Lexer<StringSourceView, StringCharacterIterator>(sourceView)
        {
            DecodeScalars = lexerOptions.DecodeScalars,
        };
        return new TomlLexer(new LexerAdapter<StringSourceView, StringCharacterIterator>(lexer));
    }

    /// <summary>
    /// Creates a lexer over TOML text from a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <returns>A lexer instance positioned before the first token.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
    public static TomlLexer Create(TextReader reader, string? sourceName = null)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return Create(reader.ReadToEnd(), sourceName);
    }

    /// <summary>
    /// Creates a lexer over TOML text from a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="lexerOptions">Options controlling lexer behavior.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <returns>A lexer instance positioned before the first token.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="lexerOptions"/> is <c>null</c>.</exception>
    public static TomlLexer Create(TextReader reader, TomlLexerOptions lexerOptions, string? sourceName = null)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (lexerOptions is null)
        {
            throw new ArgumentNullException(nameof(lexerOptions));
        }

        return Create(reader.ReadToEnd(), lexerOptions, sourceName);
    }

    /// <summary>
    /// Creates a lexer over UTF-8 TOML bytes.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML payload.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <returns>A lexer instance positioned before the first token.</returns>
    public static TomlLexer Create(ReadOnlySpan<byte> utf8Toml, string? sourceName = null)
    {
        var bytes = utf8Toml.ToArray();
        var sourceView = new StringUtf8SourceView(bytes, sourceName ?? string.Empty);
        var lexer = new Lexer<StringUtf8SourceView, StringCharacterUtf8Iterator>(sourceView)
        {
            DecodeScalars = false,
        };
        return new TomlLexer(new LexerAdapter<StringUtf8SourceView, StringCharacterUtf8Iterator>(lexer));
    }

    /// <summary>
    /// Creates a lexer over UTF-8 TOML bytes.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML payload.</param>
    /// <param name="lexerOptions">Options controlling lexer behavior.</param>
    /// <param name="sourceName">An optional source name used in diagnostics.</param>
    /// <returns>A lexer instance positioned before the first token.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lexerOptions"/> is <c>null</c>.</exception>
    public static TomlLexer Create(ReadOnlySpan<byte> utf8Toml, TomlLexerOptions lexerOptions, string? sourceName = null)
    {
        if (lexerOptions is null)
        {
            throw new ArgumentNullException(nameof(lexerOptions));
        }

        var bytes = utf8Toml.ToArray();
        var sourceView = new StringUtf8SourceView(bytes, sourceName ?? string.Empty);
        var lexer = new Lexer<StringUtf8SourceView, StringCharacterUtf8Iterator>(sourceView)
        {
            DecodeScalars = lexerOptions.DecodeScalars,
        };
        return new TomlLexer(new LexerAdapter<StringUtf8SourceView, StringCharacterUtf8Iterator>(lexer));
    }

    /// <summary>
    /// Gets the current token.
    /// </summary>
    public SyntaxTokenValue Current => _current;

    /// <summary>
    /// Gets or sets the lexer mode.
    /// </summary>
    public TomlLexerMode Mode
    {
        get => _adapter.State == LexerState.Key ? TomlLexerMode.Key : TomlLexerMode.Value;
        set => _adapter.State = value == TomlLexerMode.Key ? LexerState.Key : LexerState.Value;
    }

    /// <summary>
    /// Gets a value indicating whether lexer diagnostics include errors.
    /// </summary>
    public bool HasErrors => _adapter.HasErrors;

    /// <summary>
    /// Gets diagnostics produced by the lexer.
    /// </summary>
    public IEnumerable<DiagnosticMessage> Errors => _adapter.Errors;

    /// <summary>
    /// Advances to the next token.
    /// </summary>
    /// <returns><c>true</c> when a token is available; otherwise <c>false</c>.</returns>
    public bool MoveNext()
    {
        if (!_adapter.MoveNext())
        {
            return false;
        }

        _current = _adapter.Token;
        return true;
    }

    private interface ILexerAdapter
    {
        bool HasErrors { get; }

        IEnumerable<DiagnosticMessage> Errors { get; }

        LexerState State { get; set; }

        SyntaxTokenValue Token { get; }

        bool MoveNext();
    }

    private sealed class LexerAdapter<TSourceView, TCharReader> : ILexerAdapter
        where TSourceView : struct, ISourceView<TCharReader>
        where TCharReader : struct, CharacterIterator
    {
        private readonly Lexer<TSourceView, TCharReader> _lexer;

        public LexerAdapter(Lexer<TSourceView, TCharReader> lexer)
        {
            _lexer = lexer;
        }

        public bool HasErrors => _lexer.HasErrors;

        public IEnumerable<DiagnosticMessage> Errors => _lexer.Errors;

        public LexerState State
        {
            get => _lexer.State;
            set => _lexer.State = value;
        }

        public SyntaxTokenValue Token => _lexer.Token;

        public bool MoveNext() => _lexer.MoveNext();
    }
}
