// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomlyn.Helpers;
using Tomlyn.Serialization;

namespace Tomlyn;

/// <summary>
/// Configures the behavior of <see cref="TomlSerializer"/> operations.
/// </summary>
public sealed record TomlSerializerOptions
{
    private static readonly TomlConverter[] EmptyConverters = [];
    private static readonly ReadOnlyCollection<TomlConverter> EmptyConvertersReadOnly = Array.AsReadOnly(EmptyConverters);

    /// <summary>
    /// Gets a default options instance.
    /// </summary>
    public static TomlSerializerOptions Default { get; } = new();

    private TomlConverter[] _converters = EmptyConverters;
    private ReadOnlyCollection<TomlConverter> _convertersReadOnly = EmptyConvertersReadOnly;

    /// <summary>
    /// Gets the custom converters.
    /// </summary>
    /// <remarks>
    /// Converters are evaluated in order and take precedence over built-in converters.
    /// </remarks>
    public IReadOnlyList<TomlConverter> Converters
    {
        get => _convertersReadOnly;
        init
        {
            ArgumentGuard.ThrowIfNull(value, nameof(value));
            if (value.Count == 0)
            {
                _converters = EmptyConverters;
                _convertersReadOnly = EmptyConvertersReadOnly;
                return;
            }

            var copy = new TomlConverter[value.Count];
            for (var i = 0; i < value.Count; i++)
            {
                var converter = value[i];
                if (converter is null)
                {
                    throw new ArgumentException("Converters cannot contain null entries.", nameof(value));
                }

                copy[i] = converter;
            }

            _converters = copy;
            _convertersReadOnly = Array.AsReadOnly(copy);
        }
    }

    /// <summary>
    /// Gets or sets a metadata resolver used to retrieve <see cref="TomlTypeInfo"/> instances.
    /// </summary>
    public ITomlTypeInfoResolver? TypeInfoResolver { get; init; }

    /// <summary>
    /// Gets or sets an optional name for the TOML source.
    /// </summary>
    public string? SourceName { get; init; }

    /// <summary>
    /// Gets or sets the policy used to convert CLR property names.
    /// </summary>
    public JsonNamingPolicy? PropertyNamingPolicy { get; init; }

    /// <summary>
    /// Gets or sets the policy used to convert dictionary keys during serialization.
    /// </summary>
    public JsonNamingPolicy? DictionaryKeyPolicy { get; init; }

    /// <summary>
    /// Gets or sets the preferred object creation handling when deserializing object and collection members.
    /// </summary>
    public JsonObjectCreationHandling PreferredObjectCreationHandling
    {
        get;
        init
        {
            if (value is not JsonObjectCreationHandling.Replace and not JsonObjectCreationHandling.Populate)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Preferred object creation handling must be Replace or Populate.");
            }

            field = value;
        }
    } = JsonObjectCreationHandling.Replace;

    /// <summary>
    /// Gets or sets a value indicating whether property name matching is case-insensitive.
    /// </summary>
    public bool PropertyNameCaseInsensitive { get; init; }

    /// <summary>
    /// Gets or sets the maximum depth allowed when reading or writing nested TOML containers.
    /// </summary>
    /// <remarks>
    /// A value of <c>0</c> uses the default maximum depth of 64, mirroring <c>System.Text.Json</c>.
    /// </remarks>
    public int MaxDepth
    {
        get;
        init
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Max depth must be greater than or equal to 0.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the default ignore condition for null/default values.
    /// </summary>
    public TomlIgnoreCondition DefaultIgnoreCondition { get; init; } = TomlIgnoreCondition.WhenWritingNull;

    /// <summary>
    /// Gets or sets behavior when duplicate keys are encountered while reading.
    /// </summary>
    public TomlDuplicateKeyHandling DuplicateKeyHandling { get; init; } = TomlDuplicateKeyHandling.Error;

    /// <summary>
    /// Gets or sets member ordering behavior for emitted mappings.
    /// </summary>
    public TomlMappingOrderPolicy MappingOrder { get; init; } = TomlMappingOrderPolicy.Declaration;

    /// <summary>
    /// Gets or sets behavior for dictionary keys containing '.'.
    /// </summary>
    public TomlDottedKeyHandling DottedKeyHandling { get; init; } = TomlDottedKeyHandling.Literal;

    /// <summary>
    /// Gets or sets polymorphism options.
    /// </summary>
    public TomlPolymorphismOptions PolymorphismOptions { get; init; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether output should be indented.
    /// </summary>
    public bool WriteIndented { get; init; } = true;

    /// <summary>
    /// Gets or sets the number of spaces to use when <see cref="WriteIndented"/> is enabled.
    /// </summary>
    public int IndentSize
    {
        get;
        init
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Indent size must be at least 1.");
            }

            field = value;
        }
    } = 2;

    /// <summary>
    /// Gets or sets the newline kind used by the writer.
    /// </summary>
    public TomlNewLineKind NewLine { get; init; } = TomlNewLineKind.Lf;

    /// <summary>
    /// Gets or sets behavior for scalar/array roots that do not naturally map to a TOML document.
    /// </summary>
    public TomlRootValueHandling RootValueHandling { get; init; } = TomlRootValueHandling.Error;

    /// <summary>
    /// Gets the root key name used when <see cref="RootValueHandling"/> is <see cref="TomlRootValueHandling.WrapInRootKey"/>.
    /// </summary>
    public string RootValueKeyName
    {
        get;
        init
        {
            ArgumentGuard.ThrowIfNull(value, nameof(value));
            if (!TomlKeyValidation.IsValidKeyName(value))
            {
                throw new ArgumentException("Root value key name must be non-empty and contain valid Unicode characters.", nameof(value));
            }

            field = value;
        }
    } = "value";

    /// <summary>
    /// Gets scalar string style preferences for serialization.
    /// </summary>
    public TomlStringStylePreferences StringStylePreferences { get; init; } = new();

    /// <summary>
    /// Gets or sets inline table emission policy.
    /// </summary>
    public TomlInlineTablePolicy InlineTablePolicy { get; init; } = TomlInlineTablePolicy.Never;

    /// <summary>
    /// Gets or sets array-of-table emission style.
    /// </summary>
    public TomlTableArrayStyle TableArrayStyle { get; init; } = TomlTableArrayStyle.Headers;

    /// <summary>
    /// Gets or sets an optional metadata store used to capture or apply TOML trivia/comment metadata.
    /// </summary>
    public ITomlMetadataStore? MetadataStore { get; init; }
}

