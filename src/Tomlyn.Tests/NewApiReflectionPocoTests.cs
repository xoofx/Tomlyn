using System.Text.Json.Serialization;
using NUnit.Framework;

namespace Tomlyn.Tests;

public class NewApiReflectionPocoTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "";

        public long Age { get; set; }
    }

    private sealed class JsonNamedPerson
    {
        [JsonPropertyName("first_name")]
        public string Name { get; set; } = "";

        public long Age { get; set; }
    }

    private sealed class PrivateSetterModel
    {
        [JsonInclude]
        public int Value { get; private set; }
    }

    private sealed class IncludedFieldModel
    {
        [JsonInclude]
        public int Value;
    }

    private sealed class ReadOnlyPropertyModel
    {
        public int Value { get; } = 42;
    }

    private sealed class JsonConstructorModel
    {
        public JsonConstructorModel(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private sealed class AnnotatedJsonConstructorModel
    {
        public AnnotatedJsonConstructorModel()
        {
            Value = -1;
        }

        [JsonConstructor]
        public AnnotatedJsonConstructorModel(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private sealed class SingleCtorModel
    {
        public SingleCtorModel(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class RequiredModel
    {
        [JsonRequired]
        public int Value { get; set; }
    }

    [Test]
    public void SerializeDeserialize_Poco_UsesReflectionFallback()
    {
        var original = new Person { Name = "Ada", Age = 37 };
        var toml = TomlSerializer.Serialize(original);
        var roundtrip = TomlSerializer.Deserialize<Person>(toml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("Ada"));
        Assert.That(roundtrip.Age, Is.EqualTo(37));
    }

    [Test]
    public void Deserialize_Poco_RespectsJsonPropertyName()
    {
        var toml = """
            first_name = "Ada"
            Age = 37
            """;

        var person = TomlSerializer.Deserialize<JsonNamedPerson>(toml);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void Deserialize_PrivateSetter_WithJsonInclude_Works()
    {
        var model = TomlSerializer.Deserialize<PrivateSetterModel>("Value = 123\n");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Value, Is.EqualTo(123));
    }

    [Test]
    public void SerializeDeserialize_PublicField_WithJsonInclude_Works()
    {
        var original = new IncludedFieldModel { Value = 7 };
        var toml = TomlSerializer.Serialize(original);
        var roundtrip = TomlSerializer.Deserialize<IncludedFieldModel>(toml);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(7));
    }

    [Test]
    public void Serialize_ReadOnlyProperty_IsIncluded()
    {
        var toml = TomlSerializer.Serialize(new ReadOnlyPropertyModel());
        Assert.That(toml, Does.Contain("Value"));
        Assert.That(toml, Does.Contain("42"));
    }

    [Test]
    public void Deserialize_UsesSinglePublicConstructor_WhenNoParameterlessExists()
    {
        var model = TomlSerializer.Deserialize<SingleCtorModel>("Name = \"Ada\"\n");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("Ada"));
    }

    [Test]
    public void Deserialize_BindsConstructorParameter_ByMemberName()
    {
        var model = TomlSerializer.Deserialize<JsonConstructorModel>("Value = 5\n");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Value, Is.EqualTo(5));
    }

    [Test]
    public void Deserialize_UsesJsonConstructor_WhenAnnotated()
    {
        var model = TomlSerializer.Deserialize<AnnotatedJsonConstructorModel>("Value = 6\n");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Value, Is.EqualTo(6));
    }

    [Test]
    public void Deserialize_ThrowsWhenRequiredMemberMissing()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<RequiredModel>("Other = 1\n"));
    }
}
