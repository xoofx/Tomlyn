using System;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiConverterAttributeTests
{
    private sealed class UppercaseStringConverter : TomlConverter<string>
    {
        public override string? Read(TomlReader reader)
        {
            var value = reader.GetString();
            reader.Read();
            return value.ToUpperInvariant();
        }

        public override void Write(TomlWriter writer, string value)
        {
            writer.WriteStringValue(value.ToUpperInvariant());
        }
    }

    private sealed class MemberLevelConverterModel
    {
        [JsonConverter(typeof(UppercaseStringConverter))]
        public string Name { get; set; } = "";
    }

    [TomlConverter(typeof(SpecialTypeConverter))]
    private sealed class SpecialType
    {
        public SpecialType(string value) => Value = value;

        public string Value { get; }
    }

    private sealed class SpecialTypeConverter : TomlConverter<SpecialType>
    {
        public override SpecialType? Read(TomlReader reader)
        {
            var value = reader.GetString();
            reader.Read();
            return new SpecialType(value);
        }

        public override void Write(TomlWriter writer, SpecialType value)
        {
            writer.WriteStringValue(value.Value);
        }
    }

    private sealed class TypeLevelConverterModel
    {
        public SpecialType Item { get; set; } = new SpecialType("x");
    }

    [Test]
    public void MemberLevelJsonConverter_IsUsed()
    {
        var model = new MemberLevelConverterModel { Name = "ada" };
        var toml = TomlSerializer.Serialize(model);
        var roundtrip = TomlSerializer.Deserialize<MemberLevelConverterModel>(toml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("ADA"));
    }

    [Test]
    public void TypeLevelTomlConverter_IsUsed()
    {
        var model = new TypeLevelConverterModel { Item = new SpecialType("hello") };
        var toml = TomlSerializer.Serialize(model);
        var roundtrip = TomlSerializer.Deserialize<TypeLevelConverterModel>(toml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Item.Value, Is.EqualTo("hello"));
    }
}

