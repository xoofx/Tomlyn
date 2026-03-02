using System;
using System.IO;
using Tomlyn.Helpers;
using Tomlyn.Parsing;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Serialization;

/// <summary>
/// Reads TOML tokens for use by <see cref="TomlConverter"/> implementations.
/// </summary>
public sealed class TomlReader
{
    private readonly TomlSerializerOptions _options;
    private readonly TomlParser _parser;
    private string? _currentPropertyName;
    private string? _currentString;
    private ulong _currentData;
    private TokenKind _currentStringTokenKind;
    private TomlDateTime _currentDateTime;
    private bool _hasDateTime;
    private TomlSourceSpan? _currentSpan;
    private TomlTokenType _tokenType;

    private TomlReader(TomlParser parser, TomlSerializerOptions options)
    {
        _parser = parser;
        _options = options ?? TomlSerializerOptions.Default;
        _tokenType = TomlTokenType.None;
    }

    /// <summary>
    /// Creates a TOML reader over a string payload.
    /// </summary>
    public static TomlReader Create(string toml, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var parser = TomlParser.Create(toml, effectiveOptions);
        return new TomlReader(parser, effectiveOptions);
    }

    /// <summary>
    /// Creates a TOML reader over a text reader.
    /// </summary>
    public static TomlReader Create(TextReader reader, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var parser = TomlParser.Create(reader, effectiveOptions);
        return new TomlReader(parser, effectiveOptions);
    }

    /// <summary>
    /// Creates a TOML reader over a UTF-8 payload.
    /// </summary>
    public static TomlReader Create(ReadOnlySpan<byte> utf8Toml, TomlSerializerOptions? options = null)
    {
        var bytes = utf8Toml.ToArray();
        return Create(bytes, options);
    }

    /// <summary>
    /// Creates a TOML reader over a UTF-8 payload.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="utf8Toml"/> is <c>null</c>.</exception>
    public static TomlReader Create(byte[] utf8Toml, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(utf8Toml, nameof(utf8Toml));
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var parser = TomlParser.Create(utf8Toml, effectiveOptions);
        return new TomlReader(parser, effectiveOptions);
    }

    /// <summary>
    /// Gets the current token type.
    /// </summary>
    public TomlTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the current property name when <see cref="TokenType"/> is <see cref="TomlTokenType.PropertyName"/>.
    /// </summary>
    public string? PropertyName => _currentPropertyName;

    /// <summary>
    /// Gets the optional source name associated with the TOML payload (for example, a file path).
    /// </summary>
    public string? SourceName => _options.SourceName;

    /// <summary>
    /// Gets the current line number (1-based).
    /// </summary>
    public int Line => _currentSpan?.Start.Line + 1 ?? 0;

    /// <summary>
    /// Gets the current column number (1-based).
    /// </summary>
    public int Column => _currentSpan?.Start.Column + 1 ?? 0;

    /// <summary>
    /// Gets the optional source span associated with the current token.
    /// </summary>
    public TomlSourceSpan? CurrentSpan => _currentSpan;

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    public bool Read()
    {
        _currentPropertyName = null;
        _currentString = null;
        _currentData = 0;
        _currentStringTokenKind = default;
        _currentDateTime = default;
        _hasDateTime = false;
        _currentSpan = null;

        if (!_parser.MoveNext())
        {
            _tokenType = TomlTokenType.EndDocument;
            return false;
        }

        var parseEvent = _parser.Current;
        _currentSpan = parseEvent.Span;
        switch (parseEvent.Kind)
        {
            case TomlParseEventKind.StartDocument:
                _tokenType = TomlTokenType.StartDocument;
                break;
            case TomlParseEventKind.EndDocument:
                _tokenType = TomlTokenType.EndDocument;
                break;
            case TomlParseEventKind.StartTable:
                _tokenType = TomlTokenType.StartTable;
                break;
            case TomlParseEventKind.EndTable:
                _tokenType = TomlTokenType.EndTable;
                break;
            case TomlParseEventKind.PropertyName:
                _tokenType = TomlTokenType.PropertyName;
                _currentPropertyName = parseEvent.PropertyName ?? _parser.GetPropertyName();
                break;
            case TomlParseEventKind.StartArray:
                _tokenType = TomlTokenType.StartArray;
                break;
            case TomlParseEventKind.EndArray:
                _tokenType = TomlTokenType.EndArray;
                break;
            case TomlParseEventKind.String:
                _tokenType = TomlTokenType.String;
                _currentStringTokenKind = (TokenKind)parseEvent.Data;
                break;
            case TomlParseEventKind.Integer:
                _tokenType = TomlTokenType.Integer;
                _currentData = parseEvent.Data;
                break;
            case TomlParseEventKind.Float:
                _tokenType = TomlTokenType.Float;
                _currentData = parseEvent.Data;
                break;
            case TomlParseEventKind.Boolean:
                _tokenType = TomlTokenType.Boolean;
                _currentData = parseEvent.Data;
                break;
            case TomlParseEventKind.DateTime:
                _tokenType = TomlTokenType.DateTime;
                _currentDateTime = parseEvent.GetTomlDateTime();
                _hasDateTime = true;
                break;
            default:
                throw CreateException($"Unsupported TOML parse event `{parseEvent.Kind}`.");
        }

        return true;
    }

