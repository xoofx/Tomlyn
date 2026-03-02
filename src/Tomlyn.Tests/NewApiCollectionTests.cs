using System.Collections.Generic;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class GeneratedCollectionHolder
{
    public List<long> Values { get; set; } = new();

    public long[] Array { get; set; } = new long[0];

    public IEnumerable<long> EnumerableValues { get; set; } = new List<long>();

    public IReadOnlyList<long> ReadOnlyValues { get; set; } = new List<long>();

    public IDictionary<string, long> Map { get; set; } = new Dictionary<string, long>();
}

public sealed class DottedKeyDictionaryHolder
{
    public Dictionary<string, long> Map { get; set; } = new();
}

#pragma warning disable SYSLIB1224
[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedCollectionHolder))]
[JsonSerializable(typeof(DottedKeyDictionaryHolder))]
internal partial class TestTomlCollectionsContext : TomlSerializerContext
{
}
#pragma warning restore SYSLIB1224

public class NewApiCollectionTests
{
    [Test]
    public void Reflection_CanRoundtripCollections()
    {
        var value = new GeneratedCollectionHolder
        {
            Values = new List<long> { 1, 2, 3 },
            Array = new long[] { 4, 5 },
            EnumerableValues = new List<long> { 6, 7 },
            ReadOnlyValues = new List<long> { 8, 9 },
            Map = new Dictionary<string, long> { ["x"] = 1, ["y"] = 2 },
        };

        var toml = TomlSerializer.Serialize(value);
        var roundtrip = TomlSerializer.Deserialize<GeneratedCollectionHolder>(toml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Values, Is.EqualTo(new[] { 1L, 2L, 3L }));
        Assert.That(roundtrip.Array, Is.EqualTo(new[] { 4L, 5L }));
        Assert.That(roundtrip.EnumerableValues, Is.EqualTo(new[] { 6L, 7L }));
        Assert.That(roundtrip.ReadOnlyValues, Is.EqualTo(new[] { 8L, 9L }));
        Assert.That(roundtrip.Map, Is.EquivalentTo(new Dictionary<string, long> { ["x"] = 1, ["y"] = 2 }));
    }

    [Test]
    public void GeneratedContext_CanRoundtripCollections()
    {
        var context = TestTomlCollectionsContext.Default;
        var value = new GeneratedCollectionHolder
        {
            Values = new List<long> { 1, 2, 3 },
            Array = new long[] { 4, 5 },
            EnumerableValues = new List<long> { 6, 7 },
            ReadOnlyValues = new List<long> { 8, 9 },
            Map = new Dictionary<string, long> { ["x"] = 1, ["y"] = 2 },
        };

        var toml = TomlSerializer.Serialize(value, context.GeneratedCollectionHolder);
        var roundtrip = TomlSerializer.Deserialize(toml, context.GeneratedCollectionHolder);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Values, Is.EqualTo(new[] { 1L, 2L, 3L }));
        Assert.That(roundtrip.Array, Is.EqualTo(new[] { 4L, 5L }));
        Assert.That(roundtrip.EnumerableValues, Is.EqualTo(new[] { 6L, 7L }));
        Assert.That(roundtrip.ReadOnlyValues, Is.EqualTo(new[] { 8L, 9L }));
        Assert.That(roundtrip.Map, Is.EquivalentTo(new Dictionary<string, long> { ["x"] = 1, ["y"] = 2 }));
    }

    [Test]
    public void Default_DottedDictionaryKeys_RoundtripAsLiteral()
    {
        var context = TestTomlCollectionsContext.Default;
        var value = new DottedKeyDictionaryHolder
        {
            Map = new Dictionary<string, long> { ["a.b"] = 1 },
        };

        var toml = TomlSerializer.Serialize(value, context.DottedKeyDictionaryHolder);
        var model = Toml.ToModel(toml);

        Assert.That(model.TryGetValue("map", out var mapValue), Is.True);
        Assert.That(mapValue, Is.InstanceOf<TomlTable>());
        var map = (TomlTable)mapValue!;

        Assert.That(map.TryGetValue("a.b", out var dottedValue), Is.True);
        Assert.That(dottedValue, Is.EqualTo(1L));

        var roundtrip = TomlSerializer.Deserialize(toml, context.DottedKeyDictionaryHolder);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Map.ContainsKey("a.b"), Is.True);
        Assert.That(roundtrip.Map["a.b"], Is.EqualTo(1L));
    }
}
