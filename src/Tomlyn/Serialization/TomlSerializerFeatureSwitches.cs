using System;
using System.Diagnostics.CodeAnalysis;

namespace Tomlyn.Serialization;

internal static class TomlSerializerFeatureSwitches
{
    internal const string ReflectionSwitchName = "Tomlyn.TomlSerializer.IsReflectionEnabledByDefault";

    // This property is stubbed by ILLink.Substitutions.xml when the feature switch is disabled.
#if NET10_0_OR_GREATER
    [FeatureSwitchDefinition(ReflectionSwitchName)]
#endif
    public static bool IsReflectionEnabledByDefault
        => AppContext.TryGetSwitch(ReflectionSwitchName, out var enabled) ? enabled : true;

    public static readonly bool IsReflectionEnabledByDefaultCalculated = IsReflectionEnabledByDefault;
}

