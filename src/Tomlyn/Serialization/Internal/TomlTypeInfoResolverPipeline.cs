using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Tomlyn.Helpers;
using Tomlyn.Serialization;

namespace Tomlyn.Serialization.Internal;

internal static class TomlTypeInfoResolverPipeline
{
    private static readonly ConditionalWeakTable<TomlSerializerOptions, ConcurrentDictionary<Type, TomlTypeInfo>> ReflectionCacheByOptions = new();

    public static TomlTypeInfo Resolve(TomlSerializerOptions options, Type type)
    {
        ArgumentGuard.ThrowIfNull(options, nameof(options));
        ArgumentGuard.ThrowIfNull(type, nameof(type));

        var fromTypeConverterAttribute = TryResolveFromConverterAttributes(options, type);
        if (fromTypeConverterAttribute is not null)
        {
            return fromTypeConverterAttribute;
        }

        var fromConverters = TryResolveFromConverters(options, type);
        if (fromConverters is not null)
        {
            return fromConverters;
        }

        var fromResolver = options.TypeInfoResolver?.GetTypeInfo(type, options);
        if (fromResolver is not null)
        {
            return TomlPolymorphicTypeInfo.TryWrap(fromResolver);
        }

        var builtIn = TomlBuiltInTypeInfoResolver.GetTypeInfo(type, options);
        if (builtIn is not null)
        {
            return TomlPolymorphicTypeInfo.TryWrap(builtIn);
        }

        if (TomlSerializerFeatureSwitches.IsReflectionEnabledByDefaultCalculated)
        {
            var typeInfo = ResolveFromReflection(options, type);
            return TomlPolymorphicTypeInfo.TryWrap(typeInfo);
        }

        var polymorphicDispatch = TomlPolymorphicTypeInfo.TryCreate(type, options, baseTypeInfo: null);
        if (polymorphicDispatch is not null)
        {
            return polymorphicDispatch;
        }

        throw new InvalidOperationException(
            $"No TOML metadata is available for type '{type.FullName}'. " +
            $"Provide {nameof(TomlSerializerOptions)}.{nameof(TomlSerializerOptions.TypeInfoResolver)} (source generation) or a custom resolver.");
    }

    private static TomlTypeInfo? TryResolveFromConverterAttributes(TomlSerializerOptions options, Type type)
    {
        var tomlConverterAttribute = type.GetCustomAttribute<TomlConverterAttribute>(inherit: true);
        if (tomlConverterAttribute is not null)
        {
            var converter = CreateConverterFromAttribute(tomlConverterAttribute.ConverterType, type, options);
            return new ConverterTomlTypeInfo(type, options, converter);
        }

        var jsonConverterAttribute = type.GetCustomAttribute<JsonConverterAttribute>(inherit: true);
        if (jsonConverterAttribute is not null && jsonConverterAttribute.ConverterType is not null)
        {
            var converter = CreateConverterFromAttribute(jsonConverterAttribute.ConverterType, type, options);
            return new ConverterTomlTypeInfo(type, options, converter);
        }

        return null;
    }

    private static TomlConverter CreateConverterFromAttribute(Type converterType, Type typeToConvert, TomlSerializerOptions options)
    {
        if (!typeof(TomlConverter).IsAssignableFrom(converterType))
        {
            throw new InvalidOperationException($"Converter type '{converterType.FullName}' must derive from '{typeof(TomlConverter).FullName}'.");
        }

        var converter = (TomlConverter?)Activator.CreateInstance(converterType);
        if (converter is null)
        {
            throw new InvalidOperationException($"Failed to create converter '{converterType.FullName}'.");
        }

        if (converter is TomlConverterFactory factory)
        {
            var created = factory.CreateConverter(typeToConvert, options);
            if (created is null)
            {
                throw new InvalidOperationException($"The converter factory '{factory.GetType().FullName}' returned null.");
            }

            if (created is TomlConverterFactory)
            {
                throw new InvalidOperationException($"The converter factory '{factory.GetType().FullName}' returned another {nameof(TomlConverterFactory)}.");
            }

            if (!created.CanConvert(typeToConvert))
            {
                throw new InvalidOperationException(
                    $"The converter factory '{factory.GetType().FullName}' returned a converter that cannot convert '{typeToConvert.FullName}'.");
            }

            converter = created;
        }

        if (!converter.CanConvert(typeToConvert))
        {
            throw new InvalidOperationException($"Converter '{converterType.FullName}' cannot convert '{typeToConvert.FullName}'.");
        }

        return converter;
    }

    private static TomlTypeInfo? TryResolveFromConverters(TomlSerializerOptions options, Type type)
    {
        var converters = options.Converters;
        if (converters.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < converters.Count; i++)
        {
            var converter = converters[i];
            if (!converter.CanConvert(type))
            {
                continue;
            }

            if (converter is TomlConverterFactory factory)
            {
                var created = factory.CreateConverter(type, options);
                if (created is null)
                {
                    throw new InvalidOperationException($"The converter factory '{factory.GetType().FullName}' returned null.");
                }

                if (created is TomlConverterFactory)
                {
                    throw new InvalidOperationException($"The converter factory '{factory.GetType().FullName}' returned another {nameof(TomlConverterFactory)}.");
                }

                if (!created.CanConvert(type))
                {
                    throw new InvalidOperationException(
                        $"The converter factory '{factory.GetType().FullName}' returned a converter that cannot convert '{type.FullName}'.");
                }

                converter = created;
            }

            return new ConverterTomlTypeInfo(type, options, converter);
        }

        return null;
    }

    private static TomlTypeInfo ResolveFromReflection(TomlSerializerOptions options, Type type)
    {
        var cache = ReflectionCacheByOptions.GetOrCreateValue(options);
        return cache.GetOrAdd(type, t =>
        {
            var typeInfo = TomlReflectionTypeInfoResolver.TryCreateTypeInfo(t, options);
            if (typeInfo is not null)
            {
                return typeInfo;
            }

            var dispatch = TomlPolymorphicTypeInfo.TryCreate(t, options, baseTypeInfo: null);
            if (dispatch is not null)
            {
                return dispatch;
            }

            throw new InvalidOperationException(
                $"No TOML metadata is available for type '{t.FullName}'. " +
                $"Reflection-based metadata could not be generated for this type. " +
                $"Provide {nameof(TomlSerializerOptions)}.{nameof(TomlSerializerOptions.TypeInfoResolver)} (source generation) or a custom resolver.");
        });
    }

    private sealed class ConverterTomlTypeInfo : TomlTypeInfo
    {
        private readonly TomlConverter _converter;

        public ConverterTomlTypeInfo(Type type, TomlSerializerOptions options, TomlConverter converter)
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
}
