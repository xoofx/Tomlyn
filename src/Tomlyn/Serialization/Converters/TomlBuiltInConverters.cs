using System;
using Tomlyn.Model;

namespace Tomlyn.Serialization.Converters;

internal static class TomlBuiltInConverters
{
    public static TomlConverter? GetConverter(Type type)
    {
        if (type == typeof(string)) return TomlStringConverter.Instance;
        if (type == typeof(bool)) return TomlBooleanConverter.Instance;
        if (type == typeof(long)) return TomlInt64Converter.Instance;
        if (type == typeof(double)) return TomlDoubleConverter.Instance;
        if (type == typeof(TomlDateTime)) return TomlTomlDateTimeConverter.Instance;
        if (type == typeof(object)) return TomlUntypedObjectConverter.Instance;

        if (type == typeof(TomlTable)) return TomlTomlTableConverter.Instance;
        if (type == typeof(TomlArray)) return TomlTomlArrayConverter.Instance;
        if (type == typeof(TomlTableArray)) return TomlTomlTableArrayConverter.Instance;

        return null;
    }
}

