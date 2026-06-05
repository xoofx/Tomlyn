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

    private sealed class UppercaseStringConverterWithoutAdvance : TomlConverter<string>
    {
        public override string? Read(TomlReader reader) => reader.GetString().ToUpperInvariant();

        public override void Write(TomlWriter writer, string value)
        {
            writer.WriteStringValue(value.ToLowerInvariant());
        }
    }

    private sealed class MemberLevelConverterModel
    {
        [JsonConverter(typeof(UppercaseStringConverter))]
        public string Name { get; set; } = "";
    }

    private sealed class MemberLevelTomlConverterWithoutAdvanceModel
    {
        [TomlConverter(typeof(UppercaseStringConverterWithoutAdvance))]
        public string Name { get; set; } = "";

        public string Title { get; set; } = "";
    }

    [TomlConverter(typeof(SpecialTypeConverter))]
    private sealed class SpecialType
    {
        public SpecialType(string value) => Value = value;

        public string Value { get; }
    }

    [TomlConverter(typeof(SpecialTypeConverterWithoutAdvance))]
    private sealed class SpecialTypeWithoutAdvance
    {
        public SpecialTypeWithoutAdvance(string value) => Value = value;

        public string Value { get; }
    }

    private sealed class SpecialTypeConverterWithoutAdvance : TomlConverter<SpecialTypeWithoutAdvance>
    {
        public override SpecialTypeWithoutAdvance? Read(TomlReader reader)
        {
            var value = reader.GetString();
            return new SpecialTypeWithoutAdvance(value);
        }

        public override void Write(TomlWriter writer, SpecialTypeWithoutAdvance value)
        {
            writer.WriteStringValue(value.Value);
        }
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

    private sealed class TypeLevelConverterWithoutAdvanceModel
    {
        public SpecialTypeWithoutAdvance Item { get; set; } = new SpecialTypeWithoutAdvance("x");

        public string Title { get; set; } = "";
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
    public void MemberLevelTomlConverter_ReadNeedNotAdvanceReader()
    {
        var roundtrip = TomlSerializer.Deserialize<MemberLevelTomlConverterWithoutAdvanceModel>("Name = 'Mr Poop'\nTitle = 'Sir'");

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("MR POOP"));
        Assert.That(roundtrip.Title, Is.EqualTo("Sir"));
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

    [Test]
    public void TypeLevelTomlConverter_ReadNeedNotAdvanceReader()
    {
        var roundtrip = TomlSerializer.Deserialize<TypeLevelConverterWithoutAdvanceModel>("Item = 'hello'\nTitle = 'Sir'");

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Item.Value, Is.EqualTo("hello"));
        Assert.That(roundtrip.Title, Is.EqualTo("Sir"));
    }
}

