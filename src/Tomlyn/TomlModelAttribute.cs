using System;

namespace Tomlyn;

/// <summary>
/// Marks a type to generate an AOT-friendly TOML model mapper.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class TomlModelAttribute : Attribute
{
}
