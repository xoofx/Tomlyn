using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class GeneratedPerson
{
    public string Name { get; set; } = "";

    public long Age { get; set; }
}

public sealed class GeneratedIntPerson
{
    public int Age { get; set; }
}

public sealed class GeneratedSnakePerson
{
    public string FirstName { get; set; } = "";
}

public sealed class GeneratedOptionsPerson
{
    public string Name { get; set; } = "";

    public long Age { get; set; }
}

public sealed class GeneratedOrderedPerson
{
    [JsonPropertyOrder(-10)]
    public int Age { get; set; }

    [JsonPropertyOrder(10)]
    public string Name { get; set; } = "";
}

public sealed class GeneratedCallbackPerson : ITomlOnSerializing, ITomlOnSerialized, ITomlOnDeserializing, ITomlOnDeserialized
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
        Name = "from-callback";
    }

    public void OnTomlDeserialized()
    {
        OnDeserializedCount++;
        NameSeenInOnDeserialized = Name;
    }
}

public sealed class GeneratedCollectionsPayload
{
    public ImmutableArray<int> Numbers { get; set; }

    public ImmutableList<string>? Names { get; set; }

    public ImmutableHashSet<int>? Ids { get; set; }

    public ISet<string>? Tags { get; set; }

    public HashSet<int>? Values { get; set; }
}

public sealed class GeneratedNullablePayload
{
    public int? Count { get; set; }

    public DateTimeOffset? When { get; set; }
}

public enum GeneratedEnumKind
{
    A = 0,
    B = 1,
}

public sealed class GeneratedEnumAndObjectPayload
{
    public GeneratedEnumKind Kind { get; set; }

    public object? Value { get; set; }
}

public sealed class GeneratedOptionsConverter : TomlConverter<string>
{
    public override string? Read(TomlReader reader) => reader.GetString();

    public override void Write(TomlWriter writer, string value) => writer.WriteStringValue(value.ToUpperInvariant());
}

#pragma warning disable SYSLIB1224
[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedPerson))]
internal partial class TestTomlSerializerContext : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedIntPerson))]
internal partial class TestTomlSerializerContextInt : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GeneratedSnakePerson))]
internal partial class TestTomlSerializerContextSnakeCase : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(
    WriteIndented = false,
    IndentSize = 4,
    NewLine = TomlNewLineKind.CrLf,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = TomlIgnoreCondition.Never,
    DuplicateKeyHandling = TomlDuplicateKeyHandling.LastWins,
    MappingOrder = TomlMappingOrderPolicy.OrderThenAlphabetical,
    DottedKeyHandling = TomlDottedKeyHandling.Expand,
    RootValueHandling = TomlRootValueHandling.WrapInRootKey,
    RootValueKeyName = "root",
    InlineTablePolicy = TomlInlineTablePolicy.WhenSmall,
    TableArrayStyle = TomlTableArrayStyle.InlineArrayOfTables,
    Converters = [typeof(GeneratedOptionsConverter)])]
[JsonSerializable(typeof(GeneratedOptionsPerson))]
internal partial class TestTomlSerializerContextWithOptions : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    MappingOrder = TomlMappingOrderPolicy.OrderThenDeclaration)]
[JsonSerializable(typeof(GeneratedOrderedPerson))]
internal partial class TestTomlSerializerContextOrdering : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedCollectionsPayload))]
internal partial class TestTomlSerializerContextCollections : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedNullablePayload))]
internal partial class TestTomlSerializerContextNullables : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedEnumAndObjectPayload))]
internal partial class TestTomlSerializerContextEnumsAndObjects : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedCallbackPerson))]
internal partial class TestTomlSerializerContextCallbacks : TomlSerializerContext
{
}
#pragma warning restore SYSLIB1224

