using System;
using Tomlyn.Syntax;

namespace Tomlyn;

/// <summary>
/// Exception thrown when parsing or serializing TOML fails.
/// </summary>
public class TomlException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlException"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostics that caused the exception.</param>
    public TomlException(DiagnosticsBag diagnostics) : base(diagnostics.ToString())
    {
        Diagnostics = diagnostics;
        Span = GetFirstSpanOrNull(diagnostics);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlException"/> class.
    /// </summary>
    public TomlException(string message) : base(message)
    {
        Diagnostics = new DiagnosticsBag();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlException"/> class.
    /// </summary>
    public TomlException(string message, Exception innerException) : base(message, innerException)
    {
        Diagnostics = new DiagnosticsBag();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlException"/> class with a specific source location.
    /// </summary>
    public TomlException(SourceSpan span, string message) : this(span, message, innerException: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlException"/> class with a specific source location.
    /// </summary>
    public TomlException(SourceSpan span, string message, Exception? innerException) : base(Format(span, message), innerException)
    {
        Span = span;
        Diagnostics = new DiagnosticsBag();
        Diagnostics.Error(span, message);
    }

    /// <summary>
    /// Gets the diagnostics associated with the exception.
    /// </summary>
    public DiagnosticsBag Diagnostics { get; }

    /// <summary>
    /// Gets the optional source span associated with this exception.
    /// </summary>
    public SourceSpan? Span { get; }

    /// <summary>
    /// Gets the 1-based line number associated with this exception, or 0 when unknown.
    /// </summary>
    public int Line => Span?.Start.Line + 1 ?? 0;

    /// <summary>
    /// Gets the 1-based column number associated with this exception, or 0 when unknown.
    /// </summary>
    public int Column => Span?.Start.Column + 1 ?? 0;

    private static string Format(SourceSpan span, string message)
    {
        return new DiagnosticMessage(DiagnosticMessageKind.Error, span, message).ToString();
    }

    private static SourceSpan? GetFirstSpanOrNull(DiagnosticsBag diagnostics)
    {
        if (diagnostics is null || diagnostics.Count == 0)
        {
            return null;
        }

        return diagnostics[0].Span;
    }
}
