// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Tomlyn.Model;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Serialization.Converters;

internal sealed class TomlCharConverter : TomlConverter<char>
{
    public static TomlCharConverter Instance { get; } = new();

    public override char Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw reader.CreateException($"Expected {TomlTokenType.String} token but was {reader.TokenType}.");
        }

        var value = reader.GetString();
        if (value.Length != 1)
        {
            throw reader.CreateException("Expected a single character string.");
        }

        reader.Read();
        return value[0];
    }

    public override void Write(TomlWriter writer, char value)
    {
        writer.WriteStringValue(value.ToString());
    }
}

internal sealed class TomlStringConverter : TomlConverter<string>
{
    public static TomlStringConverter Instance { get; } = new();

    public override string? Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw reader.CreateException($"Expected {TomlTokenType.String} token but was {reader.TokenType}.");
        }

        var value = reader.GetString();
        reader.Read();
        return value;
    }

    public override void Write(TomlWriter writer, string value)
    {
        writer.WriteStringValue(value);
    }
}

internal sealed class TomlBooleanConverter : TomlConverter<bool>
{
    public static TomlBooleanConverter Instance { get; } = new();

    public override bool Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Boolean)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Boolean} token but was {reader.TokenType}.");
        }

        var value = reader.GetBoolean();
        reader.Read();
        return value;
    }

    public override void Write(TomlWriter writer, bool value)
    {
        writer.WriteBooleanValue(value);
    }
}

internal sealed class TomlInt64Converter : TomlConverter<long>
{
    public static TomlInt64Converter Instance { get; } = new();

    public override long Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var value = reader.GetInt64();
        reader.Read();
        return value;
    }

    public override void Write(TomlWriter writer, long value)
    {
        writer.WriteIntegerValue(value);
    }
}

internal sealed class TomlDoubleConverter : TomlConverter<double>
{
    public static TomlDoubleConverter Instance { get; } = new();

    public override double Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Float && reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Float} token but was {reader.TokenType}.");
        }

        var value = reader.GetDouble();
        reader.Read();
        return value;
    }

    public override void Write(TomlWriter writer, double value)
    {
        writer.WriteFloatValue(value);
    }
}

internal sealed class TomlTomlDateTimeConverter : TomlConverter<TomlDateTime>
{
    public static TomlTomlDateTimeConverter Instance { get; } = new();

    public override TomlDateTime Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.DateTime)
        {
            throw reader.CreateException($"Expected {TomlTokenType.DateTime} token but was {reader.TokenType}.");
        }

        var value = reader.GetTomlDateTime();
        reader.Read();
        return value;
    }

    public override void Write(TomlWriter writer, TomlDateTime value)
    {
        writer.WriteDateTimeValue(value);
    }
}

internal sealed class TomlSByteConverter : TomlConverter<sbyte>
{
    public static TomlSByteConverter Instance { get; } = new();

    public override sbyte Read(TomlReader reader)
    {
        var value = ReadCheckedIntegral(reader, sbyte.MinValue, sbyte.MaxValue);
        return (sbyte)value;
    }

    public override void Write(TomlWriter writer, sbyte value) => writer.WriteIntegerValue(value);

    private static long ReadCheckedIntegral(TomlReader reader, long minValue, long maxValue)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < minValue || raw > maxValue)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return raw;
    }
}

internal sealed class TomlByteConverter : TomlConverter<byte>
{
    public static TomlByteConverter Instance { get; } = new();

    public override byte Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < byte.MinValue || raw > byte.MaxValue)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return (byte)raw;
    }

    public override void Write(TomlWriter writer, byte value) => writer.WriteIntegerValue(value);
}

internal sealed class TomlInt16Converter : TomlConverter<short>
{
    public static TomlInt16Converter Instance { get; } = new();

    public override short Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < short.MinValue || raw > short.MaxValue)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return (short)raw;
    }

    public override void Write(TomlWriter writer, short value) => writer.WriteIntegerValue(value);
}

internal sealed class TomlUInt16Converter : TomlConverter<ushort>
{
    public static TomlUInt16Converter Instance { get; } = new();

    public override ushort Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < ushort.MinValue || raw > ushort.MaxValue)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return (ushort)raw;
    }

    public override void Write(TomlWriter writer, ushort value) => writer.WriteIntegerValue(value);
}

internal sealed class TomlInt32Converter : TomlConverter<int>
{
    public static TomlInt32Converter Instance { get; } = new();

    public override int Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < int.MinValue || raw > int.MaxValue)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return (int)raw;
    }

    public override void Write(TomlWriter writer, int value) => writer.WriteIntegerValue(value);
}

