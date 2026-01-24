// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Tomlyn.Syntax;

namespace Tomlyn.Model;

/// <summary>
/// Stores metadata for TOML properties, including trivia and display hints.
/// </summary>
[DebuggerDisplay("{_properties}")]
public class TomlPropertiesMetadata
{
    private readonly Dictionary<string, TomlPropertyMetadata> _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPropertiesMetadata"/> class.
    /// </summary>
    public TomlPropertiesMetadata()
    {
        _properties = new Dictionary<string, TomlPropertyMetadata>();
    }

    /// <summary>
    /// Clears all property metadata.
    /// </summary>
    public void Clear()
    {
        _properties.Clear();
    }

    /// <summary>
    /// Checks whether metadata exists for the specified property key.
    /// </summary>
    /// <param name="propertyKey">The property key.</param>
    /// <returns><c>true</c> if metadata exists; otherwise <c>false</c>.</returns>
    public bool ContainsProperty(string propertyKey) => _properties.ContainsKey(propertyKey);

    /// <summary>
    /// Tries to get metadata for the specified property key.
    /// </summary>
    /// <param name="propertyKey">The property key.</param>
    /// <param name="propertyMetadata">The metadata when found.</param>
    /// <returns><c>true</c> if metadata exists; otherwise <c>false</c>.</returns>
    public bool TryGetProperty(string propertyKey, [NotNullWhen(true)] out TomlPropertyMetadata? propertyMetadata)
    {
        return _properties.TryGetValue(propertyKey, out propertyMetadata);
    }

    /// <summary>
    /// Sets metadata for the specified property key.
    /// </summary>
    /// <param name="propertyKey">The property key.</param>
    /// <param name="propertyMetadata">The metadata to set.</param>
    public void SetProperty(string propertyKey, TomlPropertyMetadata propertyMetadata)
    {
        _properties[propertyKey] = propertyMetadata;
    }
}

/// <summary>
/// Metadata describing a single TOML property.
/// </summary>
public class TomlPropertyMetadata
{
    /// <summary>
    /// Gets the leading trivia attached to this node. Might be null if no leading trivias.
    /// </summary>
    public List<TomlSyntaxTriviaMetadata>? LeadingTrivia { get; set; }

    /// <summary>
    /// Gets or sets the preferred display kind for this property.
    /// </summary>
    public TomlPropertyDisplayKind DisplayKind { get; set; }

    /// <summary>
    /// Gets the trailing trivia attached to this node. Might be null if no trailing trivias.
    /// </summary>
    public List<TomlSyntaxTriviaMetadata>? TrailingTrivia { get; set; }

    /// <summary>
    /// Gets the trailing trivia attached to this node. Might be null if no trailing trivias.
    /// </summary>
    public List<TomlSyntaxTriviaMetadata>? TrailingTriviaAfterEndOfLine { get; set; }

    /// <summary>
    /// Gets or sets the source span for the property.
    /// </summary>
    public SourceSpan Span {get; set;}
}

/// <summary>
/// Serializable metadata for a trivia element.
/// </summary>
public record struct TomlSyntaxTriviaMetadata(TokenKind Kind, string? Text)
{
    /// <summary>
    /// Converts a <see cref="SyntaxTrivia"/> into metadata.
    /// </summary>
    /// <param name="trivia">The trivia to convert.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator TomlSyntaxTriviaMetadata(SyntaxTrivia trivia)
    {
        return new TomlSyntaxTriviaMetadata(trivia.Kind, trivia.Text);
    }
}

/// <summary>
/// Display hints for TOML property formatting.
/// </summary>
public enum TomlPropertyDisplayKind
{
    /// <summary>Default display.</summary>
    Default = 0,

    /// <summary>Offset date-time with Z suffix.</summary>
    OffsetDateTimeByZ,
    /// <summary>Offset date-time with numeric offset.</summary>
    OffsetDateTimeByNumber,
    /// <summary>Local date-time.</summary>
    LocalDateTime,
    /// <summary>Local date.</summary>
    LocalDate,
    /// <summary>Local time.</summary>
    LocalTime,

    /// <summary>Hexadecimal integer.</summary>
    IntegerHexadecimal,

    /// <summary>Octal integer.</summary>
    IntegerOctal,

    /// <summary>Binary integer.</summary>
    IntegerBinary,

    /// <summary>Multi-line string.</summary>
    StringMulti,

    /// <summary>Literal string.</summary>
    StringLiteral,

    /// <summary>Multi-line literal string.</summary>
    StringLiteralMulti,

    /// <summary>Inline table.</summary>
    InlineTable,
    /// <summary>Force non-inline table formatting.</summary>
    NoInline
}