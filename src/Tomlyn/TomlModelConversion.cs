using System;

namespace Tomlyn;

/// <summary>
/// Conversion helpers used by generated TOML mappers.
/// </summary>
public static class TomlModelConversion
{
    public static bool TryConvert(object? value, Type targetType, TomlModelOptions? options, out object? outputValue)
    {
        if (value is null || targetType.IsInstanceOfType(value))
        {
            outputValue = value;
            return true;
        }

        var unwrapped = Nullable.GetUnderlyingType(targetType);
        if (unwrapped != null)
        {
            return TryConvert(value, unwrapped, options, out outputValue);
        }

        try
        {
            if (typeof(Enum).IsAssignableFrom(targetType) && value is string text)
            {
                outputValue = Enum.Parse(targetType, text, true);
                return true;
            }

            if (value is IConvertible)
            {
                if (value.GetType().IsPrimitive && targetType.IsPrimitive)
                {
                    switch (value)
                    {
                        case sbyte i8:
                            if (targetType == typeof(byte))
                            {
                                outputValue = unchecked((byte)i8);
                                return true;
                            }
                            break;
                        case short i16:
                            if (targetType == typeof(ushort))
                            {
                                outputValue = unchecked((ushort)i16);
                                return true;
                            }
                            break;
                        case int i32:
                            if (targetType == typeof(uint))
                            {
                                outputValue = unchecked((uint)i32);
                                return true;
                            }
                            break;
                        case long i64:
                            if (targetType == typeof(ulong))
                            {
                                outputValue = unchecked((ulong)i64);
                                return true;
                            }
                            break;
                        case byte u8:
                            if (targetType == typeof(sbyte))
                            {
                                outputValue = unchecked((sbyte)u8);
                                return true;
                            }
                            break;
                        case ushort u16:
                            if (targetType == typeof(short))
                            {
                                outputValue = unchecked((short)u16);
                                return true;
                            }
                            break;
                        case uint u32:
                            if (targetType == typeof(int))
                            {
                                outputValue = unchecked((int)u32);
                                return true;
                            }
                            break;
                        case ulong u64:
                            if (targetType == typeof(long))
                            {
                                outputValue = unchecked((long)u64);
                                return true;
                            }
                            break;
                    }
                }

                try
                {
                    outputValue = Convert.ChangeType(value, targetType);
                    return true;
                }
                catch (Exception) when (options?.ConvertToModel is not null)
                {
                    // ignore and fall through
                }
            }

            if (options?.ConvertToModel is not null)
            {
                var convertedValue = options.ConvertToModel(value, targetType);
                outputValue = convertedValue;
                if (convertedValue is not null)
                {
                    return true;
                }
            }
        }
        catch
        {
            // ignore
        }

        outputValue = null;
        return false;
    }
}