internal sealed class TomlUInt32Converter : TomlConverter<uint>
{
    public static TomlUInt32Converter Instance { get; } = new();

    public override uint Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < 0 || raw > uint.MaxValue)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return (uint)raw;
    }

    public override void Write(TomlWriter writer, uint value) => writer.WriteIntegerValue(unchecked((long)value));
}

internal sealed class TomlUInt64Converter : TomlConverter<ulong>
{
    public static TomlUInt64Converter Instance { get; } = new();

    public override ulong Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < 0)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return unchecked((ulong)raw);
    }

    public override void Write(TomlWriter writer, ulong value)
    {
        if (value > long.MaxValue)
        {
            throw new TomlException($"TOML integers are limited to signed 64-bit. Value {value} cannot be written.");
        }

        writer.WriteIntegerValue(unchecked((long)value));
    }
}

#if NET7_0_OR_GREATER
internal sealed class TomlInt128Converter : TomlConverter<Int128>
{
    public static TomlInt128Converter Instance { get; } = new();

    public override Int128 Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        reader.Read();
        return raw;
    }

    public override void Write(TomlWriter writer, Int128 value)
    {
        if (value < long.MinValue || value > long.MaxValue)
        {
            throw new TomlException($"TOML integers are limited to signed 64-bit. Value {value} cannot be written.");
        }

        writer.WriteIntegerValue((long)value);
    }
}

internal sealed class TomlUInt128Converter : TomlConverter<UInt128>
{
    public static TomlUInt128Converter Instance { get; } = new();

    public override UInt128 Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < 0)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return unchecked((ulong)raw);
    }

    public override void Write(TomlWriter writer, UInt128 value)
    {
        if (value > (UInt128)long.MaxValue)
        {
            throw new TomlException($"TOML integers are limited to signed 64-bit. Value {value} cannot be written.");
        }

        writer.WriteIntegerValue(unchecked((long)(ulong)value));
    }
}
#endif

internal sealed class TomlNIntConverter : TomlConverter<nint>
{
    public static TomlNIntConverter Instance { get; } = new();

    public override nint Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (IntPtr.Size == 4 && (raw < int.MinValue || raw > int.MaxValue))
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return unchecked((nint)raw);
    }

    public override void Write(TomlWriter writer, nint value) => writer.WriteIntegerValue(value);
}

internal sealed class TomlNUIntConverter : TomlConverter<nuint>
{
    public static TomlNUIntConverter Instance { get; } = new();

    public override nuint Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}.");
        }

        var raw = reader.GetInt64();
        if (raw < 0)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        if (IntPtr.Size == 4 && raw > uint.MaxValue)
        {
            throw reader.CreateException($"TOML integer value {raw} is out of range.");
        }

        reader.Read();
        return unchecked((nuint)raw);
    }

    public override void Write(TomlWriter writer, nuint value)
    {
        var asUlong = unchecked((ulong)value);
        if (asUlong > long.MaxValue)
        {
            throw new TomlException($"TOML integers are limited to signed 64-bit. Value {asUlong} cannot be written.");
        }

        writer.WriteIntegerValue(unchecked((long)asUlong));
    }
}

internal sealed class TomlSingleConverter : TomlConverter<float>
{
    public static TomlSingleConverter Instance { get; } = new();

    public override float Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Float && reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Float} token but was {reader.TokenType}.");
        }

        var raw = reader.GetDouble();
        if (!double.IsNaN(raw) && !double.IsInfinity(raw) && Math.Abs(raw) > float.MaxValue)
        {
            throw reader.CreateException($"TOML numeric value {raw} is out of range.");
        }

        reader.Read();
        return (float)raw;
    }

    public override void Write(TomlWriter writer, float value) => writer.WriteFloatValue(value);
}

#if NET5_0_OR_GREATER
internal sealed class TomlHalfConverter : TomlConverter<Half>
{
    public static TomlHalfConverter Instance { get; } = new();

    public override Half Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Float && reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Float} token but was {reader.TokenType}.");
        }

        var raw = reader.GetDouble();
        if (!double.IsNaN(raw) && !double.IsInfinity(raw) && Math.Abs(raw) > (double)Half.MaxValue)
        {
            throw reader.CreateException($"TOML numeric value {raw} is out of range.");
        }

        reader.Read();
        return (Half)raw;
    }

    public override void Write(TomlWriter writer, Half value) => writer.WriteFloatValue((double)value);
}
#endif

