using System;

namespace Tomlyn;

/// <summary>
/// Specifies the TOML key name for a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public sealed class TomlPropertyNameAttribute : Attribute
{
    public TomlPropertyNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
