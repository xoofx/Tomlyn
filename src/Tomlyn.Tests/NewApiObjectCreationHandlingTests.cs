using System.Collections.Generic;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiObjectCreationHandlingTests
{
    private const string NestedToml = """
        Numbers = [4, 5]
        [Child]
        Value = 42
        """;

    private sealed class ReflectionChild
    {
        public int Value { get; set; } = 7;
    }

    private sealed class ReflectionReplaceRoot
    {
        public ReflectionChild Child { get; } = new();

        public List<int> Numbers { get; } = [1, 2, 3];
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    private sealed class ReflectionPopulateRoot
    {
        public ReflectionChild Child { get; } = new();

        public List<int> Numbers { get; } = [1, 2, 3];
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    private sealed class ReflectionPopulateOverrideRoot
    {
        public ReflectionChild Child { get; } = new();

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
        public List<int> Numbers { get; } = [1, 2, 3];
    }

    private sealed class ReflectionPopulateNullRoot
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ReflectionChild? Child { get; }
    }

    private struct ReflectionStructPayload
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    private sealed class ReflectionPopulateStructRoot
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ReflectionStructPayload Payload { get; } = new() { Value1 = 7 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    private sealed class ReflectionTypePopulateStructRoot
    {
        public ReflectionStructPayload Payload { get; } = new() { Value1 = 7 };
    }

    private sealed class ReflectionPopulateSettableStructRoot
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ReflectionStructPayload Payload { get; set; } = new() { Value1 = 7 };
    }

    private sealed class ReflectionPopulateNullableStructRoot
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ReflectionStructPayload? Payload { get; set; } = new() { Value1 = 7 };
    }

    private sealed class ReflectionPopulateImmutableRoot
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public string Name { get; } = "before";
    }

    private sealed class ReflectionConstructorChild
    {
        public ReflectionConstructorChild(int value1)
        {
            Value1 = value1;
        }

        public int Value1 { get; }

        public int Value2 { get; set; }
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    private sealed class ReflectionConstructorPopulateRoot
    {
        public ReflectionConstructorChild Child { get; } = new(7);
    }

    [Test]
    public void Reflection_DefaultReplace_DoesNotPopulateReadOnlyMembers()
    {
        var result = TomlSerializer.Deserialize<ReflectionReplaceRoot>(NestedToml);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(7));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Numbers);
    }

    [Test]
    public void Reflection_TypeLevelPopulate_PopulatesReadOnlyMembers()
    {
        var result = TomlSerializer.Deserialize<ReflectionPopulateRoot>(NestedToml);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(42));
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result.Numbers);
    }

    [Test]
    public void Reflection_GlobalPopulateOption_PopulatesReadOnlyMembers()
    {
        var options = TomlSerializerOptions.Default with
        {
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        };

        var result = TomlSerializer.Deserialize<ReflectionReplaceRoot>(NestedToml, options);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(42));
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result.Numbers);
    }

    [Test]
    public void Reflection_PropertyLevelReplace_OverridesTypeLevelPopulate()
    {
        var result = TomlSerializer.Deserialize<ReflectionPopulateOverrideRoot>(NestedToml);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(42));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Numbers);
    }

    [Test]
    public void Reflection_PropertyLevelPopulate_ReadOnlyNullReference_IsIgnored()
    {
        var result = TomlSerializer.Deserialize<ReflectionPopulateNullRoot>(
            """
            [Child]
            Value = 42
            """);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child, Is.Null);
    }

    [Test]
    public void Reflection_PropertyLevelPopulate_ReadOnlyStruct_Throws()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<ReflectionPopulateStructRoot>(
            """
            [Payload]
            Value2 = 42
            """));

        Assert.That(ex!.Message, Does.Contain("requires a setter"));
    }

    [Test]
    public void Reflection_TypeLevelPopulate_ReadOnlyStruct_IsIgnored()
    {
        var result = TomlSerializer.Deserialize<ReflectionTypePopulateStructRoot>(
            """
            [Payload]
            Value2 = 42
            """);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload.Value1, Is.EqualTo(7));
        Assert.That(result.Payload.Value2, Is.EqualTo(0));
    }

    [Test]
    public void Reflection_PropertyLevelPopulate_StructWithSetter_PopulatesExistingValues()
    {
        var result = TomlSerializer.Deserialize<ReflectionPopulateSettableStructRoot>(
            """
            [Payload]
            Value2 = 42
            """);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload.Value1, Is.EqualTo(7));
        Assert.That(result.Payload.Value2, Is.EqualTo(42));
    }

    [Test]
    public void Reflection_PropertyLevelPopulate_NullableStructWithSetter_PopulatesExistingValues()
    {
        var result = TomlSerializer.Deserialize<ReflectionPopulateNullableStructRoot>(
            """
            [Payload]
            Value2 = 42
            """);

        Assert.That(result, Is.Not.Null);
        var payload = result!.Payload;
        Assert.That(payload.HasValue, Is.True);
        Assert.That(payload.GetValueOrDefault().Value1, Is.EqualTo(7));
        Assert.That(payload.GetValueOrDefault().Value2, Is.EqualTo(42));
    }

    [Test]
    public void Reflection_PropertyLevelPopulate_ReadOnlyImmutableReference_Throws()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<ReflectionPopulateImmutableRoot>(
            """
            Name = "after"
            """));

        Assert.That(ex!.Message, Does.Contain("doesn't support populating"));
    }

    [Test]
    public void Reflection_TypeLevelPopulate_ReadOnlyConstructorBoundReference_PopulatesExistingInstance()
    {
        var result = TomlSerializer.Deserialize<ReflectionConstructorPopulateRoot>(
            """
            [Child]
            Value2 = 42
            """);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value1, Is.EqualTo(7));
        Assert.That(result.Child.Value2, Is.EqualTo(42));
    }
}

