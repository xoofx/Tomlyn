// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using Tomlyn.Model;

namespace Tomlyn;
using System;

/// <summary>
/// A datetime value that can represent a TOML:
/// - An offset DateTime
/// - A local DateTime
/// - A local Date
/// - A local Time,
/// </summary>
/// <param name="DateTime"></param>
/// <param name="SecondPrecision"></param>
/// <param name="OffsetKind"></param>
public record struct DateTimeValue(DateTimeOffset DateTime, int SecondPrecision, DateTimeValueOffsetKind OffsetKind) 
{
    public DateTimeValue(int year, int month, int day) : this(new DateTimeOffset(new DateTime(year, month, day)), 0, DateTimeValueOffsetKind.None)
    {
    }

    public DateTimeValue(DateTime datetime) : this(new DateTimeOffset(datetime), 0, DateTimeValueOffsetKind.None)
    {
    }

    public string ToString(TomlPropertyDisplayKind kind)
    {
        switch (kind)
        {
            case TomlPropertyDisplayKind.LocalDateTime:
                if (this.DateTime.Millisecond == 0) return this.DateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                return this.DateTime.ToString($"yyyy-MM-dd'T'HH:mm:ss.{GetFormatPrecision(this.SecondPrecision)}", CultureInfo.InvariantCulture);
            case TomlPropertyDisplayKind.LocalDate:
                return this.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            case TomlPropertyDisplayKind.LocalTime:
                return this.DateTime.Millisecond == 0 ? this.DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture) : this.DateTime.ToString($"HH:mm:ss.{GetFormatPrecision(this.SecondPrecision)}", CultureInfo.InvariantCulture);
            case TomlPropertyDisplayKind.OffsetDateTime:
            default:
                if (this.OffsetKind == DateTimeValueOffsetKind.None || this.OffsetKind == DateTimeValueOffsetKind.Zero)
                {
                    var time = this.DateTime.ToUniversalTime();
                    if (time.Millisecond == 0) return time.ToString($"yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture);
                    return time.ToString($"yyyy-MM-dd'T'HH:mm:ss.{GetFormatPrecision(this.SecondPrecision)}Z", CultureInfo.InvariantCulture);
                }
                else
                {
                    var time = this.DateTime.ToLocalTime();
                    if (time.Millisecond == 0) return time.ToString($"yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                    return time.ToString($"yyyy-MM-dd'T'HH:mm:ss.{GetFormatPrecision(this.SecondPrecision)}zzz", CultureInfo.InvariantCulture);
                }
        }
    }
    public static string GetFormatPrecision(int precision)
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

    public static implicit operator DateTimeValue(DateTime dateTime)
    {
        return new DateTimeValue(dateTime, 0, DateTimeValueOffsetKind.None);
    }
}