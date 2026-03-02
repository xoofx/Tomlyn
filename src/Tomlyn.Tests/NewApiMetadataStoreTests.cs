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
}

