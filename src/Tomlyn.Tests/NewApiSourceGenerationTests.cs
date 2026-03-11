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

public sealed class GeneratedRequiredPerson
{
    [JsonRequired]
    public string Name { get; set; } = "";

    public int Age { get; set; }
}

public sealed class GeneratedInitOnlyPerson
{
    public string Name { get; init; } = "";

    public int Age { get; init; } = 42;
}

public sealed class GeneratedRequiredInitPerson
{
    public required string Name { get; init; } = "";

    public int Age { get; init; } = 42;
}

public sealed class GeneratedCtorInitRequiredPerson
{
    [JsonConstructor]
    public GeneratedCtorInitRequiredPerson(string name)
    {
        Name = name;
    }

    public required string Name { get; init; } = "";

    public int Age { get; init; } = 42;
}

public sealed class GeneratedExtensionDataPerson
{
    public string Name { get; set; } = "";

    [JsonExtensionData]
    public Dictionary<string, object?>? Extra { get; set; }
}

public sealed class GeneratedCtorPerson
{
    [JsonConstructor]
    public GeneratedCtorPerson(string firstName, int age = 42)
    {
        FirstName = firstName;
        Age = age;
    }

    [JsonPropertyName("first_name")]
    public string FirstName { get; }

    public int Age { get; }
}

public sealed class GeneratedCtorSelectionPerson
{
    public GeneratedCtorSelectionPerson()
    {
        Name = "default";
    }

    [JsonConstructor]
    public GeneratedCtorSelectionPerson(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(GeneratedCat), "cat")]
[JsonDerivedType(typeof(GeneratedDog), "dog")]
public interface IGeneratedAnimal
{
}

public sealed class GeneratedCat : IGeneratedAnimal
{
    public string Name { get; set; } = "";

    public int Lives { get; set; }
}

public sealed class GeneratedDog : IGeneratedAnimal
{
    public string Name { get; set; } = "";

    public bool GoodBoy { get; set; }
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

public sealed class GeneratedNullableReferencePayload
{
    public string? NullableMock { get; init; }

    public string NonNullableMock { get; init; } = string.Empty;
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

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedPerson))]
internal partial class TestTomlSerializerContext : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedPerson), TypeInfoPropertyName = "GeneratedPersonInfo")]
internal partial class TestTomlSerializerContextCustomPropertyName : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedIntPerson))]
internal partial class TestTomlSerializerContextInt : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[TomlSerializable(typeof(GeneratedSnakePerson))]
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
    MaxDepth = 16,
    MappingOrder = TomlMappingOrderPolicy.OrderThenAlphabetical,
    DottedKeyHandling = TomlDottedKeyHandling.Expand,
    RootValueHandling = TomlRootValueHandling.WrapInRootKey,
    RootValueKeyName = "root",
    InlineTablePolicy = TomlInlineTablePolicy.WhenSmall,
    TableArrayStyle = TomlTableArrayStyle.InlineArrayOfTables,
    Converters = [typeof(GeneratedOptionsConverter)])]
[TomlSerializable(typeof(GeneratedOptionsPerson))]
internal partial class TestTomlSerializerContextWithOptions : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    MappingOrder = TomlMappingOrderPolicy.OrderThenDeclaration)]
[TomlSerializable(typeof(GeneratedOrderedPerson))]
internal partial class TestTomlSerializerContextOrdering : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedCollectionsPayload))]
internal partial class TestTomlSerializerContextCollections : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedNullablePayload))]
internal partial class TestTomlSerializerContextNullables : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedNullableReferencePayload))]
internal partial class TestTomlSerializerContextNullableReferences : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedEnumAndObjectPayload))]
internal partial class TestTomlSerializerContextEnumsAndObjects : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedCallbackPerson))]
internal partial class TestTomlSerializerContextCallbacks : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedRequiredPerson))]
internal partial class TestTomlSerializerContextRequired : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedInitOnlyPerson))]
internal partial class TestTomlSerializerContextInitOnly : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedRequiredInitPerson))]
internal partial class TestTomlSerializerContextRequiredInit : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedCtorInitRequiredPerson))]
internal partial class TestTomlSerializerContextCtorInitRequired : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedExtensionDataPerson))]
internal partial class TestTomlSerializerContextExtensionData : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedCtorPerson))]
internal partial class TestTomlSerializerContextConstructor : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedCtorSelectionPerson))]
internal partial class TestTomlSerializerContextConstructorSelection : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(IGeneratedAnimal))]
internal partial class TestTomlSerializerContextPolymorphism : TomlSerializerContext
{
}

