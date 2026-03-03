using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiCollectionTypeParityTests
{
    private static TomlSerializerOptions CreateRootArrayOptions()
        => new TomlSerializerOptions
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
            RootValueKeyName = "value",
        };

    [Test]
    public void Deserialize_ISet_ShouldReturnHashSet()
    {
        var options = CreateRootArrayOptions();
        var toml = """
            value = ["a", "b", "a"]
            """;

        var result = TomlSerializer.Deserialize<ISet<string>>(toml, options);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<HashSet<string>>());
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result.Contains("a"), Is.True);
        Assert.That(result.Contains("b"), Is.True);
    }

    [Test]
    public void Deserialize_HashSet_ShouldRoundTripValues()
    {
        var options = CreateRootArrayOptions();
        var toml = """
            value = [1, 2, 1]
            """;

        var result = TomlSerializer.Deserialize<HashSet<int>>(toml, options);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result.SetEquals(new[] { 1, 2 }), Is.True);
    }

    [Test]
    public void Deserialize_ImmutableArray_ShouldRoundTripValues()
    {
        var options = CreateRootArrayOptions();
        var toml = """
            value = [10, 20]
            """;

        var result = TomlSerializer.Deserialize<ImmutableArray<int>>(toml, options);

        Assert.That(result.IsDefault, Is.False);
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(10));
        Assert.That(result[1], Is.EqualTo(20));
    }

    [Test]
    public void Deserialize_ImmutableList_ShouldRoundTripValues()
    {
        var options = CreateRootArrayOptions();
        var toml = """
            value = ["a", "b"]
            """;

        var result = TomlSerializer.Deserialize<ImmutableList<string>>(toml, options);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("a"));
        Assert.That(result[1], Is.EqualTo("b"));
    }

    [Test]
    public void Deserialize_ImmutableHashSet_ShouldDeduplicateValues()
    {
        var options = CreateRootArrayOptions();
        var toml = """
            value = [1, 2, 1]
            """;

        var result = TomlSerializer.Deserialize<ImmutableHashSet<int>>(toml, options);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result.Contains(1), Is.True);
        Assert.That(result.Contains(2), Is.True);
    }

    [Test]
    public void Serialize_ImmutableArray_Default_WritesEmptyArray()
    {
        var options = CreateRootArrayOptions();
        var toml = TomlSerializer.Serialize(default(ImmutableArray<int>), options);

        Assert.That(toml, Does.Contain("value"));
        Assert.That(toml, Does.Contain("[]"));
    }
}

