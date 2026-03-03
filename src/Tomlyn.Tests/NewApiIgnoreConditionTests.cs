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

#pragma warning disable SYSLIB1224
[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(IgnoreConditionModel))]
internal partial class TestTomlIgnoreContext : TomlSerializerContext
{
}
#pragma warning restore SYSLIB1224

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
}
