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
    public sealed class TomlDateTime : TomlValue<DateTimeValue>, IConvertible
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
                    return Value.ToString(TomlPropertyDisplayKind.OffsetDateTime);
                case ObjectKind.LocalDateTime:
                    return Value.ToString(TomlPropertyDisplayKind.LocalDateTime);
                case ObjectKind.LocalDate:
                    return Value.ToString(TomlPropertyDisplayKind.LocalDate);
                case ObjectKind.LocalTime:
                    return Value.ToString(TomlPropertyDisplayKind.LocalTime);
            }
            // Return empty, should never happen as the constructor is protecting us
            return string.Empty;
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(DateTime))
            {
                return Value.DateTime;
            }

            if (conversionType == typeof(DateTimeOffset))
            {
                return Value.DateTime;
            }

            throw new InvalidCastException($"Unable to convert {nameof(TomlDateTime)} to destination type {conversionType.FullName}");
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
    }
}