public sealed class GeneratedObjectCreationChild
{
    public int Value { get; set; } = 7;
}

public struct GeneratedStructPayload
{
    public int Value1 { get; set; }

    public int Value2 { get; set; }
}

public sealed class GeneratedReplaceObjectCreationRoot
{
    public GeneratedObjectCreationChild Child { get; } = new();

    public List<int> Numbers { get; } = [1, 2, 3];
}

[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class GeneratedPopulateObjectCreationRoot
{
    public GeneratedObjectCreationChild Child { get; } = new();

    public List<int> Numbers { get; } = [1, 2, 3];
}

[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class GeneratedPopulateOverrideObjectCreationRoot
{
    public GeneratedObjectCreationChild Child { get; } = new();

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
    public List<int> Numbers { get; } = [1, 2, 3];
}

public sealed class GeneratedOptionsPopulateObjectCreationRoot
{
    public GeneratedObjectCreationChild Child { get; } = new();

    public List<int> Numbers { get; } = [1, 2, 3];
}

[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class GeneratedTypePopulateStructRoot
{
    public GeneratedStructPayload Payload { get; } = new() { Value1 = 7 };
}

public sealed class GeneratedPropertyPopulateStructRoot
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public GeneratedStructPayload Payload { get; } = new() { Value1 = 7 };
}

public sealed class GeneratedPropertyPopulateSettableStructRoot
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public GeneratedStructPayload Payload { get; set; } = new() { Value1 = 7 };
}

public sealed class GeneratedPropertyPopulateNullableStructRoot
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public GeneratedStructPayload? Payload { get; set; } = new() { Value1 = 7 };
}

public sealed class GeneratedPropertyPopulateNullReferenceRoot
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public GeneratedObjectCreationChild? Child { get; }
}

public sealed class GeneratedConstructorObjectCreationChild
{
    public GeneratedConstructorObjectCreationChild(int value1)
    {
        Value1 = value1;
    }

    public int Value1 { get; }

    public int Value2 { get; set; }
}