internal sealed class TomlDecimalConverter : TomlConverter<decimal>
{
    public static TomlDecimalConverter Instance { get; } = new();

    public override decimal Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.Float && reader.TokenType != TomlTokenType.Integer)
        {
            throw reader.CreateException($"Expected {TomlTokenType.Float} token but was {reader.TokenType}.");
        }

        if (reader.TokenType == TomlTokenType.Integer)
        {
            var integer = reader.GetInt64();
            reader.Read();
            return integer;
        }

        var raw = reader.GetDouble();
        if (double.IsNaN(raw) || double.IsInfinity(raw))
        {
            throw reader.CreateException($"TOML float literal `{reader.GetRawText()}` cannot be converted to decimal.");
        }

        try
        {
            var value = (decimal)raw;
            reader.Read();
            return value;
        }
        catch (OverflowException)
        {
            throw reader.CreateException($"TOML numeric value {raw} is out of range.");
        }
    }

    public override void Write(TomlWriter writer, decimal value)
    {
        writer.WriteFloatValue((double)value);
    }
}

internal sealed class TomlGuidConverter : TomlConverter<Guid>
{
    public static TomlGuidConverter Instance { get; } = new();

    public override Guid Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw reader.CreateException($"Expected {TomlTokenType.String} token but was {reader.TokenType}.");
        }

        var raw = reader.GetString();
        if (!Guid.TryParse(raw, out var value))
        {
            throw reader.CreateException($"Invalid GUID literal `{raw}`.");
        }

        reader.Read();
        return value;
    }

    public override void Write(TomlWriter writer, Guid value)
    {
        writer.WriteStringValue(value.ToString("D"));
    }
}

internal sealed class TomlTimeSpanConverter : TomlConverter<TimeSpan>
{
    public static TomlTimeSpanConverter Instance { get; } = new();

    public override TimeSpan Read(TomlReader reader)
    {
        if (reader.TokenType == TomlTokenType.DateTime)
        {
            var value = reader.GetTomlDateTime();
            if (value.Kind != TomlDateTimeKind.LocalTime)
            {
                throw reader.CreateException($"Expected TOML local-time but was {value.Kind}.");
            }

            var time = value.DateTime.TimeOfDay;
            reader.Read();
            return time;
        }

        throw reader.CreateException($"Expected {TomlTokenType.DateTime} token but was {reader.TokenType}.");
    }

    public override void Write(TomlWriter writer, TimeSpan value)
    {
        if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
        {
            throw new TomlException("TimeSpan values must be within a single day to be representable as TOML local-time.");
        }

        var dt = new DateTime(1, 1, 1, value.Hours, value.Minutes, value.Seconds, DateTimeKind.Unspecified).AddTicks(value.Ticks % TimeSpan.TicksPerSecond);
        var precision = 0;
        var fractionTicks = value.Ticks % TimeSpan.TicksPerSecond;
        if (fractionTicks != 0)
        {
            // Derive precision up to 7 digits from ticks (100ns).
            var fractional = (int)(fractionTicks * 10_000_000 / TimeSpan.TicksPerSecond);
            precision = 7;
            while (precision > 0 && fractional % 10 == 0)
            {
                fractional /= 10;
                precision--;
            }
        }

        var tomlTime = new TomlDateTime(new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), TimeSpan.Zero), precision, TomlDateTimeKind.LocalTime);
        writer.WriteDateTimeValue(tomlTime);
    }
}

internal sealed class TomlUriConverter : TomlConverter<Uri>
{
    public static TomlUriConverter Instance { get; } = new();

    public override Uri? Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw reader.CreateException($"Expected {TomlTokenType.String} token but was {reader.TokenType}.");
        }

        var raw = reader.GetString();
        if (!Uri.TryCreate(raw, UriKind.RelativeOrAbsolute, out var uri))
        {
            throw reader.CreateException($"Invalid URI literal `{raw}`.");
        }

        reader.Read();
        return uri;
    }

    public override void Write(TomlWriter writer, Uri value)
    {
        writer.WriteStringValue(value.ToString());
    }
}

internal sealed class TomlVersionConverter : TomlConverter<Version>
{
    public static TomlVersionConverter Instance { get; } = new();

    public override Version? Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw reader.CreateException($"Expected {TomlTokenType.String} token but was {reader.TokenType}.");
        }

        var raw = reader.GetString();

#if NET6_0_OR_GREATER
        if (!Version.TryParse(raw, out var version))
        {
            throw reader.CreateException($"Invalid version literal `{raw}`.");
        }

        reader.Read();
        return version;
