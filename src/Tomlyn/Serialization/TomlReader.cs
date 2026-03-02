using System;
using System.IO;
using Tomlyn;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization;

/// <summary>
/// Reads TOML tokens for use by <see cref="TomlConverter"/> implementations.
/// </summary>
public sealed class TomlReader
{
    private TomlReader()
    {
    }

    /// <summary>
    /// Creates a TOML reader over a string payload.
    /// </summary>
    public static TomlReader Create(string toml, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        _ = options;
        throw new NotImplementedException("TomlReader is scaffolded; implementation is added in subsequent commits.");
    }

    /// <summary>
    /// Creates a TOML reader over a text reader.
    /// </summary>
    public static TomlReader Create(TextReader reader, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        _ = options;
        throw new NotImplementedException("TomlReader is scaffolded; implementation is added in subsequent commits.");
    }

    /// <summary>
    /// Creates a TOML reader over a UTF-8 payload.
    /// </summary>
    public static TomlReader Create(ReadOnlySpan<byte> utf8Toml, TomlSerializerOptions? options = null)
    {
        _ = utf8Toml;
        _ = options;
        throw new NotImplementedException("TomlReader is scaffolded; implementation is added in subsequent commits.");
    }

    /// <summary>
    /// Gets the current token type.
    /// </summary>
    public TomlTokenType TokenType => TomlTokenType.None;

    /// <summary>
    /// Gets the current property name when <see cref="TokenType"/> is <see cref="TomlTokenType.PropertyName"/>.
    /// </summary>
    public string? PropertyName => null;

    /// <summary>
    /// Gets the optional source name associated with the TOML payload (for example, a file path).
    /// </summary>
    public string? SourceName => null;

    /// <summary>
    /// Gets the current line number (1-based).
    /// </summary>
    public int Line => 1;

    /// <summary>
    /// Gets the current column number (1-based).
    /// </summary>
    public int Column => 1;

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    public bool Read() => throw new NotImplementedException();

    /// <summary>
    /// Skips the current value.
    /// </summary>
    public void Skip() => throw new NotImplementedException();

    /// <summary>
    /// Gets the current string value.
    /// </summary>
    public string GetString() => throw new NotImplementedException();

    /// <summary>
    /// Gets the current integer value.
    /// </summary>
    public long GetInt64() => throw new NotImplementedException();

    /// <summary>
    /// Gets the current floating value.
    /// </summary>
    public double GetDouble() => throw new NotImplementedException();

    /// <summary>
    /// Gets the current floating value as decimal.
    /// </summary>
    public decimal GetDecimal() => throw new NotImplementedException();

    /// <summary>
    /// Gets the current boolean value.
    /// </summary>
    public bool GetBoolean() => throw new NotImplementedException();

    /// <summary>
    /// Gets the current TOML datetime value.
    /// </summary>
    public TomlDateTime GetTomlDateTime() => throw new NotImplementedException();
}