[TomlPolymorphic(TypeDiscriminatorPropertyName = "type")]
[TomlDerivedType(typeof(GeneratedDefaultCircle))]
[TomlDerivedType(typeof(GeneratedDefaultSquare), "square")]
public abstract class GeneratedDefaultShape
{
}

public sealed class GeneratedDefaultCircle : GeneratedDefaultShape
{
    public string Color { get; set; } = "";
    public double Radius { get; set; }
}

public sealed class GeneratedDefaultSquare : GeneratedDefaultShape
{
    public string Color { get; set; } = "";
    public double Side { get; set; }
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedDefaultShape))]
internal partial class TestTomlSerializerContextDefaultDerivedType : TomlSerializerContext
{
}

[TomlPolymorphic(TypeDiscriminatorPropertyName = "kind", UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.FallBackToBaseType)]
[TomlDerivedType(typeof(GeneratedAttrFallbackDerived), "derived")]
public class GeneratedAttrFallbackBase
{
    public string Name { get; set; } = "";
}

public sealed class GeneratedAttrFallbackDerived : GeneratedAttrFallbackBase
{
    public int Extra { get; set; }
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedAttrFallbackBase))]
internal partial class TestTomlSerializerContextAttrFallback : TomlSerializerContext
{
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(GeneratedJsonAttrFallbackDerived), "derived")]
public class GeneratedJsonAttrFallbackBase
{
    public string Name { get; set; } = "";
}

public sealed class GeneratedJsonAttrFallbackDerived : GeneratedJsonAttrFallbackBase
{
    public int Extra { get; set; }
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedJsonAttrFallbackBase))]
internal partial class TestTomlSerializerContextJsonAttrFallback : TomlSerializerContext
{
}

[TomlPolymorphic(TypeDiscriminatorPropertyName = "type")]
[TomlDerivedType(typeof(GeneratedIntDiscrimCircle), 1)]
[TomlDerivedType(typeof(GeneratedIntDiscrimSquare), 2)]
public abstract class GeneratedIntDiscrimShape
{
}

public sealed class GeneratedIntDiscrimCircle : GeneratedIntDiscrimShape
{
    public string Color { get; set; } = "";
    public double Radius { get; set; }
}