    /// <summary>
    /// Skips the current value.
    /// </summary>
    public void Skip()
    {
        if (_tokenType is not TomlTokenType.StartArray and not TomlTokenType.StartTable)
        {
            Read();
            return;
        }

        var depth = 0;
        while (true)
        {
            if (_tokenType is TomlTokenType.StartArray or TomlTokenType.StartTable) depth++;
            else if (_tokenType is TomlTokenType.EndArray or TomlTokenType.EndTable) depth--;

            if (depth == 0)
            {
                Read();
                return;
            }

            if (!Read())
            {
                return;
            }
        }
    }

    /// <summary>
    /// Gets the current string value.
    /// </summary>
    public string GetString()
    {
        if (_tokenType != TomlTokenType.String)
        {
            throw CreateException($"Expected token {TomlTokenType.String} but was {_tokenType}.");
        }

        if (_currentString is not null)
        {
            return _currentString;
        }

        if (_currentSpan is not { } span)
        {
            throw CreateException("The current token does not have a source span.");
        }

        var raw = _parser.GetText(span);
        if (raw is null)
        {
            _currentString = string.Empty;
            return _currentString;
        }

        try
        {
            _currentString = TomlStringDecoder.Decode(raw, _currentStringTokenKind);
            return _currentString;
        }
        catch (FormatException ex)
        {
            throw CreateException(ex.Message);
        }
    }

    /// <summary>
    /// Gets the current integer value.
    /// </summary>
    public long GetInt64()
    {
        if (_tokenType != TomlTokenType.Integer)
        {
            throw CreateException($"Expected token {TomlTokenType.Integer} but was {_tokenType}.");
        }

        return unchecked((long)_currentData);
    }

    /// <summary>
    /// Gets the current floating value.
    /// </summary>
    public double GetDouble()
    {
        if (_tokenType != TomlTokenType.Float && _tokenType != TomlTokenType.Integer)
        {
            throw CreateException($"Expected token {TomlTokenType.Float} but was {_tokenType}.");
        }

        return _tokenType == TomlTokenType.Float
            ? BitConverter.Int64BitsToDouble(unchecked((long)_currentData))
            : GetInt64();
    }

    /// <summary>
    /// Gets the current floating value as decimal.
    /// </summary>
    public decimal GetDecimal()
    {
        if (_tokenType != TomlTokenType.Float && _tokenType != TomlTokenType.Integer)
        {
            throw CreateException($"Expected token {TomlTokenType.Float} but was {_tokenType}.");
        }

        if (_tokenType == TomlTokenType.Integer)
        {
            return GetInt64();
        }

        return (decimal)GetDouble();
    }

    /// <summary>
    /// Gets the current boolean value.
    /// </summary>
    public bool GetBoolean()
    {
        if (_tokenType != TomlTokenType.Boolean)
        {
            throw CreateException($"Expected token {TomlTokenType.Boolean} but was {_tokenType}.");
        }

        return _currentData != 0;
    }

    /// <summary>
    /// Gets the current TOML datetime value.
    /// </summary>
    public TomlDateTime GetTomlDateTime()
    {
        if (_tokenType != TomlTokenType.DateTime)
        {
            throw CreateException($"Expected token {TomlTokenType.DateTime} but was {_tokenType}.");
        }

        if (!_hasDateTime)
        {
            throw CreateException("The current datetime token does not have a parsed value.");
        }

        return _currentDateTime;
    }

    /// <summary>
    /// Creates a <see cref="TomlException"/> with the current source location (when available).
    /// </summary>
    /// <param name="message">The exception message.</param>
    public TomlException CreateException(string message)
    {
        if (_currentSpan is { } span)
        {
            return new TomlException(span, message);
        }

        return new TomlException(message);
    }
}
