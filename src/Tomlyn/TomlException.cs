using System;
using Tomlyn.Syntax;
using Tomlyn.Text;

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
    public TomlException(TomlSourceSpan span, string message) : this(span, message, innerException: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlException"/> class with a specific source location.
    /// </summary>
    public TomlException(TomlSourceSpan span, string message, Exception? innerException) : base(Format(span, message), innerException)
    {
        Span = span;
        Diagnostics = new DiagnosticsBag();
        Diagnostics.Error(ToLegacySpan(span), message);
    }

    /// <summary>
    /// Gets the diagnostics associated with the exception.
    /// </summary>
    public DiagnosticsBag Diagnostics { get; }

    /// <summary>
    /// Gets the optional source span associated with this exception.
    /// </summary>
    public TomlSourceSpan? Span { get; }

    /// <summary>
    /// Gets the 1-based line number associated with this exception, or 0 when unknown.
    /// </summary>
    public int Line => Span?.Start.Line + 1 ?? 0;

    /// <summary>
    /// Gets the 1-based column number associated with this exception, or 0 when unknown.
    /// </summary>
    public int Column => Span?.Start.Column + 1 ?? 0;

    /// <summary>
    /// Gets the 0-based offset associated with this exception, or 0 when unknown.
    /// </summary>
    /// <remarks>
    /// When parsing UTF-8 inputs, this offset represents a byte offset. When parsing string inputs, this offset represents a character offset.
    /// </remarks>
    public int Offset => Span?.Offset ?? 0;

    private static string Format(TomlSourceSpan span, string message)
    {
        return $"{span.ToStringSimple()} : error : {message}";
    }

    private static TomlSourceSpan? GetFirstSpanOrNull(DiagnosticsBag diagnostics)
    {
        if (diagnostics is null || diagnostics.Count == 0)
        {
            return null;
        }

        var span = diagnostics[0].Span;
        return new TomlSourceSpan(span.FileName, new TomlTextPosition(span.Start.Offset, span.Start.Line, span.Start.Column), new TomlTextPosition(span.End.Offset, span.End.Line, span.End.Column));
    }

    private static SourceSpan ToLegacySpan(TomlSourceSpan span)
        => new SourceSpan(span.SourceName, new TextPosition(span.Start.Offset, span.Start.Line, span.Start.Column), new TextPosition(span.End.Offset, span.End.Line, span.End.Column));
}
