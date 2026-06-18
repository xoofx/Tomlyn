// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
/// Instructs the <see cref="Tomlyn.TomlSerializer"/> when to ignore the field or property value.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class TomlIgnoreAttribute : TomlAttribute
{
    /// <summary>
    /// Gets or sets the condition that must be met before the member is ignored.
    /// </summary>
    /// <remarks>The default value is <see cref="TomlIgnoreCondition.Always"/>.</remarks>
    public TomlIgnoreCondition Condition { get; set; } = TomlIgnoreCondition.Always;
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
/// Allows a collection member to be deserialized from either a single TOML value or a TOML array.
/// </summary>
/// <remarks>
/// When the TOML input contains a single value instead of an array, Tomlyn treats it as a collection containing
/// exactly one element. For mutable read-only collection members, values are appended to the existing collection.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlSingleOrArrayAttribute : TomlAttribute
{
}

/// <summary>
/// Overrides array-of-tables formatting for a collection member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlTableArrayStyleAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlTableArrayStyleAttribute"/> class.
    /// </summary>
    /// <param name="style">The array-of-tables style.</param>
    public TomlTableArrayStyleAttribute(TomlTableArrayStyle style)
    {
        if (style is not TomlTableArrayStyle.Headers and not TomlTableArrayStyle.InlineArrayOfTables)
        {
            throw new ArgumentOutOfRangeException(nameof(style), style, "Invalid TOML table array style.");
        }

        Style = style;
    }

    /// <summary>
    /// Gets the array-of-tables style.
    /// </summary>
    public TomlTableArrayStyle Style { get; }
}

/// <summary>
/// Overrides inline table formatting for a member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlInlineTableAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlInlineTableAttribute"/> class.
    /// </summary>
    /// <param name="policy">The inline table policy.</param>
    public TomlInlineTableAttribute(TomlInlineTablePolicy policy)
    {
        if (policy is not TomlInlineTablePolicy.Never and not TomlInlineTablePolicy.WhenSmall and not TomlInlineTablePolicy.Always)
        {
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Invalid TOML inline table policy.");
        }

        Policy = policy;
    }

    /// <summary>
    /// Gets the inline table policy.
    /// </summary>
    public TomlInlineTablePolicy Policy { get; }
}

/// <summary>
/// Represents an optional boolean formatting preference on an attribute.
/// </summary>
public enum TomlBooleanPreference
{
    /// <summary>
    /// Use the value from the broader serializer options scope.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The preference is enabled.
    /// </summary>
    True = 1,

    /// <summary>
    /// The preference is disabled.
    /// </summary>
    False = 2,
}

/// <summary>
/// Overrides string formatting for a string member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlStringStyleAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlStringStyleAttribute"/> class.
    /// </summary>
    /// <param name="style">The preferred string style.</param>
    public TomlStringStyleAttribute(TomlStringStyle style)
    {
        if (style is not TomlStringStyle.Basic and not TomlStringStyle.Literal and not TomlStringStyle.MultilineBasic and not TomlStringStyle.MultilineLiteral)
        {
            throw new ArgumentOutOfRangeException(nameof(style), style, "Invalid TOML string style.");
        }

        Style = style;
    }

    /// <summary>
    /// Gets the preferred string style.
    /// </summary>
    public TomlStringStyle Style { get; }

    /// <summary>
    /// Gets or sets whether literal strings should be preferred when no escaping is required.
    /// </summary>
    public TomlBooleanPreference PreferLiteralWhenNoEscapes { get; set; }

    /// <summary>
    /// Gets or sets whether hexadecimal escapes may be emitted for control characters.
    /// </summary>
    public TomlBooleanPreference AllowHexEscapes { get; set; }
}

/// <summary>
/// Overrides member ordering for a TOML-serializable type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class TomlMappingOrderAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlMappingOrderAttribute"/> class.
    /// </summary>
    /// <param name="policy">The mapping order policy.</param>
    public TomlMappingOrderAttribute(TomlMappingOrderPolicy policy)
    {
        if (policy is not TomlMappingOrderPolicy.Declaration and not TomlMappingOrderPolicy.Alphabetical and not TomlMappingOrderPolicy.OrderThenDeclaration and not TomlMappingOrderPolicy.OrderThenAlphabetical)
        {
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Invalid TOML mapping order policy.");
        }

        Policy = policy;
    }

    /// <summary>
    /// Gets the mapping order policy.
    /// </summary>
    public TomlMappingOrderPolicy Policy { get; }
}

/// <summary>
/// Overrides dotted-key handling for member names declared on a TOML-serializable type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class TomlDottedKeyHandlingAttribute : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDottedKeyHandlingAttribute"/> class.
    /// </summary>
    /// <param name="handling">The dotted-key handling behavior.</param>
    public TomlDottedKeyHandlingAttribute(TomlDottedKeyHandling handling)
    {
        if (handling is not TomlDottedKeyHandling.Literal and not TomlDottedKeyHandling.Expand)
        {
            throw new ArgumentOutOfRangeException(nameof(handling), handling, "Invalid TOML dotted key handling.");
        }

        Handling = handling;
    }

    /// <summary>
    /// Gets the dotted-key handling behavior.
    /// </summary>
    public TomlDottedKeyHandling Handling { get; }
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

    /// <summary>
    /// Gets or sets the behavior when an unknown discriminator is encountered.
    /// When set to a value other than <see cref="TomlUnknownDerivedTypeHandling.Unspecified"/>,
    /// this overrides the global <see cref="TomlPolymorphismOptions.UnknownDerivedTypeHandling"/>.
    /// </summary>
    public TomlUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; } = TomlUnknownDerivedTypeHandling.Unspecified;
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
