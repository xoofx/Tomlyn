using System.IO;
using System.Text;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiSerializerOverloadTests
{
    private const string SampleToml = """
        name = "Ada"
        age = 37
        """;

    [Test]
    public void Deserialize_String_WithContext_Works()
    {
        var context = TestTomlSerializerContext.Default;
        var person = TomlSerializer.Deserialize<GeneratedPerson>(SampleToml, context);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void Deserialize_String_Type_WithContext_Works()
    {
        var context = TestTomlSerializerContext.Default;
        var result = TomlSerializer.Deserialize(SampleToml, typeof(GeneratedPerson), context);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<GeneratedPerson>());
        var person = (GeneratedPerson)result!;
        Assert.That(person.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void Deserialize_Utf8Bytes_WithContext_Works()
    {
        var context = TestTomlSerializerContext.Default;
        var bytes = Encoding.UTF8.GetBytes(SampleToml);
        var person = TomlSerializer.Deserialize<GeneratedPerson>(bytes, context);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void Deserialize_TextReader_WithTypeInfo_Works()
    {
        var context = TestTomlSerializerContext.Default;
        using var reader = new StringReader(SampleToml);
        var person = TomlSerializer.Deserialize(reader, context.GeneratedPerson);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void Deserialize_TextReader_Type_WithContext_Works()
    {
        var context = TestTomlSerializerContext.Default;
        using var reader = new StringReader(SampleToml);
        var result = TomlSerializer.Deserialize(reader, typeof(GeneratedPerson), context);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<GeneratedPerson>());
    }

    [Test]
    public void Deserialize_Stream_WithTypeInfo_Works()
    {
        var context = TestTomlSerializerContext.Default;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SampleToml));
        var person = TomlSerializer.Deserialize(stream, context.GeneratedPerson);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void Serialize_TextWriter_WithContext_Works()
    {
        var context = TestTomlSerializerContext.Default;
        using var writer = new StringWriter();
        TomlSerializer.Serialize(writer, new GeneratedPerson { Name = "Ada", Age = 37 }, context);
        var toml = writer.ToString();

        var roundtrip = TomlSerializer.Deserialize(toml, context.GeneratedPerson);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("Ada"));
        Assert.That(roundtrip.Age, Is.EqualTo(37));
    }

    [Test]
    public void Serialize_Stream_WithContext_Works()
    {
        var context = TestTomlSerializerContext.Default;
        using var stream = new MemoryStream();
        TomlSerializer.Serialize(stream, new GeneratedPerson { Name = "Ada", Age = 37 }, context);
        stream.Position = 0;

        var roundtrip = TomlSerializer.Deserialize(stream, context.GeneratedPerson);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("Ada"));
        Assert.That(roundtrip.Age, Is.EqualTo(37));
    }

    [Test]
    public void TryDeserialize_String_WithContext_ReturnsFalseOnFailure()
    {
        var context = TestTomlSerializerContext.Default;
        var invalid = "name = \"Ada\"\nage = \"not-a-number\"\n";

        var ok = TomlSerializer.TryDeserialize<GeneratedPerson>(invalid, context, out var value);

        Assert.That(ok, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryDeserialize_Utf8Bytes_WithContext_ReturnsFalseOnFailure()
    {
        var context = TestTomlSerializerContext.Default;
        var bytes = Encoding.UTF8.GetBytes("name = \"Ada\"\nage = \"not-a-number\"\n");

        var ok = TomlSerializer.TryDeserialize<GeneratedPerson>(bytes, context, out var value);

        Assert.That(ok, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryDeserialize_TextReader_Type_WithContext_ReturnsFalseOnFailure()
    {
        var context = TestTomlSerializerContext.Default;
        using var reader = new StringReader("name = \"Ada\"\nage = \"not-a-number\"\n");

        var ok = TomlSerializer.TryDeserialize(reader, typeof(GeneratedPerson), context, out var value);

        Assert.That(ok, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryDeserialize_Stream_Type_WithContext_ReturnsFalseOnFailure()
    {
        var context = TestTomlSerializerContext.Default;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("name = \"Ada\"\nage = \"not-a-number\"\n"));

        var ok = TomlSerializer.TryDeserialize(stream, typeof(GeneratedPerson), context, out var value);

        Assert.That(ok, Is.False);
        Assert.That(value, Is.Null);
    }
}
