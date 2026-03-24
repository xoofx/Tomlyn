// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization.Internal;

internal sealed class TomlSourceGeneratedArrayTypeInfo<TElement> : TomlTypeInfo<TElement[]>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedArrayTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, TElement[] value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartArray();
        for (var i = 0; i < value.Length; i++)
        {
            WriteElement(writer, value[i]);
        }

        writer.WriteEndArray();
    }

    public override TElement[]? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var items = new PooledArrayBuilder<TElement>(initialCapacity: 16);
        try
        {
            reader.Read();
            while (reader.TokenType != TomlTokenType.EndArray)
            {
                items.Add(ReadElement(reader));
            }

            reader.Read();
            return items.ToArrayAndReturn();
        }
        finally
        {
            items.Dispose();
        }
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}

internal sealed class TomlSourceGeneratedListTypeInfo<TElement> : TomlTypeInfo<List<TElement>>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedListTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, List<TElement> value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartArray();
        for (var i = 0; i < value.Count; i++)
        {
            WriteElement(writer, value[i]);
        }

        writer.WriteEndArray();
    }

    public override List<TElement>? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var list = new List<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            list.Add(ReadElement(reader));
        }

        reader.Read();
        return list;
    }

    public override object? ReadInto(TomlReader reader, object? existingValue)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (existingValue is not List<TElement> list)
        {
            return Read(reader);
        }

        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            list.Add(ReadElement(reader));
        }

        reader.Read();
        return list;
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}

internal sealed class TomlSourceGeneratedListBackedEnumerableTypeInfo<TEnumerable, TElement> : TomlTypeInfo<TEnumerable>
    where TEnumerable : IEnumerable<TElement>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedListBackedEnumerableTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, TEnumerable value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartArray();
        foreach (var item in value)
        {
            WriteElement(writer, item);
        }

        writer.WriteEndArray();
    }

    public override TEnumerable? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var list = new List<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            list.Add(ReadElement(reader));
        }

        reader.Read();
        return (TEnumerable)(object)list;
    }

    public override object? ReadInto(TomlReader reader, object? existingValue)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (existingValue is not ICollection<TElement> collection)
        {
            return Read(reader);
        }

        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            collection.Add(ReadElement(reader));
        }

        reader.Read();
        return existingValue;
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}

internal sealed class TomlSourceGeneratedDictionaryTypeInfo<TDictionary, TValue> : TomlTypeInfo<TDictionary>
    where TDictionary : IEnumerable<KeyValuePair<string, TValue>>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _valueTypeInfo;
    private TomlTypeInfo<TValue>? _typedValueTypeInfo;

    public TomlSourceGeneratedDictionaryTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, TDictionary value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartTable();
        foreach (var pair in value)
        {
            var key = pair.Key;
            if (key is null)
            {
                throw new TomlException("Dictionary keys cannot be null.");
            }

            if (Options.DictionaryKeyPolicy is { } keyPolicy)
            {
                key = keyPolicy.ConvertName(key);
            }

            writer.WritePropertyName(key);
            WriteValue(writer, pair.Value);
        }

        writer.WriteEndTable();
    }

    public override TDictionary? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartTable)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.");
        }

        var dict = new Dictionary<string, TValue>(StringComparer.Ordinal);
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndTable)
        {
            if (reader.TokenType != TomlTokenType.PropertyName)
            {
                throw reader.CreateException($"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.");
            }

            var key = reader.PropertyName!;
            reader.Read();
            if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error &&
                dict.TryGetValue(key, out var existingValue))
            {
                if (TryReadTableHeaderExtension(reader, existingValue, out var mergedValue))
                {
                    dict[key] = mergedValue;
                    continue;
                }

                throw reader.CreateException($"Duplicate key '{key}' was encountered.");
            }

            if (TomlTableHeaderExtensionHelper.IsTableHeaderExtension(reader) && dict.TryGetValue(key, out existingValue))
            {
                if (TryReadTableHeaderExtension(reader, existingValue, out var mergedValue))
                {
                    dict[key] = mergedValue;
                    continue;
                }
            }

            dict[key] = ReadValue(reader);
        }

        reader.Read();
        return (TDictionary)(object)dict;
    }

    public override object? ReadInto(TomlReader reader, object? existingValue)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (existingValue is not IDictionary<string, TValue> dict)
        {
            return Read(reader);
        }

        if (reader.TokenType != TomlTokenType.StartTable)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.");
        }

        HashSet<string>? seen = null;
        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)
        {
            seen = new HashSet<string>(StringComparer.Ordinal);
        }

        reader.Read();
        while (reader.TokenType != TomlTokenType.EndTable)
        {
            if (reader.TokenType != TomlTokenType.PropertyName)
            {
                throw reader.CreateException($"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.");
            }

            var key = reader.PropertyName!;
            reader.Read();
            if (seen is not null && !seen.Add(key))
            {
                if (dict.TryGetValue(key, out var duplicateExistingValue) && TryReadTableHeaderExtension(reader, duplicateExistingValue, out var mergedValue))
                {
                    dict[key] = mergedValue;
                    continue;
                }

                throw reader.CreateException($"Duplicate key '{key}' was encountered.");
            }

            if (TomlTableHeaderExtensionHelper.IsTableHeaderExtension(reader) && dict.TryGetValue(key, out var currentValue))
            {
                if (TryReadTableHeaderExtension(reader, currentValue, out var mergedValue))
                {
                    dict[key] = mergedValue;
                    continue;
                }
            }

            dict[key] = ReadValue(reader);
        }

        reader.Read();
        return existingValue;
    }

    private void EnsureValueTypeInfo()
    {
        if (_valueTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TValue));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TValue), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TValue).FullName}' in the provided context.");
        }

        _valueTypeInfo = resolved;
        _typedValueTypeInfo = resolved as TomlTypeInfo<TValue>;
    }

    private void WriteValue(TomlWriter writer, TValue value)
    {
        EnsureValueTypeInfo();

        if (value is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedValueTypeInfo is not null)
        {
            _typedValueTypeInfo.Write(writer, value);
            return;
        }

        _valueTypeInfo!.Write(writer, value);
    }

    private TValue ReadValue(TomlReader reader)
    {
        EnsureValueTypeInfo();

        if (_typedValueTypeInfo is not null)
        {
            return _typedValueTypeInfo.Read(reader)!;
        }

        return (TValue)_valueTypeInfo!.ReadAsObject(reader)!;
    }

    private bool TryReadTableHeaderExtension(TomlReader reader, TValue existingValue, out TValue mergedValue)
    {
        EnsureValueTypeInfo();

        if (!TomlTableHeaderExtensionHelper.TryReadIntoExisting(reader, existingValue, _valueTypeInfo!, out var merged))
        {
            mergedValue = existingValue;
            return false;
        }

        mergedValue = (TValue)merged!;
        return true;
    }
}

