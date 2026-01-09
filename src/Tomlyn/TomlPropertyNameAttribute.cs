// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace Tomlyn;

/// <summary>
/// Specifies the TOML property name used during serialization/deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TomlPropertyNameAttribute : Attribute
{
    public TomlPropertyNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
