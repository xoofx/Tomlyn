using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization.Internal;

internal static class TomlTypeInfoResolverPipeline
{
    private static readonly ConditionalWeakTable<TomlSerializerOptions, ConcurrentDictionary<Type, TomlTypeInfo>> ReflectionCacheByOptions = new();

    public static TomlTypeInfo Resolve(TomlSerializerOptions options, Type type)
    {
        ArgumentGuard.ThrowIfNull(options, nameof(options));
        ArgumentGuard.ThrowIfNull(type, nameof(type));

        var fromConverters = TryResolveFromConverters(options, type);
        if (fromConverters is not null)
        {
            return fromConverters;
        }

        var fromResolver = options.TypeInfoResolver?.GetTypeInfo(type, options);
        if (fromResolver is not null)
        {
            return fromResolver;
        }

        var builtIn = TomlBuiltInTypeInfoResolver.GetTypeInfo(type, options);
        if (builtIn is not null)
        {
            return builtIn;
        }

        if (TomlSerializerFeatureSwitches.IsReflectionEnabledByDefaultCalculated)
        {
            return ResolveFromReflection(options, type);
        }

        throw new InvalidOperationException(
            $"No TOML metadata is available for type '{type.FullName}'. " +
            $"Provide {nameof(TomlSerializerOptions)}.{nameof(TomlSerializerOptions.TypeInfoResolver)} (source generation) or a custom resolver.");
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
            return typeInfo ?? throw new InvalidOperationException(
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
