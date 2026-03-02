using System;
using System.Collections.Generic;
using Tomlyn.Helpers;
using Tomlyn.Model;
using Tomlyn.Serialization.Converters;

namespace Tomlyn.Serialization.Internal;

internal static class TomlBuiltInTypeInfoResolver
{
    public static TomlTypeInfo? GetTypeInfo(Type type, TomlSerializerOptions options)
    {
        ArgumentGuard.ThrowIfNull(type, nameof(type));
        ArgumentGuard.ThrowIfNull(options, nameof(options));

        if (type == typeof(string)) return new BuiltInTomlTypeInfo<string>(options, TomlStringConverter.Instance);
        if (type == typeof(bool)) return new BuiltInTomlTypeInfo<bool>(options, TomlBooleanConverter.Instance);
        if (type == typeof(long)) return new BuiltInTomlTypeInfo<long>(options, TomlInt64Converter.Instance);
        if (type == typeof(double)) return new BuiltInTomlTypeInfo<double>(options, TomlDoubleConverter.Instance);
        if (type == typeof(TomlDateTime)) return new BuiltInTomlTypeInfo<TomlDateTime>(options, TomlTomlDateTimeConverter.Instance);

        if (type == typeof(TomlTable)) return new BuiltInTomlTypeInfo<TomlTable>(options, TomlTomlTableConverter.Instance);
        if (type == typeof(TomlArray)) return new BuiltInTomlTypeInfo<TomlArray>(options, TomlTomlArrayConverter.Instance);
        if (type == typeof(TomlTableArray)) return new BuiltInTomlTypeInfo<TomlTableArray>(options, TomlTomlTableArrayConverter.Instance);

        if (type == typeof(object)) return new BuiltInUntypedTomlTypeInfo(type, options, TomlUntypedObjectConverter.Instance);

        var collection = TryCreateCollectionTypeInfo(type, options);
        if (collection is not null)
        {
            return collection;
        }

        return null;
    }

    private static TomlTypeInfo? TryCreateCollectionTypeInfo(Type type, TomlSerializerOptions options)
    {
        if (type.IsArray && type.GetArrayRank() == 1)
        {
            var elementType = type.GetElementType();
            if (elementType is null)
            {
                return null;
            }

            var typeInfoType = typeof(TomlArrayTypeInfo<>).MakeGenericType(elementType);
            return (TomlTypeInfo?)Activator.CreateInstance(typeInfoType, options);
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var genericDefinition = type.GetGenericTypeDefinition();
        var args = type.GetGenericArguments();

        if (genericDefinition == typeof(List<>))
        {
            var typeInfoType = typeof(TomlListTypeInfo<>).MakeGenericType(args[0]);
            return (TomlTypeInfo?)Activator.CreateInstance(typeInfoType, options);
        }

        if (genericDefinition == typeof(Dictionary<,>))
        {
            if (args[0] != typeof(string))
            {
                return null;
            }

            var typeInfoType = typeof(TomlDictionaryTypeInfo<,>).MakeGenericType(type, args[1]);
            return (TomlTypeInfo?)Activator.CreateInstance(typeInfoType, options);
        }

        if (genericDefinition == typeof(IDictionary<,>) ||
            genericDefinition == typeof(IReadOnlyDictionary<,>))
        {
            if (args[0] != typeof(string))
            {
                return null;
            }

            var typeInfoType = typeof(TomlDictionaryTypeInfo<,>).MakeGenericType(type, args[1]);
            return (TomlTypeInfo?)Activator.CreateInstance(typeInfoType, options);
        }

        if (genericDefinition == typeof(IEnumerable<>) ||
            genericDefinition == typeof(ICollection<>) ||
            genericDefinition == typeof(IReadOnlyCollection<>) ||
            genericDefinition == typeof(IList<>) ||
            genericDefinition == typeof(IReadOnlyList<>))
        {
            var typeInfoType = typeof(TomlListBackedEnumerableTypeInfo<,>).MakeGenericType(type, args[0]);
            return (TomlTypeInfo?)Activator.CreateInstance(typeInfoType, options);
        }

        return null;
    }

    private sealed class BuiltInUntypedTomlTypeInfo : TomlTypeInfo
    {
        private readonly TomlConverter _converter;

        public BuiltInUntypedTomlTypeInfo(Type type, TomlSerializerOptions options, TomlConverter converter)
            : base(type, options)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public override void Write(TomlWriter writer, object? value)
        {
            ArgumentGuard.ThrowIfNull(writer, nameof(writer));
            _converter.Write(writer, value);
        }

        public override object? ReadAsObject(TomlReader reader)
        {
            ArgumentGuard.ThrowIfNull(reader, nameof(reader));
            return _converter.Read(reader, Type);
        }
    }

    private sealed class BuiltInTomlTypeInfo<T> : TomlTypeInfo<T>
    {
        private readonly TomlConverter<T> _converter;

        public BuiltInTomlTypeInfo(TomlSerializerOptions options, TomlConverter<T> converter)
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
}
