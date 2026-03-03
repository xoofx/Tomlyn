using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiMetadataStoreTests
{
    private sealed class Sample
    {
    }

    [Test]
    public void SetAndGetProperties_Works()
    {
        var store = new TomlMetadataStore();
        var instance = new Sample();
        var metadata = new TomlPropertiesMetadata();

        Assert.That(store.TryGetProperties(instance, out _), Is.False);

        store.SetProperties(instance, metadata);
        Assert.That(store.TryGetProperties(instance, out var readBack), Is.True);
        Assert.That(readBack, Is.SameAs(metadata));

        store.SetProperties(instance, metadata: null);
        Assert.That(store.TryGetProperties(instance, out _), Is.False);
    }

    [Test]
    public void Deserialize_TomlTable_CapturesMetadata_WhenStoreEnabled()
    {
        var store = new TomlMetadataStore();
        var options = new TomlSerializerOptions { MetadataStore = store };

        var table = TomlSerializer.Deserialize<TomlTable>("a = 0x1 # comment\n", options);

        Assert.That(table, Is.Not.Null);
        Assert.That(store.TryGetProperties(table!, out var properties), Is.True);
        Assert.That(properties, Is.Not.Null);
        Assert.That(properties!.TryGetProperty("a", out var propertyMetadata), Is.True);
        Assert.That(propertyMetadata, Is.Not.Null);
        Assert.That(propertyMetadata!.DisplayKind, Is.EqualTo(TomlPropertyDisplayKind.IntegerHexadecimal));
        Assert.That(propertyMetadata.TrailingTrivia, Is.Not.Null);
        Assert.That(propertyMetadata.TrailingTrivia!.Count, Is.GreaterThan(0));
    }
}