#else
        try
        {
            var version = new Version(raw);
            reader.Read();
            return version;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        {
            throw reader.CreateException($"Invalid version literal `{raw}`.");
        }
#endif
    }

    public override void Write(TomlWriter writer, Version value)
    {
        writer.WriteStringValue(value.ToString());
    }
}

internal sealed class TomlDateTimeConverter : TomlConverter<DateTime>
{
    public static TomlDateTimeConverter Instance { get; } = new();

    public override DateTime Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.DateTime)
        {
            throw reader.CreateException($"Expected {TomlTokenType.DateTime} token but was {reader.TokenType}.");
        }

        var value = reader.GetTomlDateTime();
        var result = value.Kind switch
        {
            TomlDateTimeKind.OffsetDateTimeByNumber => value.DateTime.ToUniversalTime().UtcDateTime,
            TomlDateTimeKind.OffsetDateTimeByZ => value.DateTime.ToUniversalTime().UtcDateTime,
            TomlDateTimeKind.LocalDateTime => value.DateTime.DateTime,
            TomlDateTimeKind.LocalDate => value.DateTime.DateTime,
            TomlDateTimeKind.LocalTime => throw reader.CreateException("TOML local-time cannot be converted to DateTime without a date component."),
            _ => throw reader.CreateException($"Unsupported TOML datetime kind {value.Kind}."),
        };

        reader.Read();
        return result;
    }

    public override void Write(TomlWriter writer, DateTime value)
    {
        TomlDateTime toml;
        switch (value.Kind)
        {
            case DateTimeKind.Utc:
                toml = new TomlDateTime(new DateTimeOffset(value, TimeSpan.Zero), 0, TomlDateTimeKind.OffsetDateTimeByZ);
                break;
            case DateTimeKind.Local:
                toml = new TomlDateTime(new DateTimeOffset(value), 0, TomlDateTimeKind.OffsetDateTimeByNumber);
                break;
            default:
                toml = new TomlDateTime(value);
                break;
        }

        writer.WriteDateTimeValue(toml);
    }
}

internal sealed class TomlDateTimeOffsetConverter : TomlConverter<DateTimeOffset>
{
    public static TomlDateTimeOffsetConverter Instance { get; } = new();

    public override DateTimeOffset Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.DateTime)
        {
            throw reader.CreateException($"Expected {TomlTokenType.DateTime} token but was {reader.TokenType}.");
        }

        var value = reader.GetTomlDateTime();
        if (value.Kind == TomlDateTimeKind.LocalDateTime)
        {
            throw reader.CreateException("TOML local-date-time cannot be converted to DateTimeOffset by default.");
        }

        if (value.Kind is TomlDateTimeKind.LocalDate or TomlDateTimeKind.LocalTime)
        {
            throw reader.CreateException($"TOML {value.Kind} cannot be converted to DateTimeOffset.");
        }

        reader.Read();
        return value.DateTime;
    }

    public override void Write(TomlWriter writer, DateTimeOffset value)
    {
        var kind = value.Offset == TimeSpan.Zero ? TomlDateTimeKind.OffsetDateTimeByZ : TomlDateTimeKind.OffsetDateTimeByNumber;
        writer.WriteDateTimeValue(new TomlDateTime(value, 0, kind));
    }
}

#if NET6_0_OR_GREATER
internal sealed class TomlDateOnlyConverter : TomlConverter<DateOnly>
{
    public static TomlDateOnlyConverter Instance { get; } = new();

    public override DateOnly Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.DateTime)
        {
            throw reader.CreateException($"Expected {TomlTokenType.DateTime} token but was {reader.TokenType}.");
        }

        var value = reader.GetTomlDateTime();
        if (value.Kind != TomlDateTimeKind.LocalDate)
        {
            throw reader.CreateException($"Expected TOML local-date but was {value.Kind}.");
        }

        var dateOnly = DateOnly.FromDateTime(value.DateTime.DateTime);
        reader.Read();
        return dateOnly;
    }

    public override void Write(TomlWriter writer, DateOnly value)
    {
        writer.WriteDateTimeValue(new TomlDateTime(value.Year, value.Month, value.Day));
    }
}

internal sealed class TomlTimeOnlyConverter : TomlConverter<TimeOnly>
{
    public static TomlTimeOnlyConverter Instance { get; } = new();