[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class GeneratedConstructorPopulateRoot
{
    public GeneratedConstructorObjectCreationChild Child { get; } = new(7);
}

[TomlSerializable(typeof(GeneratedReplaceObjectCreationRoot))]
internal partial class TestTomlSerializerContextObjectCreationReplace : TomlSerializerContext
{
}

[TomlSerializable(typeof(GeneratedPopulateObjectCreationRoot))]
internal partial class TestTomlSerializerContextObjectCreationPopulate : TomlSerializerContext
{
}

[TomlSerializable(typeof(GeneratedPopulateOverrideObjectCreationRoot))]
internal partial class TestTomlSerializerContextObjectCreationOverride : TomlSerializerContext
{
}

[TomlSourceGenerationOptions(PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate)]
[TomlSerializable(typeof(GeneratedOptionsPopulateObjectCreationRoot))]
internal partial class TestTomlSerializerContextObjectCreationPopulateOptions : TomlSerializerContext
{
}

[JsonSourceGenerationOptions(PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate)]
[TomlSerializable(typeof(GeneratedOptionsPopulateObjectCreationRoot))]
internal partial class TestTomlSerializerContextJsonObjectCreationPopulateOptions : TomlSerializerContext
{
}

[TomlSerializable(typeof(GeneratedTypePopulateStructRoot))]
[TomlSerializable(typeof(GeneratedPropertyPopulateStructRoot))]
[TomlSerializable(typeof(GeneratedPropertyPopulateSettableStructRoot))]
[TomlSerializable(typeof(GeneratedPropertyPopulateNullableStructRoot))]
[TomlSerializable(typeof(GeneratedPropertyPopulateNullReferenceRoot))]
[TomlSerializable(typeof(GeneratedConstructorPopulateRoot))]
internal partial class TestTomlSerializerContextObjectCreationPopulateAdvanced : TomlSerializerContext
{
}

public sealed class NewApiSourceGenerationObjectCreationHandlingTests
{
    private const string NestedToml = """
        Numbers = [4, 5]
        [Child]
        Value = 42
        """;

    [Test]
    public void GeneratedContext_DefaultReplace_DoesNotPopulateReadOnlyMembers()
    {
        var context = TestTomlSerializerContextObjectCreationReplace.Default;

        var result = TomlSerializer.Deserialize(NestedToml, context.GeneratedReplaceObjectCreationRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(7));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Numbers);
    }

    [Test]
    public void GeneratedContext_TypeLevelPopulate_PopulatesReadOnlyMembers()
    {
        var context = TestTomlSerializerContextObjectCreationPopulate.Default;

        var result = TomlSerializer.Deserialize(NestedToml, context.GeneratedPopulateObjectCreationRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(42));
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result.Numbers);
    }

    [Test]
    public void GeneratedContext_PropertyLevelReplace_OverridesTypeLevelPopulate()
    {
        var context = TestTomlSerializerContextObjectCreationOverride.Default;

        var result = TomlSerializer.Deserialize(NestedToml, context.GeneratedPopulateOverrideObjectCreationRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(42));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Numbers);
    }

    [Test]
    public void GeneratedContext_TomlSourceGenerationOptions_PopulateReadOnlyMembers()
    {
        var context = TestTomlSerializerContextObjectCreationPopulateOptions.Default;

        var result = TomlSerializer.Deserialize(NestedToml, context.GeneratedOptionsPopulateObjectCreationRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(42));
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result.Numbers);
        Assert.That(context.Options.PreferredObjectCreationHandling, Is.EqualTo(JsonObjectCreationHandling.Populate));
    }

    [Test]
    public void GeneratedContext_JsonSourceGenerationOptions_PopulateOptionIsApplied()
    {
        var context = TestTomlSerializerContextJsonObjectCreationPopulateOptions.Default;

        var result = TomlSerializer.Deserialize(NestedToml, context.GeneratedOptionsPopulateObjectCreationRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value, Is.EqualTo(42));
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result.Numbers);
        Assert.That(context.Options.PreferredObjectCreationHandling, Is.EqualTo(JsonObjectCreationHandling.Populate));
    }

    [Test]
    public void GeneratedContext_TypeLevelPopulate_ReadOnlyStruct_IsIgnored()
    {
        var context = TestTomlSerializerContextObjectCreationPopulateAdvanced.Default;

        var result = TomlSerializer.Deserialize(
            """
            [Payload]
            Value2 = 42
            """,
            context.GeneratedTypePopulateStructRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload.Value1, Is.EqualTo(7));
        Assert.That(result.Payload.Value2, Is.EqualTo(0));
    }

    [Test]
    public void GeneratedContext_PropertyLevelPopulate_ReadOnlyStruct_Throws()
    {
        var context = TestTomlSerializerContextObjectCreationPopulateAdvanced.Default;

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize(
            """
            [Payload]
            Value2 = 42
            """,
            context.GeneratedPropertyPopulateStructRoot));

        Assert.That(ex!.Message, Does.Contain("requires a setter"));
    }

    [Test]
    public void GeneratedContext_PropertyLevelPopulate_StructWithSetter_PopulatesExistingValues()
    {
        var context = TestTomlSerializerContextObjectCreationPopulateAdvanced.Default;

        var result = TomlSerializer.Deserialize(
            """
            [Payload]
            Value2 = 42
            """,
            context.GeneratedPropertyPopulateSettableStructRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload.Value1, Is.EqualTo(7));
        Assert.That(result.Payload.Value2, Is.EqualTo(42));
    }

    [Test]
    public void GeneratedContext_PropertyLevelPopulate_NullableStructWithSetter_PopulatesExistingValues()
    {
        var context = TestTomlSerializerContextObjectCreationPopulateAdvanced.Default;

        var result = TomlSerializer.Deserialize(
            """
            [Payload]
            Value2 = 42
            """,
            context.GeneratedPropertyPopulateNullableStructRoot);

        Assert.That(result, Is.Not.Null);
        var payload = result!.Payload;
        Assert.That(payload.HasValue, Is.True);
        Assert.That(payload.GetValueOrDefault().Value1, Is.EqualTo(7));
        Assert.That(payload.GetValueOrDefault().Value2, Is.EqualTo(42));
    }

    [Test]
    public void GeneratedContext_PropertyLevelPopulate_ReadOnlyNullReference_IsIgnored()
    {
        var context = TestTomlSerializerContextObjectCreationPopulateAdvanced.Default;

        var result = TomlSerializer.Deserialize(
            """
            [Child]
            Value = 42
            """,
            context.GeneratedPropertyPopulateNullReferenceRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child, Is.Null);
    }

    [Test]
    public void GeneratedContext_TypeLevelPopulate_ReadOnlyConstructorBoundReference_PopulatesExistingInstance()
    {
        var context = TestTomlSerializerContextObjectCreationPopulateAdvanced.Default;

        var result = TomlSerializer.Deserialize(
            """
            [Child]
            Value2 = 42
            """,
            context.GeneratedConstructorPopulateRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Child.Value1, Is.EqualTo(7));
        Assert.That(result.Child.Value2, Is.EqualTo(42));
    }
}