public class NewApiSourceGenerationTests
{
    [Test]
    public void GeneratedContext_RespectsJsonPropertyOrder_WhenWriting()
    {
        var context = TestTomlSerializerContextOrdering.Default;
        var toml = TomlSerializer.Serialize(new GeneratedOrderedPerson { Name = "Ada", Age = 37 }, context.GeneratedOrderedPerson);

        var ageIndex = toml.IndexOf("age", StringComparison.Ordinal);
        var nameIndex = toml.IndexOf("name", StringComparison.Ordinal);
        Assert.That(ageIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(ageIndex, Is.LessThan(nameIndex));
    }

    [Test]
    public void GeneratedContext_CanDeserializePoco()
    {
        var context = TestTomlSerializerContext.Default;
        var toml = """
            name = "Ada"
            age = 37
            """;

        var person = TomlSerializer.Deserialize(toml, context.GeneratedPerson);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void GeneratedContext_CanSerializePoco()
    {
        var context = TestTomlSerializerContext.Default;
        var toml = TomlSerializer.Serialize(new GeneratedPerson { Name = "Ada", Age = 37 }, context.GeneratedPerson);
        var roundtrip = TomlSerializer.Deserialize(toml, context.GeneratedPerson);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("Ada"));
        Assert.That(roundtrip.Age, Is.EqualTo(37));
    }

    [Test]
    public void GeneratedContext_InvokesLifecycleCallbacks()
    {
        var context = TestTomlSerializerContextCallbacks.Default;
        var original = new GeneratedCallbackPerson { Name = "Ada" };
        var toml = TomlSerializer.Serialize(original, context.GeneratedCallbackPerson);

        Assert.That(original.OnSerializingCount, Is.EqualTo(1));
        Assert.That(original.OnSerializedCount, Is.EqualTo(1));
        Assert.That(original.OnDeserializingCount, Is.EqualTo(0));
        Assert.That(original.OnDeserializedCount, Is.EqualTo(0));

        var roundtrip = TomlSerializer.Deserialize(toml, context.GeneratedCallbackPerson);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("Ada"));
        Assert.That(roundtrip.OnDeserializingCount, Is.EqualTo(1));
        Assert.That(roundtrip.OnDeserializedCount, Is.EqualTo(1));
        Assert.That(roundtrip.NameWasAlreadyAssignedInOnDeserializing, Is.False);
        Assert.That(roundtrip.NameSeenInOnDeserialized, Is.EqualTo("Ada"));
    }

