using System;

namespace Tomlyn;

/// <summary>
/// Marks a root type to generate an AOT-friendly TOML model mapper.
/// </summary>
/// <remarks>
/// Nested model types referenced by the root type are discovered transitively and do not need to be annotated.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class TomlModelAttribute : Attribute
{
}
