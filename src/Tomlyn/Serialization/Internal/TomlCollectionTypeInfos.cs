using System;
using System.Collections.Generic;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization.Internal;

internal sealed class TomlArrayTypeInfo<TElement> : TomlTypeInfo<TElement[]>
{
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlArrayTypeInfo(TomlSerializerOptions options)
        : base(options)
    {
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

        var items = new List<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            items.Add(ReadElement(reader));
        }

        reader.Read();
        return items.ToArray();
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var resolved = TomlTypeInfoResolverPipeline.Resolve(Options, typeof(TElement));
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

internal sealed class TomlListTypeInfo<TElement> : TomlTypeInfo<List<TElement>>
{
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlListTypeInfo(TomlSerializerOptions options)
        : base(options)
    {
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

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var resolved = TomlTypeInfoResolverPipeline.Resolve(Options, typeof(TElement));
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

internal sealed class TomlListBackedEnumerableTypeInfo<TEnumerable, TElement> : TomlTypeInfo<TEnumerable>
    where TEnumerable : IEnumerable<TElement>
{
    private TomlTypeInfo? _elementTypeInfo;
    private TomlTypeInfo<TElement>? _typedElementTypeInfo;

    public TomlListBackedEnumerableTypeInfo(TomlSerializerOptions options)
        : base(options)
    {
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

        var list = new List<TElement>();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            list.Add(ReadElement(reader));
        }

        reader.Read();
        return (TEnumerable)(object)list;
    }

    private void EnsureElementTypeInfo()
    {
        if (_elementTypeInfo is not null)
        {
            return;
        }

        var resolved = TomlTypeInfoResolverPipeline.Resolve(Options, typeof(TElement));
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

internal sealed class TomlDictionaryTypeInfo<TDictionary, TValue> : TomlTypeInfo<TDictionary>
    where TDictionary : IEnumerable<KeyValuePair<string, TValue>>
{
    private TomlTypeInfo? _valueTypeInfo;
    private TomlTypeInfo<TValue>? _typedValueTypeInfo;

    public TomlDictionaryTypeInfo(TomlSerializerOptions options)
        : base(options)
    {
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
            if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error && dict.ContainsKey(key))
            {
                throw reader.CreateException($"Duplicate key '{key}' was encountered.");
            }

            reader.Read();
            dict[key] = ReadValue(reader);
        }

        reader.Read();
        return (TDictionary)(object)dict;
    }

    private void EnsureValueTypeInfo()
    {
        if (_valueTypeInfo is not null)
        {
            return;
        }

        var resolved = TomlTypeInfoResolverPipeline.Resolve(Options, typeof(TValue));
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
}