    public override TimeOnly Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.DateTime)
        {
            throw reader.CreateException($"Expected {TomlTokenType.DateTime} token but was {reader.TokenType}.");
        }

        var value = reader.GetTomlDateTime();
        if (value.Kind != TomlDateTimeKind.LocalTime)
        {
            throw reader.CreateException($"Expected TOML local-time but was {value.Kind}.");
        }

        var timeOnly = TimeOnly.FromDateTime(value.DateTime.DateTime);
        reader.Read();
        return timeOnly;
    }

    public override void Write(TomlWriter writer, TimeOnly value)
    {
        var dt = new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, DateTimeKind.Unspecified).AddTicks(value.Ticks % TimeSpan.TicksPerSecond);
        var precision = 0;
        if (value.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            precision = 7;
        }

        writer.WriteDateTimeValue(new TomlDateTime(new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), TimeSpan.Zero), precision, TomlDateTimeKind.LocalTime));
    }
}
#endif

internal sealed class TomlEnumConverter : TomlConverter
{
    public static TomlEnumConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override object? Read(TomlReader reader, Type typeToConvert)
    {
        if (!typeToConvert.IsEnum)
        {
            throw new InvalidOperationException($"Type '{typeToConvert.FullName}' is not an enum.");
        }

        if (reader.TokenType == TomlTokenType.Integer)
        {
            var raw = reader.GetInt64();
            reader.Read();
            return Enum.ToObject(typeToConvert, raw);
        }

        if (reader.TokenType == TomlTokenType.String)
        {
            var name = reader.GetString();
            try
            {
                var parsed = Enum.Parse(typeToConvert, name, ignoreCase: true);
                reader.Read();
                return parsed;
            }
            catch (ArgumentException)
            {
                throw reader.CreateException($"Invalid enum name `{name}` for type '{typeToConvert.FullName}'.");
            }
        }

        throw reader.CreateException($"Expected {TomlTokenType.Integer} or {TomlTokenType.String} token but was {reader.TokenType}.");
    }

    public override void Write(TomlWriter writer, object? value)
    {
        if (value is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        var type = value.GetType();
        if (!type.IsEnum)
        {
            throw new TomlException($"Expected an enum value but was '{type.FullName}'.");
        }

        var underlying = Enum.GetUnderlyingType(type);
        try
        {
            if (underlying == typeof(ulong) || underlying == typeof(uint) || underlying == typeof(ushort) || underlying == typeof(byte) ||
                underlying == typeof(nuint))
            {
                var raw = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                if (raw > long.MaxValue)
                {
                    throw new TomlException($"TOML integers are limited to signed 64-bit. Value {raw} cannot be written.");
                }

                writer.WriteIntegerValue(unchecked((long)raw));
                return;
            }

            var signed = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            writer.WriteIntegerValue(signed);
        }
        catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
        {
            throw new TomlException($"Enum value '{value}' cannot be written as a TOML integer.", ex);
        }
    }
}

internal sealed class TomlUntypedObjectConverter : TomlConverter
{
    public static TomlUntypedObjectConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(object);

    public override object? Read(TomlReader reader, Type typeToConvert)
    {
        return reader.TokenType switch
        {
            TomlTokenType.StartTable => ReadTable(reader),
            TomlTokenType.StartArray => ReadArrayValue(reader),
            TomlTokenType.String => reader.GetString().AlsoAdvance(reader),
            TomlTokenType.Boolean => reader.GetBoolean().AlsoAdvance(reader),
            TomlTokenType.Integer => reader.GetInt64().AlsoAdvance(reader),
            TomlTokenType.Float => reader.GetDouble().AlsoAdvance(reader),
            TomlTokenType.DateTime => reader.GetTomlDateTime().AlsoAdvance(reader),
            _ => throw reader.CreateException($"Unexpected token {reader.TokenType} when reading object."),
        };
    }

