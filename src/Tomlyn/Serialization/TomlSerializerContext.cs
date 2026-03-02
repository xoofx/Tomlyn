using System;
using System.Collections.Generic;
using Tomlyn;
using Tomlyn.Helpers;
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

        var typeInfo = TomlBuiltInTypeInfoResolver.GetTypeInfo(typeof(T), options);
        if (typeInfo is TomlTypeInfo<T> typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"No built-in TOML metadata is available for type '{typeof(T).FullName}'.");
    }

    /// <summary>
    /// Creates metadata for a single-dimensional array type.
    /// </summary>
    protected static TomlTypeInfo<TElement[]> CreateArrayTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlArrayTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for a <see cref="List{T}"/>.
    /// </summary>
    protected static TomlTypeInfo<List<TElement>> CreateListTypeInfo<TElement>(TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlListTypeInfo<TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for an enumerable interface type backed by <see cref="List{T}"/>.
    /// </summary>
    protected static TomlTypeInfo<TEnumerable> CreateListBackedEnumerableTypeInfo<TEnumerable, TElement>(TomlSerializerContext context)
        where TEnumerable : IEnumerable<TElement>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlListBackedEnumerableTypeInfo<TEnumerable, TElement>(context.Options);
    }

    /// <summary>
    /// Creates metadata for a dictionary-like type with string keys.
    /// </summary>
    protected static TomlTypeInfo<TDictionary> CreateDictionaryTypeInfo<TDictionary, TValue>(TomlSerializerContext context)
        where TDictionary : IEnumerable<KeyValuePair<string, TValue>>
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return new TomlDictionaryTypeInfo<TDictionary, TValue>(context.Options);
    }
}
