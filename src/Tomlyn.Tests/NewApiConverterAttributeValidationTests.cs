using System;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiConverterAttributeValidationTests
{
    private sealed class NotAConverter
    {
    }

    [TomlConverter(typeof(NotAConverter))]
    private sealed class BadTypeConverterType
    {
        public int Value { get; set; }
    }

    [JsonConverter(typeof(NotAConverter))]
    private sealed class BadJsonConverterType
    {
        public int Value { get; set; }
    }

    private sealed class NoPublicCtorIntConverter : TomlConverter<int>
    {
        private NoPublicCtorIntConverter()
        {
        }

        public override int Read(TomlReader reader) => throw new NotSupportedException();

        public override void Write(TomlWriter writer, int value) => writer.WriteIntegerValue(value);
    }

    private sealed class BadMemberConverterType
    {
        [TomlConverter(typeof(NotAConverter))]
        public int Value { get; set; }
    }

    private sealed class BadMemberNoPublicCtor
    {
        [TomlConverter(typeof(NoPublicCtorIntConverter))]
        public int Value { get; set; }
    }

    [Test]
    public void TomlConverterAttribute_InvalidConverterType_ThrowsTomlException()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(new BadTypeConverterType()));
    }

    [Test]
    public void JsonConverterAttribute_InvalidConverterType_ThrowsTomlException()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(new BadJsonConverterType()));
    }

    [Test]
    public void TomlConverterAttribute_InvalidMemberConverterType_ThrowsTomlException()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(new BadMemberConverterType()));
    }

    [Test]
    public void TomlConverterAttribute_NoPublicParameterlessCtor_ThrowsTomlException()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(new BadMemberNoPublicCtor()));
    }
}

