using System;
using System.Text;
using Tomlyn.Syntax;

namespace Tomlyn;

public class TomlException : Exception
{
    public TomlException(DiagnosticsBag diagnostics) : base(diagnostics.ToString())
    {
        Diagnostics = diagnostics;
    }

    public DiagnosticsBag Diagnostics { get; }
}