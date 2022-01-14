// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Globalization;

namespace Tomlyn.Model
{
    /// <summary>
    /// Runtime representation of a TOML datetime
    /// </summary>
    public sealed class TomlDateTime : TomlValue<DateTimeValue>
    {
        public TomlDateTime(ObjectKind kind, DateTimeValue value) : base(CheckDateTimeKind(kind), value)
        {
        }

        private static ObjectKind CheckDateTimeKind(ObjectKind kind)
        {
            switch (kind)
            {
                case ObjectKind.OffsetDateTime:
                case ObjectKind.LocalDateTime:
                case ObjectKind.LocalDate:
                case ObjectKind.LocalTime:
                    return kind;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case ObjectKind.OffsetDateTime:
                    if (Value.OffsetKind == DateTimeValueOffsetKind.None || Value.OffsetKind == DateTimeValueOffsetKind.Zero)
                    {
                        var time = Value.DateTime.ToUniversalTime();
                        if (time.Millisecond == 0) return time.ToString($"yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture);
                        return time.ToString($"yyyy-MM-dd'T'HH:mm:ss.{GetFormatPrecision(Value.SecondPrecision)}Z", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        var time = Value.DateTime.ToLocalTime();
                        if (time.Millisecond == 0) return time.ToString($"yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                        return time.ToString($"yyyy-MM-dd'T'HH:mm:ss.{GetFormatPrecision(Value.SecondPrecision)}zzz", CultureInfo.InvariantCulture);
                    }
                case ObjectKind.LocalDateTime:
                    if (Value.DateTime.Millisecond == 0) return Value.DateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                    return Value.DateTime.ToString($"yyyy-MM-dd'T'HH:mm:ss.{GetFormatPrecision(Value.SecondPrecision)}", CultureInfo.InvariantCulture);
                case ObjectKind.LocalDate:
                    return Value.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case ObjectKind.LocalTime:
                    return Value.DateTime.Millisecond == 0 ? Value.DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture) : Value.DateTime.ToString($"HH:mm:ss.{GetFormatPrecision(Value.SecondPrecision)}", CultureInfo.InvariantCulture);
            }
            // Return empty, should never happen as the constructor is protecting us
            return string.Empty;
        }

        private string GetFormatPrecision(int precision)
        {
            switch (precision)
            {
                case 1: return "f";
                case 2: return "ff";
                case 3: return "fff";
                case 4: return "ffff";
                case 5: return "fffff";
                case 6: return "ffffff";
                case 7: return "fffffff";
                default:
                    return "fff";
            }
        }
    }
}