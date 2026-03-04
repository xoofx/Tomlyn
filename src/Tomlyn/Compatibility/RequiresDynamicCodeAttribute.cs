// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

#if NETSTANDARD2_0
using System;

/// <summary>
/// Indicates that the annotated member requires dynamic code generation at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class RequiresDynamicCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresDynamicCodeAttribute"/> class.
    /// </summary>
    /// <param name="message">A message explaining the dynamic code requirement.</param>
    public RequiresDynamicCodeAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the message describing why dynamic code is required.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets an optional URL with more information.
    /// </summary>
    public string? Url { get; set; }
}
#endif

