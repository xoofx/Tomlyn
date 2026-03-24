// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Tomlyn;
using Tomlyn.Helpers;
using Tomlyn.Model;
using Tomlyn.Serialization.Internal;

namespace Tomlyn.Serialization;

/// <summary>
/// Writes TOML tokens for use by <see cref="TomlConverter"/> implementations.
/// </summary>
public sealed class TomlWriter
{
    private readonly Stack<object> _stack;
    private readonly int _effectiveMaxDepth;
    private readonly TomlSerializationOperationState _operationState;
    private object? _root;
    private string? _pendingPropertyName;
    private bool _pendingPropertyNameIsLiteral;
    private bool _documentStarted;
    private bool _documentEnded;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlWriter"/> class.
    /// </summary>
    public TomlWriter(TextWriter writer, TomlSerializerOptions? options = null)
        : this(writer, options, operationState: null)
    {
    }

    internal TomlWriter(TextWriter writer, TomlSerializerOptions? options, TomlSerializationOperationState? operationState)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        Writer = writer;
        Options = options ?? TomlSerializerOptions.Default;
        _operationState = operationState ?? new TomlSerializationOperationState(Options);
        _stack = new Stack<object>();
        _effectiveMaxDepth = TomlDepthHelper.GetEffectiveMaxDepth(Options.MaxDepth);
    }

    internal TextWriter Writer { get; }

    internal object? RootValue => _root;

    internal TomlTable? CurrentTable => _stack.Count > 0 && _stack.Peek() is TomlTable table ? table : null;

    internal void TryAttachMetadata(object instance)
    {
        ArgumentGuard.ThrowIfNull(instance, nameof(instance));

        if (Options.MetadataStore is not { } store)
        {
            return;
        }

        if (!store.TryGetProperties(instance, out var metadata) || metadata is null)
        {
            return;
        }

        if (CurrentTable is { } table)
        {
            table.PropertiesMetadata = metadata;
        }
    }

    /// <summary>
    /// Gets the options instance used by this writer.
    /// </summary>
    public TomlSerializerOptions Options { get; }

    internal TomlSerializationOperationState OperationState => _operationState;

    [RequiresUnreferencedCode(TomlTypeInfoResolverPipeline.ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(TomlTypeInfoResolverPipeline.ReflectionBasedSerializationMessage)]
    internal TomlTypeInfo ResolveTypeInfo(Type type)
    {
        return _operationState.ResolveTypeInfo(type);
    }

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

        TomlModelTextWriter.WriteDocument(Writer, rootTable, Options);
        _documentEnded = true;
    }

    /// <summary>
    /// Writes the start of a table value.
    /// </summary>
    public void WriteStartTable()
    {
        ValidateContainerDepth();
        var inline = _stack.Count > 0 && _stack.Peek() is TomlArray;
        var table = new TomlTable(inline);
        WriteValue(table);
        _stack.Push(table);
    }

    /// <summary>
    /// Writes the start of an inline table value.
    /// </summary>
    public void WriteStartInlineTable()
    {
        ValidateContainerDepth();
        var table = new TomlTable(inline: true);
        WriteValue(table);
        _stack.Push(table);
    }

    /// <summary>
    /// Writes the end of an inline table value.
    /// </summary>
    public void WriteEndInlineTable()
    {
        if (_stack.Count == 0 || _stack.Peek() is not TomlTable { Kind: ObjectKind.InlineTable })
        {
            throw new InvalidOperationException("No inline table to end.");
        }

        _stack.Pop();
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
        WritePropertyNameCore(name, isLiteral: false);
    }

    internal void WritePropertyNameLiteral(string name)
    {
        WritePropertyNameCore(name, isLiteral: true);
    }

    private void WritePropertyNameCore(string name, bool isLiteral)
    {
        ArgumentGuard.ThrowIfNull(name, nameof(name));
        if (_stack.Count == 0 || _stack.Peek() is not TomlTable)
        {
            throw new InvalidOperationException("Property names can only be written inside a table.");
        }

        _pendingPropertyName = name;
        _pendingPropertyNameIsLiteral = isLiteral;
    }

    /// <summary>
    /// Writes the start of an array value.
    /// </summary>
    public void WriteStartArray()
    {
        ValidateContainerDepth();
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
        ValidateContainerDepth();
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

            WriteTableValue(table, _pendingPropertyName, value, _pendingPropertyNameIsLiteral);
            _pendingPropertyName = null;
            _pendingPropertyNameIsLiteral = false;
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

    private void WriteTableValue(TomlTable table, string propertyName, object value, bool isLiteralPropertyName)
    {
        if (isLiteralPropertyName || Options.DottedKeyHandling != TomlDottedKeyHandling.Expand || propertyName.IndexOf('.') < 0)
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

    private void ValidateContainerDepth()
    {
        var nextDepth = _stack.Count + 1;
        if (nextDepth > _effectiveMaxDepth)
        {
            TomlDepthHelper.ThrowDepthExceeded(_effectiveMaxDepth);
        }
    }
}
