using System;
using System.IO;
using Tomlyn;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization;

/// <summary>
/// Writes TOML tokens for use by <see cref="TomlConverter"/> implementations.
/// </summary>
public sealed class TomlWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlWriter"/> class.
    /// </summary>
    public TomlWriter(TextWriter writer, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        Writer = writer;
        Options = options ?? TomlSerializerOptions.Default;
    }

    internal TextWriter Writer { get; }

    /// <summary>
    /// Gets the options instance used by this writer.
    /// </summary>
    public TomlSerializerOptions Options { get; }

    /// <summary>
    /// Writes the start of a TOML document.
    /// </summary>
    public void WriteStartDocument() => throw new NotImplementedException();

    /// <summary>
    /// Writes the end of a TOML document.
    /// </summary>
    public void WriteEndDocument() => throw new NotImplementedException();

    /// <summary>
    /// Writes the start of a table value.
    /// </summary>
    public void WriteStartTable() => throw new NotImplementedException();

    /// <summary>
    /// Writes the end of a table value.
    /// </summary>
    public void WriteEndTable() => throw new NotImplementedException();

    /// <summary>
    /// Writes a property name within the current table context.
    /// </summary>
    /// <param name="name">The property name.</param>
    public void WritePropertyName(string name)
    {
        ArgumentGuard.ThrowIfNull(name, nameof(name));
        throw new NotImplementedException();
    }

    /// <summary>
    /// Writes the start of an array value.
    /// </summary>
    public void WriteStartArray() => throw new NotImplementedException();

    /// <summary>
    /// Writes the end of an array value.
    /// </summary>
    public void WriteEndArray() => throw new NotImplementedException();

    /// <summary>
    /// Writes a string value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteStringValue(string value)
    {
        ArgumentGuard.ThrowIfNull(value, nameof(value));
        throw new NotImplementedException();
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteIntegerValue(long value) => throw new NotImplementedException();

    /// <summary>
    /// Writes a floating point value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteFloatValue(double value) => throw new NotImplementedException();

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteBooleanValue(bool value) => throw new NotImplementedException();

    /// <summary>
    /// Writes a TOML date/time value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteDateTimeValue(TomlDateTime value) => throw new NotImplementedException();
}
