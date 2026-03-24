using System;
using System.Text.Json.Serialization;
using NUnit.Framework;

namespace Tomlyn.Tests;

public sealed class NewApiOptionsValidationTests
{
    [Test]
    public void RootValueKeyName_Empty_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            _ = TomlSerializerOptions.Default with
            {
                RootValueKeyName = "",
            };
        });

        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void RootValueKeyName_InvalidSurrogate_Throws()
    {
        var invalid = "\uD800";
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            _ = TomlSerializerOptions.Default with
            {
                RootValueKeyName = invalid,
            };
        });

        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void RootValueWrapping_DoesNotExpandDottedRootKey()
    {
        var options = TomlSerializerOptions.Default with
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
            RootValueKeyName = "a.b",
            DottedKeyHandling = TomlDottedKeyHandling.Expand,
        };

        var toml = TomlSerializer.Serialize(1, options);

        Assert.That(toml, Does.Contain("\"a.b\""));
        Assert.That(toml, Does.Not.Contain("[a]"));

        var value = TomlSerializer.Deserialize<int>(toml, options);
        Assert.AreEqual(1, value);
    }

    [Test]
    public void PreferredObjectCreationHandling_InvalidValue_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = TomlSerializerOptions.Default with
            {
                PreferredObjectCreationHandling = (JsonObjectCreationHandling)99,
            };
        });

        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }
}
