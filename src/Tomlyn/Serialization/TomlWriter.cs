using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Helpers;
using Tomlyn.Model;

namespace Tomlyn.Serialization;

/// <summary>
/// Writes TOML tokens for use by <see cref="TomlConverter"/> implementations.
/// </summary>
public sealed class TomlWriter
{
    private readonly Stack<object> _stack;
    private object? _root;
    private string? _pendingPropertyName;
    private bool _documentStarted;
    private bool _documentEnded;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlWriter"/> class.
    /// </summary>
    public TomlWriter(TextWriter writer, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        Writer = writer;
        Options = options ?? TomlSerializerOptions.Default;
        _stack = new Stack<object>();
    }

    internal TextWriter Writer { get; }

    /// <summary>
    /// Gets the options instance used by this writer.
    /// </summary>
    public TomlSerializerOptions Options { get; }

    /// <summary>
    /// Writes the start of a TOML document.
    /// </summary>
    public void WriteStartDocument()
    {
        if (_documentStarted)
        {
            throw new InvalidOperationException("Document has already started.");
        }

        _documentStarted = true;
    }

    /// <summary>
    /// Writes the end of a TOML document.
    /// </summary>
    public void WriteEndDocument()
    {
        if (!_documentStarted)
        {
            throw new InvalidOperationException("Document has not started.");
        }

        if (_documentEnded)
        {
            throw new InvalidOperationException("Document has already ended.");
        }

        if (_stack.Count != 0)
        {
            throw new InvalidOperationException("Unbalanced container writes.");
        }

        if (_root is null)
        {
            throw new TomlException("No root value was written.");
        }

        if (_root is not TomlTable rootTable)
        {
            throw new TomlException($"The root value must be a TOML table, but was `{_root.GetType().FullName}`.");
        }

        var toml = Toml.FromModel(rootTable);
        Writer.Write(toml);
        _documentEnded = true;
    }

    /// <summary>
    /// Writes the start of a table value.
    /// </summary>
    public void WriteStartTable()
    {
        var table = new TomlTable();
        WriteValue(table);
        _stack.Push(table);
    }

    /// <summary>
    /// Writes the end of a table value.
    /// </summary>
    public void WriteEndTable()
    {
        if (_stack.Count == 0 || _stack.Peek() is not TomlTable)
        {
            throw new InvalidOperationException("No table to end.");
        }

        _stack.Pop();
    }

    /// <summary>
    /// Writes a property name within the current table context.
    /// </summary>
    /// <param name="name">The property name.</param>
    public void WritePropertyName(string name)
    {
        ArgumentGuard.ThrowIfNull(name, nameof(name));
        if (_stack.Count == 0 || _stack.Peek() is not TomlTable)
        {
            throw new InvalidOperationException("Property names can only be written inside a table.");
        }

        _pendingPropertyName = name;
    }

    /// <summary>
    /// Writes the start of an array value.
    /// </summary>
    public void WriteStartArray()
    {
        var array = new TomlArray();
        WriteValue(array);
        _stack.Push(array);
    }

    /// <summary>
    /// Writes the start of an array-of-tables value.
    /// </summary>
    /// <remarks>
    /// This method writes an array container whose elements must be tables.
    /// </remarks>
    public void WriteStartTableArray()
    {
        var array = new TomlTableArray();
        WriteValue(array);
        _stack.Push(array);
    }

    /// <summary>
    /// Writes the end of an array value.
    /// </summary>
    public void WriteEndArray()
    {
        if (_stack.Count == 0 || _stack.Peek() is not TomlArray)
        {
            throw new InvalidOperationException("No array to end.");
        }

        _stack.Pop();
    }

    /// <summary>
    /// Writes the end of an array-of-tables value.
    /// </summary>
    public void WriteEndTableArray()
    {
        if (_stack.Count == 0 || _stack.Peek() is not TomlTableArray)
        {
            throw new InvalidOperationException("No table array to end.");
        }

        _stack.Pop();
    }

    /// <summary>
    /// Writes a string value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteStringValue(string value)
    {
        ArgumentGuard.ThrowIfNull(value, nameof(value));
        WriteValue(value);
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteIntegerValue(long value)
    {
        WriteValue(value);
    }

    /// <summary>
    /// Writes a floating point value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteFloatValue(double value)
    {
        WriteValue(value);
    }

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteBooleanValue(bool value)
    {
        WriteValue(value);
    }

    /// <summary>
    /// Writes a TOML date/time value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteDateTimeValue(TomlDateTime value)
    {
        WriteValue(value);
    }

    private void WriteValue(object value)
    {
        if (!_documentStarted)
        {
            throw new InvalidOperationException("Document has not started.");
        }

        if (_documentEnded)
        {
            throw new InvalidOperationException("Document has already ended.");
        }

        if (_stack.Count == 0)
        {
            _root = value;
            return;
        }

        var parent = _stack.Peek();
        if (parent is TomlTable table)
        {
            if (_pendingPropertyName is null)
            {
                throw new InvalidOperationException("A property name must be written before writing a value into a table.");
            }

            WriteTableValue(table, _pendingPropertyName, value);
            _pendingPropertyName = null;
            return;
        }

        if (parent is TomlArray array)
        {
            array.Add(value);
            return;
        }

        if (parent is TomlTableArray tableArray)
        {
            if (value is not TomlTable childTable)
            {
                throw new TomlException($"Array-of-tables values must be tables, but encountered `{value.GetType().FullName}`.");
            }

            tableArray.Add(childTable);
            return;
        }

        throw new InvalidOperationException($"Unsupported container type `{parent.GetType().FullName}`.");
    }

    private void WriteTableValue(TomlTable table, string propertyName, object value)
    {
        if (Options.DottedKeyHandling != TomlDottedKeyHandling.Expand || propertyName.IndexOf('.') < 0)
        {
            table[propertyName] = value;
            return;
        }

        // Expand "a.b.c" into nested tables.
        var segments = propertyName.Split('.');
        if (segments.Length < 2)
        {
            table[propertyName] = value;
            return;
        }

        var current = table;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (segment.Length == 0)
            {
                table[propertyName] = value;
                return;
            }

            if (!current.TryGetValue(segment, out var existing) || existing is null)
            {
                var next = new TomlTable();
                current[segment] = next;
                current = next;
                continue;
            }

            if (existing is TomlTable nextTable)
            {
                current = nextTable;
                continue;
            }

            throw new TomlException($"Cannot expand dotted key '{propertyName}' because '{segment}' is already set to `{existing.GetType().FullName}`.");
        }

        var leaf = segments[segments.Length - 1];
        if (leaf.Length == 0)
        {
            table[propertyName] = value;
            return;
        }

        current[leaf] = value;
    }
}
