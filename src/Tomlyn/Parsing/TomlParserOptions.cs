namespace Tomlyn.Parsing;

/// <summary>
/// Configures the behavior of <see cref="TomlParser"/>.
/// </summary>
public sealed record TomlParserOptions
{
    /// <summary>
    /// Gets or sets how the parser handles syntax errors.
    /// </summary>
    public TomlParserMode Mode { get; init; } = TomlParserMode.Strict;

    /// <summary>
    /// Gets or sets a value indicating whether the parser eagerly decodes scalar string literals.
    /// When <c>false</c> (default), string values are decoded on demand (for example via <see cref="TomlParser.GetString"/>).
    /// </summary>
    public bool DecodeScalars { get; init; }
}
