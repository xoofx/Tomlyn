using System;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization.Internal;

internal sealed class TomlConverterTypeInfo<T> : TomlTypeInfo<T>
{
    private readonly TomlConverter<T> _converter;

    public TomlConverterTypeInfo(TomlSerializerOptions options, TomlConverter<T> converter)
        : base(options)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public override void Write(TomlWriter writer, T value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        _converter.Write(writer, value);
    }

    public override T? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        return _converter.Read(reader);
    }
}

internal sealed class TomlUntypedConverterTypeInfo<T> : TomlTypeInfo<T>
{
    private readonly TomlConverter _converter;
    private readonly Type _typeToConvert;

    public TomlUntypedConverterTypeInfo(TomlSerializerOptions options, TomlConverter converter)
        : base(options)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _typeToConvert = typeof(T);
    }

    public override void Write(TomlWriter writer, T value)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        _converter.Write(writer, value);
    }

    public override T? Read(TomlReader reader)
    {
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));
        return (T)_converter.Read(reader, _typeToConvert)!;
    }
}

