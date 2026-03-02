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
}