    [Test]
    public void GeneratedContext_CanHandleIntProperties()
    {
        var context = TestTomlSerializerContextInt.Default;
        var toml = """
            age = 37
            """;

        var person = TomlSerializer.Deserialize(toml, context.GeneratedIntPerson);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Age, Is.EqualTo(37));
    }

    [Test]
    public void GeneratedContext_CanApplySnakeCaseNamingPolicy()
    {
        var context = TestTomlSerializerContextSnakeCase.Default;
        var toml = """
            first_name = "Ada"
            """;

        var person = TomlSerializer.Deserialize(toml, context.GeneratedSnakePerson);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.FirstName, Is.EqualTo("Ada"));
    }

    [Test]
    public void GeneratedContext_AppliesTomlSourceGenerationOptions()
    {
        var context = TestTomlSerializerContextWithOptions.Default;
        var options = context.Options;

        Assert.That(options.WriteIndented, Is.False);
        Assert.That(options.IndentSize, Is.EqualTo(4));
        Assert.That(options.NewLine, Is.EqualTo(TomlNewLineKind.CrLf));
        Assert.That(options.PropertyNameCaseInsensitive, Is.True);
        Assert.That(options.DefaultIgnoreCondition, Is.EqualTo(TomlIgnoreCondition.Never));
        Assert.That(options.DuplicateKeyHandling, Is.EqualTo(TomlDuplicateKeyHandling.LastWins));
        Assert.That(options.MappingOrder, Is.EqualTo(TomlMappingOrderPolicy.OrderThenAlphabetical));
        Assert.That(options.DottedKeyHandling, Is.EqualTo(TomlDottedKeyHandling.Expand));
        Assert.That(options.RootValueHandling, Is.EqualTo(TomlRootValueHandling.WrapInRootKey));
        Assert.That(options.RootValueKeyName, Is.EqualTo("root"));
        Assert.That(options.InlineTablePolicy, Is.EqualTo(TomlInlineTablePolicy.WhenSmall));
        Assert.That(options.TableArrayStyle, Is.EqualTo(TomlTableArrayStyle.InlineArrayOfTables));
        Assert.That(options.Converters, Has.Count.EqualTo(1));
        Assert.That(options.Converters[0], Is.InstanceOf<GeneratedOptionsConverter>());
    }

    [Test]
    public void GeneratedContext_CanHandleImmutableAndSetCollections()
    {
        var context = TestTomlSerializerContextCollections.Default;
        var toml = """
            numbers = [1, 2]
            names = ["a", "b"]
            ids = [1, 2, 1]
            tags = ["x", "y", "x"]
            values = [10, 20, 10]
            """;

        var payload = TomlSerializer.Deserialize(toml, context.GeneratedCollectionsPayload);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Numbers.Length, Is.EqualTo(2));
        Assert.That(payload.Numbers[0], Is.EqualTo(1));
        Assert.That(payload.Numbers[1], Is.EqualTo(2));
        Assert.That(payload.Names, Is.Not.Null);
        Assert.That(payload.Names!, Has.Count.EqualTo(2));
        Assert.That(payload.Ids, Is.Not.Null);
        Assert.That(payload.Ids!, Has.Count.EqualTo(2));
        Assert.That(payload.Tags, Is.Not.Null);
        Assert.That(payload.Tags!, Has.Count.EqualTo(2));
        Assert.That(payload.Values, Is.Not.Null);
        Assert.That(payload.Values!, Has.Count.EqualTo(2));

        var roundtripToml = TomlSerializer.Serialize(payload, context.GeneratedCollectionsPayload);
        var roundtrip = TomlSerializer.Deserialize(roundtripToml, context.GeneratedCollectionsPayload);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Numbers, Is.EqualTo(payload.Numbers));
        Assert.That(roundtrip.Names, Is.EqualTo(payload.Names));
        Assert.That(roundtrip.Ids, Is.EqualTo(payload.Ids));
        Assert.That(roundtrip.Tags, Is.Not.Null);
        Assert.That(roundtrip.Tags!.SetEquals(payload.Tags!), Is.True);
        Assert.That(roundtrip.Values, Is.Not.Null);
        Assert.That(roundtrip.Values!.SetEquals(payload.Values!), Is.True);
    }

    [Test]
    public void GeneratedContext_CanHandleNullableValueTypes()
    {
        var context = TestTomlSerializerContextNullables.Default;
        var toml = """
            count = 1
            when = 1979-05-27T07:32:00Z
            """;

        var payload = TomlSerializer.Deserialize(toml, context.GeneratedNullablePayload);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Count, Is.EqualTo(1));
        Assert.That(payload.When, Is.Not.Null);

        var roundtripToml = TomlSerializer.Serialize(payload, context.GeneratedNullablePayload);
        var roundtrip = TomlSerializer.Deserialize(roundtripToml, context.GeneratedNullablePayload);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Count, Is.EqualTo(payload.Count));
        Assert.That(roundtrip.When, Is.EqualTo(payload.When));
    }

    [Test]
    public void GeneratedContext_CanHandleEnumsAndUntypedObjects()
    {
        var context = TestTomlSerializerContextEnumsAndObjects.Default;
        var toml = """
            kind = "a"
            value = 1
            """;

        var payload = TomlSerializer.Deserialize(toml, context.GeneratedEnumAndObjectPayload);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Kind, Is.EqualTo(GeneratedEnumKind.A));
        Assert.That(payload.Value, Is.InstanceOf<long>());
        Assert.That((long)payload.Value!, Is.EqualTo(1));

        var roundtripToml = TomlSerializer.Serialize(payload, context.GeneratedEnumAndObjectPayload);
        var roundtrip = TomlSerializer.Deserialize(roundtripToml, context.GeneratedEnumAndObjectPayload);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Kind, Is.EqualTo(payload.Kind));
        Assert.That(roundtrip.Value, Is.EqualTo(payload.Value));
    }
}