internal sealed class TomlSourceGeneratedHashSetTypeInfo<TElement> : TomlTypeInfo<HashSet<TElement>>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedHashSetTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, HashSet<TElement> value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartArray();
        foreach (var element in value)
        {
            WriteElement(writer, element);
        }
        writer.WriteEndArray();
    }

    public override HashSet<TElement>? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var set = new HashSet<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            set.Add(ReadElement(reader));
        }

        reader.Read();
        return set;
    }

    public override object? ReadInto(TomlReader reader, object? existingValue)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (existingValue is not HashSet<TElement> set)
        {
            return Read(reader);
        }

        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            set.Add(ReadElement(reader));
        }

        reader.Read();
        return set;
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}

internal sealed class TomlSourceGeneratedHashSetBackedEnumerableTypeInfo<TEnumerable, TElement> : TomlTypeInfo<TEnumerable>
    where TEnumerable : IEnumerable<TElement>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedHashSetBackedEnumerableTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, TEnumerable value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartArray();
        foreach (var element in value)
        {
            WriteElement(writer, element);
        }
        writer.WriteEndArray();
    }

    public override TEnumerable? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var set = new HashSet<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            set.Add(ReadElement(reader));
        }

        reader.Read();
        return (TEnumerable)(object)set;
    }

    public override object? ReadInto(TomlReader reader, object? existingValue)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (existingValue is not ICollection<TElement> collection)
        {
            return Read(reader);
        }

        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            collection.Add(ReadElement(reader));
        }

        reader.Read();
        return existingValue;
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}

internal sealed class TomlSourceGeneratedImmutableArrayTypeInfo<TElement> : TomlTypeInfo<ImmutableArray<TElement>>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedImmutableArrayTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, ImmutableArray<TElement> value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));

        if (value.IsDefault)
        {
            value = ImmutableArray<TElement>.Empty;
        }

        writer.WriteStartArray();
        for (var i = 0; i < value.Length; i++)
        {
            WriteElement(writer, value[i]);
        }
        writer.WriteEndArray();
    }

    public override ImmutableArray<TElement> Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var builder = ImmutableArray.CreateBuilder<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            builder.Add(ReadElement(reader));
        }

        reader.Read();
        return builder.ToImmutable();
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}

internal sealed class TomlSourceGeneratedImmutableListTypeInfo<TElement> : TomlTypeInfo<ImmutableList<TElement>>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedImmutableListTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, ImmutableList<TElement> value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartArray();
        for (var i = 0; i < value.Count; i++)
        {
            WriteElement(writer, value[i]);
        }
        writer.WriteEndArray();
    }

    public override ImmutableList<TElement>? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var builder = ImmutableList.CreateBuilder<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            builder.Add(ReadElement(reader));
        }

        reader.Read();
        return builder.ToImmutable();
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}

internal sealed class TomlSourceGeneratedImmutableHashSetTypeInfo<TElement> : TomlTypeInfo<ImmutableHashSet<TElement>>
{
    private readonly TomlSerializerContext _context;
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlSourceGeneratedImmutableHashSetTypeInfo(TomlSerializerContext context)
        : base(context.Options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void Write(TomlWriter writer, ImmutableHashSet<TElement> value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(value, nameof(value));

        writer.WriteStartArray();
        foreach (var element in value)
        {
            WriteElement(writer, element);
        }
        writer.WriteEndArray();
    }

    public override ImmutableHashSet<TElement>? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var builder = ImmutableHashSet.CreateBuilder<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            builder.Add(ReadElement(reader));
        }

        reader.Read();
        return builder.ToImmutable();
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var fromConverters = TomlTypeInfoResolverPipeline.TryResolveFromConverters(Options, typeof(TElement));
        var resolved = fromConverters ?? _context.GetTypeInfo(typeof(TElement), Options);
        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No generated metadata is available for type '{typeof(TElement).FullName}' in the provided context.");
        }

        _elementTypeInfo = resolved;
        _typedElementTypeInfo = resolved as TomlTypeInfo<TElement>;
    }

    private void WriteElement(TomlWriter writer, TElement element)
    {
        EnsureElementTypeInfo();

        if (element is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        if (_typedElementTypeInfo is not null)
        {
            _typedElementTypeInfo.Write(writer, element);
            return;
        }

        _elementTypeInfo!.Write(writer, element);
    }

    private TElement ReadElement(TomlReader reader)
    {
        EnsureElementTypeInfo();

        if (_typedElementTypeInfo is not null)
        {
            return _typedElementTypeInfo.Read(reader)!;
        }

        return (TElement)_elementTypeInfo!.ReadAsObject(reader)!;
    }
}