public sealed class GeneratedIntDiscrimSquare : GeneratedIntDiscrimShape
{
    public string Color { get; set; } = "";
    public double Side { get; set; }
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(GeneratedIntDiscrimShape))]
internal partial class TestTomlSerializerContextIntDiscriminator : TomlSerializerContext
{
}

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
    public void GeneratedContext_CanUseCustomTypeInfoPropertyName()
    {
        var context = TestTomlSerializerContextCustomPropertyName.Default;
        var toml = """
            name = "Ada"
            age = 37
            """;

        var person = TomlSerializer.Deserialize(toml, context.GeneratedPersonInfo);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(37));
    }

    [Test]
    public void GeneratedContext_CanDeserializeInitOnlyMembers()
    {
        var context = TestTomlSerializerContextInitOnly.Default;
        var toml = """
            name = "Ada"
            """;

        var person = TomlSerializer.Deserialize(toml, context.GeneratedInitOnlyPerson);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(42));
    }

    [Test]
    public void GeneratedContext_CanDeserializeRequiredKeywordMembers()
    {
        var context = TestTomlSerializerContextRequiredInit.Default;
        var toml = """
            name = "Ada"
            """;

        var person = TomlSerializer.Deserialize(toml, context.GeneratedRequiredInitPerson);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Ada"));
        Assert.That(person.Age, Is.EqualTo(42));
    }

    [Test]
    public void GeneratedContext_Throws_WhenRequiredKeywordMemberIsMissing()
    {
        var context = TestTomlSerializerContextRequiredInit.Default;
        var toml = """
            age = 37
            """;

        var exception = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize(toml, context.GeneratedRequiredInitPerson));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Missing required TOML key 'name'"));
        Assert.That(exception.Message, Does.Contain(typeof(GeneratedRequiredInitPerson).FullName));
    }

    [Test]
    public void GeneratedContext_Throws_WhenRequiredMemberIsMissing()
    {
        var context = TestTomlSerializerContextRequired.Default;
        var toml = """
            age = 37
            """;

        var exception = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize(toml, context.GeneratedRequiredPerson));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Missing required TOML key 'name'"));
        Assert.That(exception.Message, Does.Contain(typeof(GeneratedRequiredPerson).FullName));
        Assert.That(exception.Line, Is.GreaterThanOrEqualTo(1));
        Assert.That(exception.Column, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void GeneratedContext_CanDeserializeConstructorBoundRequiredInitMembers()
    {
        var context = TestTomlSerializerContextCtorInitRequired.Default;
        var toml = """
            name = "Ada"
            age = 37
            """;

        var model = TomlSerializer.Deserialize(toml, context.GeneratedCtorInitRequiredPerson);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("Ada"));
        Assert.That(model.Age, Is.EqualTo(37));
    }

    [Test]
    public void GeneratedContext_PreservesConstructorDefaultsForMissingInitOnlyMembers()
    {
        var context = TestTomlSerializerContextCtorInitRequired.Default;
        var toml = """
            name = "Ada"
            """;

        var model = TomlSerializer.Deserialize(toml, context.GeneratedCtorInitRequiredPerson);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("Ada"));
        Assert.That(model.Age, Is.EqualTo(42));
    }

    [Test]
    public void GeneratedContext_CapturesJsonExtensionData_WhenReading()
    {
        var context = TestTomlSerializerContextExtensionData.Default;
        var toml = """
            name = "Ada"
            unknown = 1
            """;

        var model = TomlSerializer.Deserialize(toml, context.GeneratedExtensionDataPerson);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("Ada"));
        Assert.That(model.Extra, Is.Not.Null);
        Assert.That(model.Extra!.ContainsKey("unknown"), Is.True);
        Assert.That(model.Extra["unknown"], Is.InstanceOf<long>());
        Assert.That((long)model.Extra["unknown"]!, Is.EqualTo(1));
    }

    [Test]
    public void GeneratedContext_Throws_WhenExtensionDataKeyConflictsWithMemberKey_OnWrite()
    {
        var context = TestTomlSerializerContextExtensionData.Default;
        var model = new GeneratedExtensionDataPerson
        {
            Name = "Ada",
            Extra = new Dictionary<string, object?> { ["name"] = "from-extra" },
        };

        var exception = Assert.Throws<TomlException>(() => TomlSerializer.Serialize(model, context.GeneratedExtensionDataPerson));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Extension data key"));
        Assert.That(exception.Message, Does.Contain("conflicts"));
    }

    [Test]
    public void GeneratedContext_CanDeserializeUsingJsonConstructor_AndDefaultValues()
    {
        var context = TestTomlSerializerContextConstructor.Default;
        var toml = """
            first_name = "Ada"
            """;

        var model = TomlSerializer.Deserialize(toml, context.GeneratedCtorPerson);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.FirstName, Is.EqualTo("Ada"));
        Assert.That(model.Age, Is.EqualTo(42));
    }

    [Test]
    public void GeneratedContext_Throws_WhenRequiredConstructorParameterIsMissing()
    {
        var context = TestTomlSerializerContextConstructorSelection.Default;
        var toml = """
            other = 1
            """;

        var exception = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize(toml, context.GeneratedCtorSelectionPerson));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Missing required constructor parameter 'name'"));
        Assert.That(exception.Message, Does.Contain(typeof(GeneratedCtorSelectionPerson).FullName));
        Assert.That(exception.Line, Is.GreaterThanOrEqualTo(1));
        Assert.That(exception.Column, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void GeneratedContext_PrefersAnnotatedJsonConstructor_WhenMultipleAreAvailable()
    {
        var context = TestTomlSerializerContextConstructorSelection.Default;
        var toml = """
            name = "Ada"
            """;

        var model = TomlSerializer.Deserialize(toml, context.GeneratedCtorSelectionPerson);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("Ada"));
    }

    [Test]
    public void GeneratedContext_CanDeserializePolymorphicInterface()
    {
        var context = TestTomlSerializerContextPolymorphism.Default;
        var toml = """
            kind = "cat"
            name = "Ada"
            lives = 9
            """;

        var model = TomlSerializer.Deserialize(toml, context.IGeneratedAnimal);

        Assert.That(model, Is.Not.Null);
        Assert.That(model, Is.InstanceOf<GeneratedCat>());
        var cat = (GeneratedCat)model!;
        Assert.That(cat.Name, Is.EqualTo("Ada"));
        Assert.That(cat.Lives, Is.EqualTo(9));
    }

    [Test]
    public void GeneratedContext_Throws_WhenPolymorphicDiscriminatorIsMissing()
    {
        var context = TestTomlSerializerContextPolymorphism.Default;
        var toml = """
            name = "Ada"
            """;

        var exception = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize(toml, context.IGeneratedAnimal));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Missing discriminator key 'kind'"));
        Assert.That(exception.Message, Does.Contain(typeof(IGeneratedAnimal).FullName));
        Assert.That(exception.Line, Is.GreaterThanOrEqualTo(1));
        Assert.That(exception.Column, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void GeneratedContext_CanSerializePolymorphicInterface()
    {
        var context = TestTomlSerializerContextPolymorphism.Default;
        IGeneratedAnimal model = new GeneratedDog { Name = "Rex", GoodBoy = true };

        var toml = TomlSerializer.Serialize(model, context.IGeneratedAnimal);

        Assert.That(toml, Does.Contain("kind = \"dog\""));
        Assert.That(toml, Does.Contain("name = \"Rex\""));
        Assert.That(toml, Does.Contain("goodBoy = true"));
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
        Assert.That(options.MaxDepth, Is.EqualTo(16));
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
    public void GeneratedContext_CanHandleNullableReferenceTypes()
    {
        var context = TestTomlSerializerContextNullableReferences.Default;

        var emptyPayload = TomlSerializer.Deserialize("", context.GeneratedNullableReferencePayload);

        Assert.That(emptyPayload, Is.Not.Null);
        Assert.That(emptyPayload!.NullableMock, Is.Null);
        Assert.That(emptyPayload.NonNullableMock, Is.EqualTo(string.Empty));

        var toml = """
            nullableMock = "hello"
            nonNullableMock = "world"
            """;

        var payload = TomlSerializer.Deserialize(toml, context.GeneratedNullableReferencePayload);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.NullableMock, Is.EqualTo("hello"));
        Assert.That(payload.NonNullableMock, Is.EqualTo("world"));
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

    // --- Feature 1: Default Derived Type (source-gen) ---

    [Test]
    public void GeneratedContext_DefaultDerivedType_Serialize_OmitsDiscriminator()
    {
        var context = TestTomlSerializerContextDefaultDerivedType.Default;
        GeneratedDefaultShape model = new GeneratedDefaultCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(model, context.GeneratedDefaultShape);

        Assert.That(toml, Does.Not.Contain("type"));
        Assert.That(toml, Does.Contain("color = \"red\""));
        Assert.That(toml, Does.Contain("radius = 5"));
    }

    [Test]
    public void GeneratedContext_DefaultDerivedType_Serialize_WritesDiscriminator_ForNonDefault()
    {
        var context = TestTomlSerializerContextDefaultDerivedType.Default;
        GeneratedDefaultShape model = new GeneratedDefaultSquare { Color = "blue", Side = 3.0 };
        var toml = TomlSerializer.Serialize(model, context.GeneratedDefaultShape);

        Assert.That(toml, Does.Contain("type = \"square\""));
        Assert.That(toml, Does.Contain("color = \"blue\""));
        Assert.That(toml, Does.Contain("side = 3"));
    }

    [Test]
    public void GeneratedContext_DefaultDerivedType_Deserialize_MissingDiscriminator()
    {
        var context = TestTomlSerializerContextDefaultDerivedType.Default;
        var toml = """
            color = "red"
            radius = 5.0
            """;

        var result = TomlSerializer.Deserialize(toml, context.GeneratedDefaultShape);

        Assert.That(result, Is.TypeOf<GeneratedDefaultCircle>());
        var circle = (GeneratedDefaultCircle)result!;
        Assert.That(circle.Color, Is.EqualTo("red"));
        Assert.That(circle.Radius, Is.EqualTo(5.0));
    }

    [Test]
    public void GeneratedContext_DefaultDerivedType_Deserialize_UnknownDiscriminator()
    {
        var context = TestTomlSerializerContextDefaultDerivedType.Default;
        var toml = """
            type = "triangle"
            color = "green"
            """;

        var result = TomlSerializer.Deserialize(toml, context.GeneratedDefaultShape);

        Assert.That(result, Is.TypeOf<GeneratedDefaultCircle>());
        Assert.That(((GeneratedDefaultCircle)result!).Color, Is.EqualTo("green"));
    }

    [Test]
    public void GeneratedContext_DefaultDerivedType_Roundtrip()
    {
        var context = TestTomlSerializerContextDefaultDerivedType.Default;
        GeneratedDefaultShape original = new GeneratedDefaultCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(original, context.GeneratedDefaultShape);
        var result = TomlSerializer.Deserialize(toml, context.GeneratedDefaultShape);

        Assert.That(result, Is.TypeOf<GeneratedDefaultCircle>());
        var circle = (GeneratedDefaultCircle)result!;
        Assert.That(circle.Color, Is.EqualTo("red"));
        Assert.That(circle.Radius, Is.EqualTo(5.0));
    }

    // --- Feature 2: UnknownDerivedTypeHandling on attribute (source-gen) ---

    [Test]
    public void GeneratedContext_UnknownDiscriminator_FallbackViaTomlAttribute()
    {
        var context = TestTomlSerializerContextAttrFallback.Default;
        var toml = """
            kind = "unknown"
            name = "test"
            """;

        var result = TomlSerializer.Deserialize(toml, context.GeneratedAttrFallbackBase);

        Assert.That(result, Is.TypeOf<GeneratedAttrFallbackBase>());
        Assert.That(result!.Name, Is.EqualTo("test"));
    }

    [Test]
    public void GeneratedContext_UnknownDiscriminator_FallbackViaJsonAttribute()
    {
        var context = TestTomlSerializerContextJsonAttrFallback.Default;
        var toml = """
            kind = "unknown"
            name = "test"
            """;

        var result = TomlSerializer.Deserialize(toml, context.GeneratedJsonAttrFallbackBase);

        Assert.That(result, Is.TypeOf<GeneratedJsonAttrFallbackBase>());
        Assert.That(result!.Name, Is.EqualTo("test"));
    }

    // --- Feature 3: Integer Discriminators (source-gen) ---

    [Test]
    public void GeneratedContext_IntDiscriminator_Serialize()
    {
        var context = TestTomlSerializerContextIntDiscriminator.Default;
        GeneratedIntDiscrimShape model = new GeneratedIntDiscrimCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(model, context.GeneratedIntDiscrimShape);

        Assert.That(toml, Does.Contain("type = \"1\""));
        Assert.That(toml, Does.Contain("color = \"red\""));
        Assert.That(toml, Does.Contain("radius = 5"));
    }

    [Test]
    public void GeneratedContext_IntDiscriminator_Deserialize()
    {
        var context = TestTomlSerializerContextIntDiscriminator.Default;
        var toml = """
            type = "2"
            color = "blue"
            side = 3.0
            """;

        var result = TomlSerializer.Deserialize(toml, context.GeneratedIntDiscrimShape);

        Assert.That(result, Is.TypeOf<GeneratedIntDiscrimSquare>());
        var square = (GeneratedIntDiscrimSquare)result!;
        Assert.That(square.Color, Is.EqualTo("blue"));
        Assert.That(square.Side, Is.EqualTo(3.0));
    }

    [Test]
    public void GeneratedContext_IntDiscriminator_Roundtrip()
    {
        var context = TestTomlSerializerContextIntDiscriminator.Default;
        GeneratedIntDiscrimShape original = new GeneratedIntDiscrimCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(original, context.GeneratedIntDiscrimShape);
        var result = TomlSerializer.Deserialize(toml, context.GeneratedIntDiscrimShape);

        Assert.That(result, Is.TypeOf<GeneratedIntDiscrimCircle>());
        var circle = (GeneratedIntDiscrimCircle)result!;
        Assert.That(circle.Color, Is.EqualTo("red"));
        Assert.That(circle.Radius, Is.EqualTo(5.0));
    }
}
