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

    /// <summary>
    /// Gets or sets a value indicating whether the parser captures comment trivia.
    /// </summary>
    /// <remarks>
    /// When enabled, the parser records comment trivia around property names and scalar values so that higher-level
    /// layers can populate <see cref="Tomlyn.Serialization.ITomlMetadataStore"/> instances.
    /// </remarks>
    public bool CaptureTrivia { get; init; }
}
