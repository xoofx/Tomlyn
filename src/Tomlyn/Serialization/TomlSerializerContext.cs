using System;
using Tomlyn;
using Tomlyn.Helpers;

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
}