    public override void Write(TomlWriter writer, object? value)
    {
        if (value is null)
        {
            throw new TomlException("TOML does not support null values.");
        }

        var options = writer.Options;
        var runtimeType = value.GetType();

        // Custom converters always win (mirrors serializer pipeline).
        var fromConverters = Tomlyn.Serialization.Internal.TomlTypeInfoResolverPipeline.TryResolveFromConverters(options, runtimeType);
        if (fromConverters is not null)
        {
            fromConverters.Write(writer, value);
            return;
        }

        var fromResolver = options.TypeInfoResolver?.GetTypeInfo(runtimeType, options);
        if (fromResolver is not null)
        {
            fromResolver.Write(writer, value);
            return;
        }

        switch (value)
        {
            case string s:
                writer.WriteStringValue(s);
                return;
            case char c:
                TomlCharConverter.Instance.Write(writer, c);
                return;
            case bool b:
                writer.WriteBooleanValue(b);
                return;
            case sbyte i8:
                TomlSByteConverter.Instance.Write(writer, i8);
                return;
            case byte u8:
                TomlByteConverter.Instance.Write(writer, u8);
                return;
            case short i16:
                TomlInt16Converter.Instance.Write(writer, i16);
                return;
            case ushort u16:
                TomlUInt16Converter.Instance.Write(writer, u16);
                return;
            case uint u32:
                TomlUInt32Converter.Instance.Write(writer, u32);
                return;
            case long l:
                writer.WriteIntegerValue(l);
                return;
            case int i:
                writer.WriteIntegerValue(i);
                return;
            case ulong u64:
                TomlUInt64Converter.Instance.Write(writer, u64);
                return;
            case nint ni:
                TomlNIntConverter.Instance.Write(writer, ni);
                return;
            case nuint nu:
                TomlNUIntConverter.Instance.Write(writer, nu);
                return;
            case double d:
                writer.WriteFloatValue(d);
                return;
            case float f:
                writer.WriteFloatValue(f);
                return;
            case decimal m:
                TomlDecimalConverter.Instance.Write(writer, m);
                return;
            case TomlDateTime dt:
                writer.WriteDateTimeValue(dt);
                return;
            case DateTime dt2:
                TomlDateTimeConverter.Instance.Write(writer, dt2);
                return;
            case DateTimeOffset dto:
                TomlDateTimeOffsetConverter.Instance.Write(writer, dto);
                return;
#if NET6_0_OR_GREATER
            case DateOnly dateOnly:
                TomlDateOnlyConverter.Instance.Write(writer, dateOnly);
                return;
            case TimeOnly timeOnly:
                TomlTimeOnlyConverter.Instance.Write(writer, timeOnly);
                return;
#endif
#if NET5_0_OR_GREATER
            case Half half:
                TomlHalfConverter.Instance.Write(writer, half);
                return;
#endif
#if NET7_0_OR_GREATER
            case Int128 i128:
                TomlInt128Converter.Instance.Write(writer, i128);
                return;
            case UInt128 u128:
                TomlUInt128Converter.Instance.Write(writer, u128);
                return;
#endif
            case Guid guid:
                TomlGuidConverter.Instance.Write(writer, guid);
                return;
            case TimeSpan ts:
                TomlTimeSpanConverter.Instance.Write(writer, ts);
                return;
            case Uri uri:
                TomlUriConverter.Instance.Write(writer, uri);
                return;
            case Version version:
                TomlVersionConverter.Instance.Write(writer, version);
                return;
            case TomlTable table:
                WriteTable(writer, table);
                return;
            case TomlArray array:
                WriteArray(writer, array);
                return;
            case TomlTableArray tableArray:
                WriteTableArray(writer, tableArray);
                return;
            case Enum:
                TomlEnumConverter.Instance.Write(writer, value);
                return;
            default:
                if (runtimeType == typeof(object))
                {
                    throw new TomlException("An untyped System.Object instance cannot be represented as TOML. Use a supported scalar/container type or a custom converter.");
                }

                throw new TomlException($"Unsupported untyped TOML value `{runtimeType.FullName}`.");
        }
    }

    internal static object ReadValue(TomlReader reader)
    {
        return reader.TokenType switch
        {
            TomlTokenType.StartTable => ReadTable(reader),
            TomlTokenType.StartArray => ReadArrayValue(reader),
            TomlTokenType.String => reader.GetString().AlsoAdvance(reader),
            TomlTokenType.Boolean => reader.GetBoolean().AlsoAdvance(reader),
            TomlTokenType.Integer => reader.GetInt64().AlsoAdvance(reader),
            TomlTokenType.Float => reader.GetDouble().AlsoAdvance(reader),
            TomlTokenType.DateTime => reader.GetTomlDateTime().AlsoAdvance(reader),
            _ => throw reader.CreateException($"Unexpected token {reader.TokenType} when reading untyped TOML value."),
        };
    }

    internal static TomlTable ReadTable(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.StartTable)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.");
        }