/// <summary>
/// Specifies when a member should be ignored during TOML serialization.
/// </summary>
public enum TomlIgnoreCondition
{
    /// <summary>
    /// Never ignore a member because of default/null value.
    /// </summary>
    Never = 0,

    /// <summary>
    /// Ignore a member when writing and its value is <see langword="null"/>.
    /// </summary>
    WhenWritingNull = 1,

    /// <summary>
    /// Ignore a member when writing and its value is the type default.
    /// </summary>
    WhenWritingDefault = 2,
}

/// <summary>
/// Specifies behavior when duplicate keys are encountered.
/// </summary>
public enum TomlDuplicateKeyHandling
{
    /// <summary>
    /// Reject duplicate keys.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Keep the last value for a duplicate key.
    /// </summary>
    LastWins = 1,
}

/// <summary>
/// Specifies member ordering behavior for emitted mappings.
/// </summary>
public enum TomlMappingOrderPolicy
{
    /// <summary>
    /// Order members by declaration order.
    /// </summary>
    Declaration = 0,

    /// <summary>
    /// Order members alphabetically by serialized name.
    /// </summary>
    Alphabetical = 1,

    /// <summary>
    /// Order members by explicit order then declaration order.
    /// </summary>
    OrderThenDeclaration = 2,

    /// <summary>
    /// Order members by explicit order then alphabetical order.
    /// </summary>
    OrderThenAlphabetical = 3,
}

/// <summary>
/// Specifies behavior for dictionary keys containing '.'.
/// </summary>
public enum TomlDottedKeyHandling
{
    /// <summary>
    /// Treat keys containing '.' as a literal key name by quoting/escaping as needed.
    /// </summary>
    Literal = 0,

    /// <summary>
    /// Treat keys containing '.' as a dotted path and expand into subtables.
    /// </summary>
    Expand = 1,
}

/// <summary>
/// Specifies newline kind.
/// </summary>
public enum TomlNewLineKind
{
    /// <summary>
    /// Use LF (<c>\n</c>).
    /// </summary>
    Lf = 0,

    /// <summary>
    /// Use CRLF (<c>\r\n</c>).
    /// </summary>
    CrLf = 1,
}

/// <summary>
/// Specifies behavior for scalar/array roots that do not naturally map to a TOML document.
/// </summary>
public enum TomlRootValueHandling
{
    /// <summary>
    /// Throw when attempting to serialize/deserialize a non-table root.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Wrap the root value under <see cref="TomlSerializerOptions.RootValueKeyName"/>.
    /// </summary>
    WrapInRootKey = 1,
}

/// <summary>
/// Specifies inline table emission policy.
/// </summary>
public enum TomlInlineTablePolicy
{
    /// <summary>
    /// Never emit inline tables.
    /// </summary>
    Never = 0,

    /// <summary>
    /// Emit inline tables when a size heuristic considers it reasonable.
    /// </summary>
    WhenSmall = 1,

    /// <summary>
    /// Always emit inline tables when possible.
    /// </summary>
    Always = 2,
}

/// <summary>
/// Specifies how arrays of tables are emitted.
/// </summary>
public enum TomlTableArrayStyle
{
    /// <summary>
    /// Emit arrays of tables using headers (<c>[[a]]</c> style).
    /// </summary>
    Headers = 0,

