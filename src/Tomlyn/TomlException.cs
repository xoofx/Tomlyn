using System;
using System.Text;
using Tomlyn.Syntax;

namespace Tomlyn;

/// <summary>
/// Exception thrown when TOML model mapping fails.
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
    }

    /// <summary>
    /// Gets the diagnostics associated with the exception.
    /// </summary>
    public DiagnosticsBag Diagnostics { get; }
}