        var table = new TomlTable(inline: reader.CurrentSpan is not null);
        ReadTableInto(reader, table);
        return table;
    }

    internal static void ReadTableInto(TomlReader reader, TomlTable table)
    {
        if (reader.TokenType != TomlTokenType.StartTable)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.");
        }

        var store = reader.Options.MetadataStore;
        var hasStore = store is not null;
        TomlPropertiesMetadata? propertiesMetadata = null;
        var capturedAnyMetadata = false;
        if (hasStore)
        {
            if (store!.TryGetProperties(table, out var existingMetadata) && existingMetadata is not null)
            {
                propertiesMetadata = existingMetadata;
            }
            else
            {
                propertiesMetadata = new TomlPropertiesMetadata();
            }
        }

        reader.Read();
        while (reader.TokenType != TomlTokenType.EndTable)
        {
            if (reader.TokenType != TomlTokenType.PropertyName)
            {
                throw reader.CreateException($"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.");
            }

            var name = reader.PropertyName!;
            var nameSpan = reader.CurrentSpan;
            var leadingTrivia = reader.CurrentLeadingTrivia;
            reader.Read();
            if (propertiesMetadata is not null)
            {
                capturedAnyMetadata |= CapturePropertyMetadata(propertiesMetadata, name, nameSpan, leadingTrivia, reader.CurrentTrailingTrivia, GetDisplayKind(reader));
            }

            if (reader.TokenType == TomlTokenType.StartTable &&
                table.TryGetValue(name, out var existingValue) &&
                existingValue is TomlTable existingTable)
            {
                if (existingTable.Kind == ObjectKind.InlineTable)
                {
                    throw reader.CreateException($"Cannot extend inline table '{name}' with a non-inline table definition.");
                }

                ReadTableInto(reader, existingTable);
                continue;
            }

            table[name] = ReadValue(reader);
        }

        reader.Read();
        if (hasStore && propertiesMetadata is not null && capturedAnyMetadata)
        {
            store!.SetProperties(table, propertiesMetadata);
        }
    }

    private static bool CapturePropertyMetadata(
        TomlPropertiesMetadata propertiesMetadata,
        string name,
        TomlSourceSpan? span,
        TomlSyntaxTriviaMetadata[]? leadingTrivia,
        TomlSyntaxTriviaMetadata[]? trailingTrivia,
        TomlPropertyDisplayKind displayKind)
    {
        var hasLeading = leadingTrivia is { Length: > 0 };
        var hasTrailing = trailingTrivia is { Length: > 0 };
        if (span is null && !hasLeading && !hasTrailing && displayKind == TomlPropertyDisplayKind.Default)
        {
            return false;
        }

        var propertyMetadata = new TomlPropertyMetadata
        {
            DisplayKind = displayKind,
        };

        if (span is { } locatedSpan)
        {
            propertyMetadata.Span = new SourceSpan(
                locatedSpan.SourceName,
                new TextPosition(locatedSpan.Start.Offset, locatedSpan.Start.Line, locatedSpan.Start.Column),
                new TextPosition(locatedSpan.End.Offset, locatedSpan.End.Line, locatedSpan.End.Column));
        }

        if (hasLeading)
        {
            propertyMetadata.LeadingTrivia = new List<TomlSyntaxTriviaMetadata>(leadingTrivia!);
        }

        if (hasTrailing)
        {
            propertyMetadata.TrailingTrivia = new List<TomlSyntaxTriviaMetadata>(trailingTrivia!);
        }

        propertiesMetadata.SetProperty(name, propertyMetadata);
        return true;
    }

    private static TomlPropertyDisplayKind GetDisplayKind(TomlReader reader)
    {
        switch (reader.TokenType)
        {
            case TomlTokenType.Integer:
            {
                var raw = reader.GetRawText();
                if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return TomlPropertyDisplayKind.IntegerHexadecimal;
                if (raw.StartsWith("0o", StringComparison.OrdinalIgnoreCase)) return TomlPropertyDisplayKind.IntegerOctal;
                if (raw.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return TomlPropertyDisplayKind.IntegerBinary;
                return TomlPropertyDisplayKind.Default;
            }
            case TomlTokenType.String:
            {
                return reader.CurrentStringTokenKind switch
                {
                    TokenKind.StringMulti => TomlPropertyDisplayKind.StringMulti,
                    TokenKind.StringLiteral => TomlPropertyDisplayKind.StringLiteral,
                    TokenKind.StringLiteralMulti => TomlPropertyDisplayKind.StringLiteralMulti,
                    _ => TomlPropertyDisplayKind.Default,
                };
            }
            case TomlTokenType.DateTime:
            {
                var value = reader.GetTomlDateTime();
                return value.Kind switch
                {
                    TomlDateTimeKind.OffsetDateTimeByZ => TomlPropertyDisplayKind.OffsetDateTimeByZ,
                    TomlDateTimeKind.OffsetDateTimeByNumber => TomlPropertyDisplayKind.OffsetDateTimeByNumber,
                    TomlDateTimeKind.LocalDateTime => TomlPropertyDisplayKind.LocalDateTime,
                    TomlDateTimeKind.LocalDate => TomlPropertyDisplayKind.LocalDate,
                    TomlDateTimeKind.LocalTime => TomlPropertyDisplayKind.LocalTime,
                    _ => TomlPropertyDisplayKind.Default,
                };
            }
            case TomlTokenType.StartTable:
                return reader.CurrentSpan is not null ? TomlPropertyDisplayKind.InlineTable : TomlPropertyDisplayKind.Default;
            default:
                return TomlPropertyDisplayKind.Default;
        }
    }

    internal static object ReadArrayValue(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        // Prefer preserving arrays-of-tables as TomlTableArray to match TOML semantics and writer expectations.
        // Note that empty arrays can't be distinguished and are treated as TomlArray.
        reader.Read();
        if (reader.TokenType == TomlTokenType.StartTable && reader.CurrentSpan is null)
        {
            var tableArray = new TomlTableArray();
            while (reader.TokenType != TomlTokenType.EndArray)
            {
                if (reader.TokenType != TomlTokenType.StartTable)
                {
                    throw reader.CreateException($"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.");
                }

                tableArray.Add(ReadTable(reader));
            }

            reader.Read();
            return tableArray;
        }

        var array = new TomlArray();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            array.Add(ReadValue(reader));
        }

        reader.Read();
        return array;
    }

    internal static TomlArray ReadArray(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var array = new TomlArray();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            array.Add(ReadValue(reader));
        }

        reader.Read();
        return array;
    }

    private static void WriteTable(TomlWriter writer, TomlTable table)
    {
        if (table.Kind == ObjectKind.InlineTable)
        {
            writer.WriteStartInlineTable();
        }
        else
        {
            writer.WriteStartTable();
        }

        writer.TryAttachMetadata(table);
        if (table.PropertiesMetadata is { } metadata && writer.CurrentTable is { } current)
        {
            current.PropertiesMetadata = metadata;
        }

        foreach (var pair in table)
        {
            writer.WritePropertyName(pair.Key);
            Instance.Write(writer, pair.Value);
        }

        if (table.Kind == ObjectKind.InlineTable)
        {
            writer.WriteEndInlineTable();
        }
        else
        {
            writer.WriteEndTable();
        }
    }

    private static void WriteArray(TomlWriter writer, TomlArray array)
    {
        writer.WriteStartArray();
        foreach (var item in array)
        {
            Instance.Write(writer, item);
        }

        writer.WriteEndArray();
    }

    private static void WriteTableArray(TomlWriter writer, TomlTableArray array)
    {
        writer.WriteStartTableArray();
        foreach (var item in array)
        {
            WriteTable(writer, item);
        }

        writer.WriteEndTableArray();
    }
}

