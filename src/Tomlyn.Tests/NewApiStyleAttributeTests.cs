using System.Collections.Generic;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class InlineTableOverrideHolder
{
    [TomlInlineTable(TomlInlineTablePolicy.Always)]
    public InlineTableOverrideChild Child { get; set; } = new() { X = 1 };

    public InlineTableOverrideChild Other { get; set; } = new() { X = 2 };
}

public sealed class InlineTableOverrideChild
{
    public int X { get; set; }
}

[TomlMappingOrder(TomlMappingOrderPolicy.Alphabetical)]
public sealed class AttributeOrderedHolder
{
    public int Z { get; set; } = 2;

    public int A { get; set; } = 1;
}

[TomlDottedKeyHandling(TomlDottedKeyHandling.Expand)]
public sealed class AttributeDottedKeyHolder
{
    [TomlPropertyName("a.b")]
    public int Value { get; set; } = 1;

    public Dictionary<string, int> Map { get; set; } = new() { ["x.y"] = 2 };
}

public sealed class AttributeStringStyleHolder
{
    [TomlStringStyle(TomlStringStyle.Basic)]
    public string FallsBackToGlobalPreferLiteral { get; set; } = "safe";

    [TomlStringStyle(TomlStringStyle.Basic, PreferLiteralWhenNoEscapes = TomlBooleanPreference.True)]
    public string OverridesPreferLiteral { get; set; } = "safe";
}

public sealed class InvalidStringStyleAttributeHolder
{
    [TomlStringStyle(TomlStringStyle.Basic)]
    public int NotString { get; set; } = 1;
}

[TomlSerializable(typeof(InlineTableOverrideHolder))]
[TomlSerializable(typeof(AttributeOrderedHolder))]
[TomlSerializable(typeof(AttributeDottedKeyHolder))]
[TomlSerializable(typeof(AttributeStringStyleHolder))]
internal partial class TestTomlStyleAttributesContext : TomlSerializerContext
{
}

public class NewApiStyleAttributeTests
{
    [Test]
    public void PropertyInlineTableOverride_AppliesOnlyToAnnotatedProperty()
    {
        var value = new InlineTableOverrideHolder();

        var reflectionToml = TomlSerializer.Serialize(value);
        var generatedToml = TomlSerializer.Serialize(value, TestTomlStyleAttributesContext.Default.InlineTableOverrideHolder);

        Assert.That(reflectionToml, Does.Contain("Child = {X = 1}"));
        Assert.That(reflectionToml, Does.Contain("[Other]"));
        Assert.That(generatedToml, Does.Contain("Child = {X = 1}"));
        Assert.That(generatedToml, Does.Contain("[Other]"));
    }

    [Test]
    public void TypeMappingOrderOverride_OrdersMembersAlphabetically()
    {
        var reflectionToml = TomlSerializer.Serialize(new AttributeOrderedHolder());
        var generatedToml = TomlSerializer.Serialize(new AttributeOrderedHolder(), TestTomlStyleAttributesContext.Default.AttributeOrderedHolder);

        Assert.That(reflectionToml.IndexOf("A = 1", System.StringComparison.Ordinal), Is.LessThan(reflectionToml.IndexOf("Z = 2", System.StringComparison.Ordinal)));
        Assert.That(generatedToml.IndexOf("A = 1", System.StringComparison.Ordinal), Is.LessThan(generatedToml.IndexOf("Z = 2", System.StringComparison.Ordinal)));
    }

    [Test]
    public void TypeDottedKeyOverride_ExpandsMemberNamesButNotDictionaryKeys()
    {
        var reflectionToml = TomlSerializer.Serialize(new AttributeDottedKeyHolder());
        var generatedToml = TomlSerializer.Serialize(new AttributeDottedKeyHolder(), TestTomlStyleAttributesContext.Default.AttributeDottedKeyHolder);

        Assert.That(reflectionToml, Does.Contain("[a]"));
        Assert.That(reflectionToml, Does.Contain("b = 1"));
        Assert.That(reflectionToml, Does.Contain("\"x.y\" = 2"));
        Assert.That(reflectionToml, Does.Not.Contain("[Map.x]"));
        Assert.That(generatedToml, Does.Contain("[a]"));
        Assert.That(generatedToml, Does.Contain("b = 1"));
        Assert.That(generatedToml, Does.Contain("\"x.y\" = 2"));
        Assert.That(generatedToml, Does.Not.Contain("[Map.x]"));
    }

    [Test]
    public void PropertyStringStyle_UnspecifiedPreferencesFallbackToGlobalOptions()
    {
        var options = new TomlSerializerOptions
        {
            StringStylePreferences = new TomlStringStylePreferences
            {
                PreferLiteralWhenNoEscapes = false,
            },
        };

        var toml = TomlSerializer.Serialize(new AttributeStringStyleHolder(), options);

        Assert.That(toml, Does.Contain("FallsBackToGlobalPreferLiteral = \"safe\""));
        Assert.That(toml, Does.Contain("OverridesPreferLiteral = 'safe'"));
    }

    [Test]
    public void Reflection_StringStyleAttributeRejectsNonStringMembers()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(new InvalidStringStyleAttributeHolder()));
    }
}
