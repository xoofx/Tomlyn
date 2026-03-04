// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace Tomlyn;

/// <summary>
/// Specifies the TOML key name for a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public sealed class TomlPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPropertyNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The TOML key name.</param>
    public TomlPropertyNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the TOML key name.
    /// </summary>
    public string Name { get; }
}