internal sealed class TomlTomlTableConverter : TomlConverter<TomlTable>
{
    public static TomlTomlTableConverter Instance { get; } = new();

    public override TomlTable? Read(TomlReader reader)
    {
        return TomlUntypedObjectConverter.ReadTable(reader);
    }

    public override void Write(TomlWriter writer, TomlTable value)
    {
        TomlUntypedObjectConverter.Instance.Write(writer, value);
    }
}

internal sealed class TomlTomlArrayConverter : TomlConverter<TomlArray>
{
    public static TomlTomlArrayConverter Instance { get; } = new();

    public override TomlArray? Read(TomlReader reader)
    {
        return TomlUntypedObjectConverter.ReadArray(reader);
    }

    public override void Write(TomlWriter writer, TomlArray value)
    {
        TomlUntypedObjectConverter.Instance.Write(writer, value);
    }
}

internal sealed class TomlTomlTableArrayConverter : TomlConverter<TomlTableArray>
{
    public static TomlTomlTableArrayConverter Instance { get; } = new();

    public override TomlTableArray? Read(TomlReader reader)
    {
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}.");
        }

        var array = new TomlTableArray();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndArray)
        {
            if (reader.TokenType != TomlTokenType.StartTable)
            {
                throw reader.CreateException($"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.");
            }

            array.Add(TomlUntypedObjectConverter.ReadTable(reader)!);
        }

        reader.Read();
        return array;
    }

    public override void Write(TomlWriter writer, TomlTableArray value)
    {
        TomlUntypedObjectConverter.Instance.Write(writer, value);
    }
}

internal static class TomlReaderExtensions
{
    public static T AlsoAdvance<T>(this T value, TomlReader reader)
    {
        reader.Read();
        return value;
    }
}
