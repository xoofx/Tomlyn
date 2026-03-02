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
#pragma warning restore SYSLIB1224

public class NewApiSourceGenerationTests
{
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
}