    /// <summary>
    /// Emit arrays of tables as an inline array of inline tables (<c>a = [{...}, {...}]</c>).
    /// </summary>
    InlineArrayOfTables = 1,
}

/// <summary>
/// Specifies preferred default string style.
/// </summary>
public enum TomlStringStyle
{
    /// <summary>Basic string.</summary>
    Basic = 0,
    /// <summary>Literal string.</summary>
    Literal = 1,
    /// <summary>Multiline basic string.</summary>
    MultilineBasic = 2,
    /// <summary>Multiline literal string.</summary>
    MultilineLiteral = 3,
}

/// <summary>
/// Configures string scalar style preferences for TOML serialization.
/// </summary>
public sealed record TomlStringStylePreferences
{
    /// <summary>Preferred default string style.</summary>
    public TomlStringStyle DefaultStyle { get; init; } = TomlStringStyle.Basic;

    /// <summary>If true, prefer literal strings when no escaping is required.</summary>
    public bool PreferLiteralWhenNoEscapes { get; init; } = true;

    /// <summary>If true, allow emitting <c>\\xHH</c> escapes for control characters.</summary>
    public bool AllowHexEscapes { get; init; } = true;
}

/// <summary>
/// Specifies behavior when an unknown derived type discriminator is encountered.
/// </summary>
public enum TomlUnknownDerivedTypeHandling
{
    /// <summary>Use the serializer options default. This value is only valid on attribute properties and must not be used on <see cref="TomlPolymorphismOptions"/>.</summary>
    Unspecified = -1,
    /// <summary>Fail deserialization.</summary>
    Fail = 0,
    /// <summary>Fallback to the base type.</summary>
    FallBackToBaseType = 1,
}

/// <summary>
/// Configures discriminator-based polymorphism.
/// </summary>
public sealed record TomlPolymorphismOptions
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyList<TomlDerivedType>> EmptyDerivedTypeMappings =
        new ReadOnlyDictionary<Type, IReadOnlyList<TomlDerivedType>>(new Dictionary<Type, IReadOnlyList<TomlDerivedType>>());

    private TomlUnknownDerivedTypeHandling _unknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.Fail;
    private IReadOnlyDictionary<Type, IReadOnlyList<TomlDerivedType>> _derivedTypeMappings = EmptyDerivedTypeMappings;

    /// <summary>
    /// Gets the property name used for discriminator-based polymorphism.
    /// </summary>
    public string TypeDiscriminatorPropertyName { get; init; } = "$type";

    /// <summary>
    /// Gets behavior when an unknown discriminator is encountered.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when set to <see cref="TomlUnknownDerivedTypeHandling.Unspecified"/>.</exception>
    public TomlUnknownDerivedTypeHandling UnknownDerivedTypeHandling
    {
        get => _unknownDerivedTypeHandling;
        init
        {
            if (value == TomlUnknownDerivedTypeHandling.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(TomlUnknownDerivedTypeHandling.Unspecified)} is not a valid value for {nameof(TomlPolymorphismOptions)}.{nameof(UnknownDerivedTypeHandling)}.");
            }

            _unknownDerivedTypeHandling = value;
        }
    }

    /// <summary>
    /// Gets runtime-registered derived type mappings, keyed by polymorphic base type.
    /// </summary>
    /// <remarks>
    /// These mappings are used by reflection-based serialization and are merged with attribute-based registrations.
    /// Base-type attributes take precedence when the same derived type or discriminator is registered in both places.
    /// </remarks>
    public IReadOnlyDictionary<Type, IReadOnlyList<TomlDerivedType>> DerivedTypeMappings
    {
        get => _derivedTypeMappings;
        init
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Count == 0)
            {
                _derivedTypeMappings = EmptyDerivedTypeMappings;
                return;
            }

            var copy = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>(value.Count);
            foreach (var pair in value)
            {
                if (pair.Key is null)
                {
                    throw new ArgumentException("Derived type mapping base types cannot be null.", nameof(value));
                }

                if (pair.Value is null)
                {
                    throw new ArgumentException($"Derived type mappings for '{pair.Key}' cannot be null.", nameof(value));
                }

                var derivedTypes = new TomlDerivedType[pair.Value.Count];
                for (var i = 0; i < derivedTypes.Length; i++)
                {
                    var entry = pair.Value[i];
                    if (entry is null)
                    {
                        throw new ArgumentException($"Derived type mappings for '{pair.Key}' cannot contain null entries.", nameof(value));
                    }

                    derivedTypes[i] = entry;
                }

                copy[pair.Key] = Array.AsReadOnly(derivedTypes);
            }

            _derivedTypeMappings = new ReadOnlyDictionary<Type, IReadOnlyList<TomlDerivedType>>(copy);
        }
    }
}
