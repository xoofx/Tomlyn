// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Globalization;
using Tomlyn.Model;
using Tomlyn.Text;

namespace Tomlyn.Helpers;

public class TomlFormatHelper
{
    public static string ToString(bool b) => b ? "true" : "false";

    public static string ToString(string s, TomlPropertyDisplayKind displayKind)
    {
        switch (displayKind)
        {
            case TomlPropertyDisplayKind.StringMulti:
                return $"\"\"\"{s.EscapeForToml(true)}\"\"\"";
            case TomlPropertyDisplayKind.StringLiteral:
                if (IsSafeStringLiteral(s))
                {
                    return $"'{s}'";
                }
                else
                {
                    goto default;
                }
            case TomlPropertyDisplayKind.StringLiteralMulti:
                if (IsSafeStringLiteralMulti(s))
                {
                    return $"'''{s}'''";
                }
                else
                {
                    goto default;
                }
            default:
                return $"\"{s.EscapeForToml()}\"";
        }
    }

    public static string ToString(int i32, TomlPropertyDisplayKind displayKind)
    {
        return displayKind switch
        {
            TomlPropertyDisplayKind.IntegerHexadecimal => $"0x{i32:x8}",
            TomlPropertyDisplayKind.IntegerOctal => $"0o{Convert.ToString(i32, 8)}",
            TomlPropertyDisplayKind.IntegerBinary => $"0b{Convert.ToString(i32, 2)}",
            _ => (i32.ToString(CultureInfo.InvariantCulture))
        };
    }

