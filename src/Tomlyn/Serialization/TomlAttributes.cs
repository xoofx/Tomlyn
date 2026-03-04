using System;
using System.Diagnostics.CodeAnalysis;
using Tomlyn;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization;

/// <summary>
/// Specifies the serialized TOML property name for a member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlPropertyNameAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPropertyNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The serialized member name.</param>
    public TomlPropertyNameAttribute(string name)
    {
        ArgumentGuard.ThrowIfNull(name, nameof(name));
        if (name.Length == 0)
        {
            throw new ArgumentException("Property name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    /// <summary>
    /// Gets the serialized member name.
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// Instructs the <see cref="Tomlyn.TomlSerializer"/> not to serialize the field or property value.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class TomlIgnoreAttribute : TomlAttribute
{
    /// <summary>
    /// Gets or sets an optional ignore condition.
    /// </summary>
    public TomlIgnoreCondition Condition { get; set; } = TomlIgnoreCondition.Never;
}

/// <summary>
/// Includes a non-public member in TOML serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlIncludeAttribute : TomlAttribute
{
}

/// <summary>
/// Specifies which constructor should be used during TOML deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
public sealed class TomlConstructorAttribute : TomlAttribute
{
}

/// <summary>
/// Specifies that a member is required during deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlRequiredAttribute : TomlAttribute
{
}

/// <summary>
/// Specifies the emitted order for a serialized member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlPropertyOrderAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPropertyOrderAttribute"/> class.
    /// </summary>
    /// <param name="order">The order value.</param>
    public TomlPropertyOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Gets the order value.
    /// </summary>
    public int Order { get; }
}

/// <summary>
/// Indicates that a member should receive any unmapped TOML keys encountered during deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlExtensionDataAttribute : TomlAttribute
{
}

/// <summary>
/// Specifies a custom <see cref="TomlConverter"/> to use when serializing or deserializing a member or type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface |
                AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlConverterAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlConverterAttribute"/> class.
    /// </summary>
    /// <param name="converterType">The converter type.</param>
#if NETSTANDARD2_0
    public TomlConverterAttribute(Type converterType)
#else
    public TomlConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type converterType)
#endif
    {
        ArgumentGuard.ThrowIfNull(converterType, nameof(converterType));
        ConverterType = converterType;
    }

    /// <summary>
    /// Gets the converter type.
    /// </summary>
#if !NETSTANDARD2_0
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
    public Type ConverterType { get; }
}

/// <summary>
/// Marks a base type as polymorphic for TOML serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class TomlPolymorphicAttribute : TomlAttribute
{
    /// <summary>
    /// Gets or sets the discriminator property name.
    /// </summary>
    public string? TypeDiscriminatorPropertyName { get; set; }
}

/// <summary>
/// Registers a derived type for polymorphic TOML serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class TomlDerivedTypeAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDerivedTypeAttribute"/> class with a string discriminator.
    /// </summary>
    /// <param name="derivedType">The derived CLR type.</param>
    /// <param name="discriminator">The discriminator value.</param>
    public TomlDerivedTypeAttribute(Type derivedType, string discriminator)
    {
        ArgumentGuard.ThrowIfNull(derivedType, nameof(derivedType));
        ArgumentGuard.ThrowIfNull(discriminator, nameof(discriminator));

        DerivedType = derivedType;
        Discriminator = discriminator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDerivedTypeAttribute"/> class with an integer discriminator.
    /// </summary>
    /// <param name="derivedType">The derived CLR type.</param>
    /// <param name="discriminator">The integer discriminator value (converted to string via invariant culture).</param>
    public TomlDerivedTypeAttribute(Type derivedType, int discriminator)
    {
        ArgumentGuard.ThrowIfNull(derivedType, nameof(derivedType));

        DerivedType = derivedType;
        Discriminator = discriminator.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDerivedTypeAttribute"/> class as the default derived type (no discriminator).
    /// When a type is registered without a discriminator, it becomes the default type used when the discriminator property
    /// is missing or does not match any registered type.
    /// </summary>
    /// <param name="derivedType">The derived CLR type.</param>
    public TomlDerivedTypeAttribute(Type derivedType)
    {
        ArgumentGuard.ThrowIfNull(derivedType, nameof(derivedType));

        DerivedType = derivedType;
        Discriminator = null;
    }

    /// <summary>
    /// Gets the derived CLR type.
    /// </summary>
    public Type DerivedType { get; }

    /// <summary>
    /// Gets the discriminator value, or <see langword="null"/> if this is the default derived type.
    /// </summary>
    public string? Discriminator { get; }
}
