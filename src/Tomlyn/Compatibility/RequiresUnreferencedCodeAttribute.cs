// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

#if NETSTANDARD2_0
using System;

/// <summary>
/// Indicates that the annotated member requires unreferenced code and is incompatible with trimming.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresUnreferencedCodeAttribute"/> class.
    /// </summary>
    /// <param name="message">A message explaining the trimming requirement.</param>
    public RequiresUnreferencedCodeAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the message describing why unreferenced code is required.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets an optional URL with more information.
    /// </summary>
    public string? Url { get; set; }
}
#endif

