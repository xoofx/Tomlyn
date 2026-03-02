using System;
using Tomlyn.Model;

namespace Tomlyn.Serialization.Converters;

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

internal sealed class TomlUntypedObjectConverter : TomlConverter
{
    public static TomlUntypedObjectConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(object);

    public override object? Read(TomlReader reader, Type typeToConvert)
    {
        return reader.TokenType switch
        {
            TomlTokenType.StartTable => ReadTable(reader),
            TomlTokenType.StartArray => ReadArray(reader),
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

        switch (value)
        {
            case string s:
                writer.WriteStringValue(s);
                return;
            case bool b:
                writer.WriteBooleanValue(b);
                return;
            case long l:
                writer.WriteIntegerValue(l);
                return;
            case int i:
                writer.WriteIntegerValue(i);
                return;
            case double d:
                writer.WriteFloatValue(d);
                return;
            case float f:
                writer.WriteFloatValue(f);
                return;
            case TomlDateTime dt:
                writer.WriteDateTimeValue(dt);
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
            default:
                throw new TomlException($"Unsupported untyped TOML value `{value.GetType().FullName}`.");
        }
    }

    internal static object ReadValue(TomlReader reader)
    {
        return reader.TokenType switch
        {
            TomlTokenType.StartTable => ReadTable(reader),
            TomlTokenType.StartArray => ReadArray(reader),
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

        var table = new TomlTable();
        reader.Read();
        while (reader.TokenType != TomlTokenType.EndTable)
        {
            if (reader.TokenType != TomlTokenType.PropertyName)
            {
                throw reader.CreateException($"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.");
            }

            var name = reader.PropertyName!;
            reader.Read();
            table[name] = ReadValue(reader);
        }

        reader.Read();
        return table;
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
        writer.WriteStartTable();
        foreach (var pair in table)
        {
            writer.WritePropertyName(pair.Key);
            Instance.Write(writer, pair.Value);
        }

        writer.WriteEndTable();
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
        writer.WriteStartArray();
        foreach (var item in array)
        {
            WriteTable(writer, item);
        }

        writer.WriteEndArray();
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
