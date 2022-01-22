using System;
using System.Reflection;
using Tomlyn.Syntax;

namespace Tomlyn.Model;

internal class TomlModelContext
{
    public TomlModelContext(TomlModelOptions options)
    {
        GetPropertyName = options.GetPropertyName;
        CreateInstance = options.CreateInstance;
        Diagnostics = new DiagnosticsBag();
    }

    public Func<PropertyInfo, string> GetPropertyName { get; set; }
    
    public Func<Type, object> CreateInstance { get; set; }

    public DiagnosticsBag Diagnostics { get; }
}