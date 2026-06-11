using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class IgnoreConditionModel
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Optional { get; set; }

    [TomlIgnore]
    public string AlwaysIgnored { get; set; } = "ignored";

    [TomlIgnore(Condition = TomlIgnoreCondition.WhenWritingDefault)]
    public long DefaultIgnored { get; set; }
}

public sealed class DirectionalIgnoreConditionModel
{
    public int Keep { get; set; }

    [TomlIgnore(Condition = TomlIgnoreCondition.Never)]
    public int Never { get; set; }

    [TomlIgnore(Condition = TomlIgnoreCondition.WhenWriting)]
    public int TomlWriteOnly { get; set; }

    [TomlIgnore(Condition = TomlIgnoreCondition.WhenReading)]
    public int TomlReadOnly { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public int JsonWriteOnly { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
    public int JsonReadOnly { get; set; }
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(IgnoreConditionModel))]
[TomlSerializable(typeof(DirectionalIgnoreConditionModel))]
internal partial class TestTomlIgnoreContext : TomlSerializerContext
{
}

public class NewApiIgnoreConditionTests
{
    [Test]
    public void GeneratedContext_RespectsIgnoreConditions()
    {
        var context = TestTomlIgnoreContext.Default;

        var empty = new IgnoreConditionModel
        {
            Optional = null,
            AlwaysIgnored = "should_not_appear",
            DefaultIgnored = 0,
        };

        var toml = TomlSerializer.Serialize(empty, context.IgnoreConditionModel);
        Assert.That(toml, Does.Not.Contain("optional"));
        Assert.That(toml, Does.Not.Contain("alwaysIgnored"));
        Assert.That(toml, Does.Not.Contain("defaultIgnored"));

        var populated = new IgnoreConditionModel
        {
            Optional = "x",
            AlwaysIgnored = "should_not_appear",
            DefaultIgnored = 1,
        };

        var toml2 = TomlSerializer.Serialize(populated, context.IgnoreConditionModel);
        var model = TomlSerializer.Deserialize<TomlTable>(toml2);

        Assert.That(model, Is.Not.Null);
        var nonNullModel = model!;
        Assert.That(nonNullModel.ContainsKey("optional"), Is.True);
        Assert.That(nonNullModel.ContainsKey("defaultIgnored"), Is.True);
        Assert.That(nonNullModel.ContainsKey("alwaysIgnored"), Is.False);
    }

    [Test]
    public void Reflection_RespectsIgnoreConditions()
    {
        var populated = new IgnoreConditionModel
        {
            Optional = "x",
            AlwaysIgnored = "should_not_appear",
            DefaultIgnored = 1,
        };

        var toml = TomlSerializer.Serialize(populated, options: TomlSerializerOptions.Default with { PropertyNamingPolicy = null });

        Assert.That(toml, Does.Contain("Optional"));
        Assert.That(toml, Does.Contain("DefaultIgnored"));
        Assert.That(toml, Does.Not.Contain("AlwaysIgnored"));
    }

    [Test]
    public void Deserialize_SkipsIgnoredMembers()
    {
        var context = TestTomlIgnoreContext.Default;
        var toml = """
            optional = "x"
            alwaysIgnored = "y"
            defaultIgnored = 2
            """;

        var model = TomlSerializer.Deserialize(toml, context.IgnoreConditionModel);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Optional, Is.EqualTo("x"));
        Assert.That(model.DefaultIgnored, Is.EqualTo(2));
        Assert.That(model.AlwaysIgnored, Is.EqualTo("ignored"));
    }

    [Test]
    public void GeneratedContext_RespectsDirectionalIgnoreConditions()
    {
        var context = TestTomlIgnoreContext.Default;
        var toml = TomlSerializer.Serialize(new DirectionalIgnoreConditionModel
        {
            Keep = 1,
            Never = 2,
            TomlWriteOnly = 3,
            TomlReadOnly = 4,
            JsonWriteOnly = 5,
            JsonReadOnly = 6,
        }, context.DirectionalIgnoreConditionModel);

        Assert.That(toml, Does.Contain("keep = 1"));
        Assert.That(toml, Does.Contain("never = 2"));
        Assert.That(toml, Does.Contain("tomlReadOnly = 4"));
        Assert.That(toml, Does.Contain("jsonReadOnly = 6"));
        Assert.That(toml, Does.Not.Contain("tomlWriteOnly"));
        Assert.That(toml, Does.Not.Contain("jsonWriteOnly"));

        var deserialized = TomlSerializer.Deserialize("""
            keep = 10
            never = 20
            tomlWriteOnly = 30
            tomlReadOnly = 40
            jsonWriteOnly = 50
            jsonReadOnly = 60
            """, context.DirectionalIgnoreConditionModel);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Keep, Is.EqualTo(10));
        Assert.That(deserialized.Never, Is.EqualTo(20));
        Assert.That(deserialized.TomlWriteOnly, Is.EqualTo(30));
        Assert.That(deserialized.TomlReadOnly, Is.Zero);
        Assert.That(deserialized.JsonWriteOnly, Is.EqualTo(50));
        Assert.That(deserialized.JsonReadOnly, Is.Zero);
    }

    [Test]
    public void Reflection_RespectsDirectionalIgnoreConditions()
    {
        var toml = TomlSerializer.Serialize(new DirectionalIgnoreConditionModel
        {
            Keep = 1,
            Never = 2,
            TomlWriteOnly = 3,
            TomlReadOnly = 4,
            JsonWriteOnly = 5,
            JsonReadOnly = 6,
        });

        Assert.That(toml, Does.Contain("Keep = 1"));
        Assert.That(toml, Does.Contain("Never = 2"));
        Assert.That(toml, Does.Contain("TomlReadOnly = 4"));
        Assert.That(toml, Does.Contain("JsonReadOnly = 6"));
        Assert.That(toml, Does.Not.Contain("TomlWriteOnly"));
        Assert.That(toml, Does.Not.Contain("JsonWriteOnly"));

        var deserialized = TomlSerializer.Deserialize<DirectionalIgnoreConditionModel>("""
            Keep = 10
            Never = 20
            TomlWriteOnly = 30
            TomlReadOnly = 40
            JsonWriteOnly = 50
            JsonReadOnly = 60
            """);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Keep, Is.EqualTo(10));
        Assert.That(deserialized.Never, Is.EqualTo(20));
        Assert.That(deserialized.TomlWriteOnly, Is.EqualTo(30));
        Assert.That(deserialized.TomlReadOnly, Is.Zero);
        Assert.That(deserialized.JsonWriteOnly, Is.EqualTo(50));
        Assert.That(deserialized.JsonReadOnly, Is.Zero);
    }
}
