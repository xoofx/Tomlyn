using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public class NewApiReflectionPocoTests
{
    private sealed class CallbackModel : ITomlOnSerializing, ITomlOnSerialized, ITomlOnDeserializing, ITomlOnDeserialized
    {
        public string Name { get; set; } = "";

        [TomlIgnore]
        public int OnSerializingCount { get; private set; }

        [TomlIgnore]
        public int OnSerializedCount { get; private set; }

        [TomlIgnore]
        public int OnDeserializingCount { get; private set; }

        [TomlIgnore]
        public int OnDeserializedCount { get; private set; }

        [TomlIgnore]
        public bool NameWasAlreadyAssignedInOnDeserializing { get; private set; }

        [TomlIgnore]
        public string? NameSeenInOnDeserialized { get; private set; }

        public void OnTomlSerializing() => OnSerializingCount++;

        public void OnTomlSerialized() => OnSerializedCount++;

        public void OnTomlDeserializing()
        {
            OnDeserializingCount++;
            NameWasAlreadyAssignedInOnDeserializing = Name == "Ada";

            // If this callback runs before TOML member mapping, this should be overwritten by TOML data.
            Name = "from-callback";
        }

        public void OnTomlDeserialized()
        {
            OnDeserializedCount++;
            NameSeenInOnDeserialized = Name;
        }
    }

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

    private abstract class JsonNamedBaseOptions
    {
        [JsonPropertyName("baseValue")]
        public string? Base { get; init; }
    }

    private sealed class JsonNamedDerivedOptions : JsonNamedBaseOptions
    {
        [JsonPropertyName("derivedValue")]
        public string? Derived { get; init; }
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

    private sealed class EmptyModel
    {
    }

    private sealed class MultipleAnnotatedConstructorsModel
    {
        [JsonConstructor]
        public MultipleAnnotatedConstructorsModel(int value)
        {
            Value = value;
        }

        [TomlConstructor]
        public MultipleAnnotatedConstructorsModel(string value)
        {
            Value = int.Parse(value);
        }

        public int Value { get; }
    }

    private sealed class AmbiguousConstructorsModel
    {
        public AmbiguousConstructorsModel(int value)
        {
            Value = value;
        }

        public AmbiguousConstructorsModel(string value)
        {
            Value = int.Parse(value);
        }

        public int Value { get; }
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
    public void SerializeDeserialize_InheritedProperties_RespectJsonPropertyName()
    {
        var original = new JsonNamedDerivedOptions
        {
            Base = "shared",
            Derived = "leaf",
        };

        var toml = TomlSerializer.Serialize(original);
        var roundtrip = TomlSerializer.Deserialize<JsonNamedDerivedOptions>(toml);

        Assert.That(toml, Does.Contain("baseValue = \"shared\""));
        Assert.That(toml, Does.Contain("derivedValue = \"leaf\""));
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Base, Is.EqualTo("shared"));
        Assert.That(roundtrip.Derived, Is.EqualTo("leaf"));
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

    [Test]
    public void SerializeDeserialize_EmptyPoco_NoMembers_Works()
    {
        var toml = TomlSerializer.Serialize(new EmptyModel());
        var model = TomlSerializer.Deserialize<EmptyModel>(toml);
        Assert.That(model, Is.Not.Null);
    }

    [Test]
    public void Deserialize_MultipleAnnotatedConstructors_ThrowsTomlException()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<MultipleAnnotatedConstructorsModel>("Value = 1\n"));
        Assert.That(ex!.Message, Does.Contain("Multiple constructors"));
    }

    [Test]
    public void Deserialize_AmbiguousConstructors_ThrowsTomlException()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<AmbiguousConstructorsModel>("Value = 1\n"));
        Assert.That(ex!.Message, Does.Contain("No suitable constructor"));
    }

    [Test]
    public void SerializeDeserialize_LifecycleCallbacks_AreInvoked()
    {
        var original = new CallbackModel { Name = "Ada" };
        var toml = TomlSerializer.Serialize(original);

        Assert.That(original.OnSerializingCount, Is.EqualTo(1));
        Assert.That(original.OnSerializedCount, Is.EqualTo(1));
        Assert.That(original.OnDeserializingCount, Is.EqualTo(0));
        Assert.That(original.OnDeserializedCount, Is.EqualTo(0));

        var roundtrip = TomlSerializer.Deserialize<CallbackModel>(toml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("Ada"));
        Assert.That(roundtrip.OnDeserializingCount, Is.EqualTo(1));
        Assert.That(roundtrip.OnDeserializedCount, Is.EqualTo(1));
        Assert.That(roundtrip.NameWasAlreadyAssignedInOnDeserializing, Is.False);
        Assert.That(roundtrip.NameSeenInOnDeserialized, Is.EqualTo("Ada"));
    }
}
