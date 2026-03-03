using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Tomlyn;
using Tomlyn.Helpers;
using Tomlyn.Model;
using Tomlyn.Serialization.Converters;
using Tomlyn.Serialization.Internal;

namespace Tomlyn.Serialization;

/// <summary>
/// Base type for source-generated TOML serializer contexts.
/// </summary>
public abstract partial class TomlSerializerContext : ITomlTypeInfoResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerContext"/> class.
    /// </summary>
    protected TomlSerializerContext()
        : this(new TomlSerializerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerContext"/> class.
    /// </summary>
    /// <param name="options">The options used by this context.</param>
    protected TomlSerializerContext(TomlSerializerOptions options)
    {
        ArgumentGuard.ThrowIfNull(options, nameof(options));

        if (options.TypeInfoResolver is null)
        {
            Options = options with { TypeInfoResolver = this };
            return;
        }

        if (!ReferenceEquals(options.TypeInfoResolver, this))
        {
            throw new ArgumentException(
                $"The provided {nameof(TomlSerializerOptions)} instance is associated with a different {nameof(TomlSerializerOptions.TypeInfoResolver)}. " +
                $"A {nameof(TomlSerializerContext)} must use an options instance whose {nameof(TomlSerializerOptions.TypeInfoResolver)} is the context itself.",
                nameof(options));
        }

        Options = options;
    }

    /// <summary>
    /// Gets the options instance associated with this context.
    /// </summary>
    public TomlSerializerOptions Options { get; }

    /// <inheritdoc />
    public abstract TomlTypeInfo? GetTypeInfo(Type type, TomlSerializerOptions options);

    /// <summary>
    /// Resolves built-in type metadata for a known scalar/container type.
    /// </summary>
    /// <remarks>
    /// This method exists to support source-generated contexts without requiring generated code to reference internal resolver types.
    /// </remarks>
    protected static TomlTypeInfo<T> GetBuiltInTypeInfo<T>(TomlSerializerOptions options)
    {
        ArgumentGuard.ThrowIfNull(options, nameof(options));

        var type = typeof(T);

        if (type == typeof(char)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<char>(options, TomlCharConverter.Instance);
        if (type == typeof(string)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<string>(options, TomlStringConverter.Instance);
        if (type == typeof(bool)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<bool>(options, TomlBooleanConverter.Instance);
        if (type == typeof(sbyte)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<sbyte>(options, TomlSByteConverter.Instance);
        if (type == typeof(byte)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<byte>(options, TomlByteConverter.Instance);
        if (type == typeof(short)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<short>(options, TomlInt16Converter.Instance);
        if (type == typeof(ushort)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<ushort>(options, TomlUInt16Converter.Instance);
        if (type == typeof(int)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<int>(options, TomlInt32Converter.Instance);
        if (type == typeof(uint)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<uint>(options, TomlUInt32Converter.Instance);
        if (type == typeof(long)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<long>(options, TomlInt64Converter.Instance);
        if (type == typeof(ulong)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<ulong>(options, TomlUInt64Converter.Instance);
        if (type == typeof(nint)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<nint>(options, TomlNIntConverter.Instance);
        if (type == typeof(nuint)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<nuint>(options, TomlNUIntConverter.Instance);
        if (type == typeof(float)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<float>(options, TomlSingleConverter.Instance);
        if (type == typeof(double)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<double>(options, TomlDoubleConverter.Instance);
        if (type == typeof(decimal)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<decimal>(options, TomlDecimalConverter.Instance);

#if NET5_0_OR_GREATER
        if (type == typeof(Half)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<Half>(options, TomlHalfConverter.Instance);
#endif

#if NET7_0_OR_GREATER
        if (type == typeof(Int128)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<Int128>(options, TomlInt128Converter.Instance);
        if (type == typeof(UInt128)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<UInt128>(options, TomlUInt128Converter.Instance);
#endif

        if (type == typeof(DateTime)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<DateTime>(options, TomlDateTimeConverter.Instance);
        if (type == typeof(DateTimeOffset)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<DateTimeOffset>(options, TomlDateTimeOffsetConverter.Instance);
        if (type == typeof(TomlDateTime)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<TomlDateTime>(options, TomlTomlDateTimeConverter.Instance);

#if NET6_0_OR_GREATER
        if (type == typeof(DateOnly)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<DateOnly>(options, TomlDateOnlyConverter.Instance);
        if (type == typeof(TimeOnly)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<TimeOnly>(options, TomlTimeOnlyConverter.Instance);
#endif

        if (type == typeof(Guid)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<Guid>(options, TomlGuidConverter.Instance);
        if (type == typeof(TimeSpan)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<TimeSpan>(options, TomlTimeSpanConverter.Instance);
        if (type == typeof(Uri)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<Uri>(options, TomlUriConverter.Instance);
        if (type == typeof(Version)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<Version>(options, TomlVersionConverter.Instance);

        if (type == typeof(TomlTable)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<TomlTable>(options, TomlTomlTableConverter.Instance);
        if (type == typeof(TomlArray)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<TomlArray>(options, TomlTomlArrayConverter.Instance);
        if (type == typeof(TomlTableArray)) return (TomlTypeInfo<T>)(object)new TomlConverterTypeInfo<TomlTableArray>(options, TomlTomlTableArrayConverter.Instance);

        if (type == typeof(object)) return (TomlTypeInfo<T>)(object)new TomlUntypedConverterTypeInfo<object>(options, TomlUntypedObjectConverter.Instance);
        if (type.IsEnum) return new TomlUntypedConverterTypeInfo<T>(options, TomlEnumConverter.Instance);

        throw new TomlException($"No built-in TOML metadata is available for type '{type.FullName}'.");
    }

    /// <summary>
    /// Creates metadata for a single-dimensional array type.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<TElement[]> CreateArrayTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlArrayTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for a single-dimensional array type using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<TElement[]> CreateSourceGeneratedArrayTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedArrayTypeInfo<TElement>(context);
    }

    /// <summary>
    /// Creates metadata for a <see cref="List{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<List<TElement>> CreateListTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlListTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for a <see cref="List{T}"/> using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<List<TElement>> CreateSourceGeneratedListTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedListTypeInfo<TElement>(context);
    }

    /// <summary>
    /// Creates metadata for a <see cref="HashSet{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<HashSet<TElement>> CreateHashSetTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlHashSetTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for a <see cref="HashSet{T}"/> using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<HashSet<TElement>> CreateSourceGeneratedHashSetTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedHashSetTypeInfo<TElement>(context);
    }

    /// <summary>
    /// Creates metadata for a set-like interface type backed by <see cref="HashSet{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<TEnumerable> CreateHashSetBackedEnumerableTypeInfo<TEnumerable, TElement>(TomlSerializerContext context)
        where TEnumerable : IEnumerable<TElement>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlHashSetBackedEnumerableTypeInfo<TEnumerable, TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for a set-like interface type backed by <see cref="HashSet{T}"/> using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<TEnumerable> CreateSourceGeneratedHashSetBackedEnumerableTypeInfo<TEnumerable, TElement>(TomlSerializerContext context)
        where TEnumerable : IEnumerable<TElement>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedHashSetBackedEnumerableTypeInfo<TEnumerable, TElement>(context);
    }

    /// <summary>
    /// Creates metadata for <see cref="ImmutableArray{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<ImmutableArray<TElement>> CreateImmutableArrayTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlImmutableArrayTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for <see cref="ImmutableArray{T}"/> using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<ImmutableArray<TElement>> CreateSourceGeneratedImmutableArrayTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedImmutableArrayTypeInfo<TElement>(context);
    }

    /// <summary>
    /// Creates metadata for <see cref="ImmutableList{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<ImmutableList<TElement>> CreateImmutableListTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlImmutableListTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for <see cref="ImmutableList{T}"/> using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<ImmutableList<TElement>> CreateSourceGeneratedImmutableListTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedImmutableListTypeInfo<TElement>(context);
    }

    /// <summary>
    /// Creates metadata for <see cref="ImmutableHashSet{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<ImmutableHashSet<TElement>> CreateImmutableHashSetTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlImmutableHashSetTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for <see cref="ImmutableHashSet{T}"/> using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<ImmutableHashSet<TElement>> CreateSourceGeneratedImmutableHashSetTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedImmutableHashSetTypeInfo<TElement>(context);
    }

    /// <summary>
    /// Creates metadata for an enumerable interface type backed by <see cref="List{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<TEnumerable> CreateListBackedEnumerableTypeInfo<TEnumerable, TElement>(TomlSerializerContext context)
        where TEnumerable : IEnumerable<TElement>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlListBackedEnumerableTypeInfo<TEnumerable, TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for an enumerable interface type backed by <see cref="List{T}"/> using source-generated resolution for nested elements.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<TEnumerable> CreateSourceGeneratedListBackedEnumerableTypeInfo<TEnumerable, TElement>(TomlSerializerContext context)
        where TEnumerable : IEnumerable<TElement>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedListBackedEnumerableTypeInfo<TEnumerable, TElement>(context);
    }

    /// <summary>
    /// Creates metadata for a dictionary-like type with string keys.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    [RequiresDynamicCode("Reflection-based TOML serialization is not compatible with trimming/NativeAOT. Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.")]
    protected static TomlTypeInfo<TDictionary> CreateDictionaryTypeInfo<TDictionary, TValue>(TomlSerializerContext context)
        where TDictionary : IEnumerable<KeyValuePair<string, TValue>>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlDictionaryTypeInfo<TDictionary, TValue>(context.Options);
    }

    /// <summary>
    /// Creates metadata for a dictionary-like type with string keys using source-generated resolution for nested values.
    /// </summary>
    /// <remarks>
    /// This method avoids reflection-based metadata resolution, making it compatible with trimming and NativeAOT.
    /// </remarks>
    protected static TomlTypeInfo<TDictionary> CreateSourceGeneratedDictionaryTypeInfo<TDictionary, TValue>(TomlSerializerContext context)
        where TDictionary : IEnumerable<KeyValuePair<string, TValue>>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlSourceGeneratedDictionaryTypeInfo<TDictionary, TValue>(context);
    }
}
