// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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

    public static implicit operator DateTimeValue(DateTime dateTime)
    {
        return new DateTimeValue(dateTime, 0, DateTimeValueOffsetKind.None);
    }
}