    public static string ToString(long i64, TomlPropertyDisplayKind displayKind)
    {
        return displayKind switch
        {
            TomlPropertyDisplayKind.IntegerHexadecimal => $"0x{i64:x16}",
            TomlPropertyDisplayKind.IntegerOctal => $"0o{Convert.ToString(i64, 8)}",
            TomlPropertyDisplayKind.IntegerBinary => $"0b{Convert.ToString(i64, 2)}",
            _ => i64.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static string ToString(uint u32, TomlPropertyDisplayKind displayKind)
    {
        return displayKind switch
        {
            TomlPropertyDisplayKind.IntegerHexadecimal => $"0x{u32:x8}",
            TomlPropertyDisplayKind.IntegerOctal => $"0o{Convert.ToString(u32, 8)}",
            TomlPropertyDisplayKind.IntegerBinary => $"0b{Convert.ToString(u32, 2)}",
            _ => u32.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static string ToString(ulong u64, TomlPropertyDisplayKind displayKind)
    {
        return ToString(unchecked((long) u64), displayKind);
    }

    public static string ToString(sbyte i8, TomlPropertyDisplayKind displayKind)
    {
        return displayKind switch
        {
            TomlPropertyDisplayKind.IntegerHexadecimal => $"0x{i8:x2}",
            TomlPropertyDisplayKind.IntegerOctal => $"0o{Convert.ToString(i8, 8)}",
            TomlPropertyDisplayKind.IntegerBinary => $"0b{Convert.ToString(i8, 2)}",
            _ => i8.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static string ToString(byte u8, TomlPropertyDisplayKind displayKind)
    {
        return displayKind switch
        {
            TomlPropertyDisplayKind.IntegerHexadecimal => $"0x{u8:x2}",
            TomlPropertyDisplayKind.IntegerOctal => $"0o{Convert.ToString(u8, 8)}",
            TomlPropertyDisplayKind.IntegerBinary => $"0b{Convert.ToString(u8, 2)}",
            _ => u8.ToString(CultureInfo.InvariantCulture)
        };
    }
    public static string ToString(short i16, TomlPropertyDisplayKind displayKind)
    {
        return displayKind switch
        {
            TomlPropertyDisplayKind.IntegerHexadecimal => $"0x{i16:x4}",
            TomlPropertyDisplayKind.IntegerOctal => $"0o{Convert.ToString(i16, 8)}",
            TomlPropertyDisplayKind.IntegerBinary => $"0b{Convert.ToString(i16, 2)}",
            _ => i16.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static string ToString(ushort u16, TomlPropertyDisplayKind displayKind)
    {
        return displayKind switch
        {
            TomlPropertyDisplayKind.IntegerHexadecimal => $"0x{u16:x4}",
            TomlPropertyDisplayKind.IntegerOctal => $"0o{Convert.ToString(u16, 8)}",
            TomlPropertyDisplayKind.IntegerBinary => $"0b{Convert.ToString(u16, 2)}",
            _ => u16.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static string ToString(float value)
    {
        if (float.IsNaN(value))
        {
            return "nan";
        }
        if (float.IsPositiveInfinity(value))
        {
            return "+inf";
        }
        if (float.IsNegativeInfinity(value))
        {
            return "-inf";
        }
        return AppendDecimalPoint(value.ToString("g9", CultureInfo.InvariantCulture));
    }

    public static string ToString(double value)
    {
        if (double.IsNaN(value))
        {
            return "nan";
        }
        if (double.IsPositiveInfinity(value))
        {
            return "+inf";
        }
        if (double.IsNegativeInfinity(value))
        {
            return "-inf";
        }
        return AppendDecimalPoint(value.ToString("g16", CultureInfo.InvariantCulture));
    }


    public static string ToString(TomlDateTime tomlDateTime) => tomlDateTime.ToString();


    public static string ToString(DateTime dateTime, TomlPropertyDisplayKind displayKind)
    {
        return new TomlDateTime(dateTime, 0,
            GetDateTimeDisplayKind(displayKind == TomlPropertyDisplayKind.Default
                ? TomlPropertyDisplayKind.LocalDateTime
                : displayKind)).ToString();
    }

    public static string ToString(DateTimeOffset dateTimeOffset, TomlPropertyDisplayKind displayKind)
    {
        return new TomlDateTime(dateTimeOffset, 0,
            GetDateTimeDisplayKind(displayKind == TomlPropertyDisplayKind.Default
                ? TomlPropertyDisplayKind.OffsetDateTimeByZ
                : displayKind)).ToString();
    }

    private static string AppendDecimalPoint(string text)
    {
        if (text == "0") return "0.0";
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            // Do not append a decimal point if floating point type value
            // - is in exponential form, or
            // - already has a decimal point
            if (c == 'e' || c == 'E' || c == '.')
            {
                return text;
            }
        }
        return text + ".0";
    }

    private static bool IsSafeStringLiteral(string text)
    {
        foreach (var c in text)
        {
            if (char.IsControl(c) || c == '\'') return false;
        }
        return true;
    }

    private static bool IsSafeStringLiteralMulti(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            // new line are permitted
            if ((c == '\r' && i + 1 < text.Length && text[i + 1] == 'n') || c == '\n')
            {
                continue;
            }
            if (char.IsControl(c)) return false;

            // Sequence of 3 or more ' are not permitted
            if (c == '\'')
            {
                if (i + 1 < text.Length && text[i + 1] == '\'' &&
                    i + 2 < text.Length && text[i + 2] == '\'')
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static TomlDateTimeKind GetDateTimeDisplayKind(TomlPropertyDisplayKind kind)
    {
        switch (kind)
        {
            default:
            case TomlPropertyDisplayKind.OffsetDateTimeByZ:
                return TomlDateTimeKind.OffsetDateTimeByZ;
            case TomlPropertyDisplayKind.OffsetDateTimeByNumber:
                return TomlDateTimeKind.OffsetDateTimeByNumber;
            case TomlPropertyDisplayKind.LocalDateTime:
                return TomlDateTimeKind.LocalDateTime;
            case TomlPropertyDisplayKind.LocalDate:
                return TomlDateTimeKind.LocalDate;
            case TomlPropertyDisplayKind.LocalTime:
                return TomlDateTimeKind.LocalTime;
        }
    }

}