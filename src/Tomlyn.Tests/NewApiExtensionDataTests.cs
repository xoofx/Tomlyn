using System.Collections.Generic;
using System.Text.Json.Serialization;
using NUnit.Framework;

namespace Tomlyn.Tests;

public sealed class NewApiExtensionDataTests
{
    private sealed class ExtensionDataModel
    {
        public int Known { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?> Extra { get; set; } = new();
    }

    private sealed class NullableExtensionDataModel
    {
        public int Known { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?>? Extra { get; set; }
    }

    [Test]
    public void Deserialize_StoresUnknownKeys_IntoExtensionData()
    {
        var model = TomlSerializer.Deserialize<ExtensionDataModel>("Known = 1\nUnknown = \"x\"\n");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Known, Is.EqualTo(1));
        Assert.That(model.Extra.ContainsKey("Unknown"), Is.True);
        Assert.That(model.Extra["Unknown"], Is.EqualTo("x"));
    }

    [Test]
    public void Serialize_MergesExtensionData_IntoSurroundingTable()
    {
        var model = new ExtensionDataModel
        {
            Known = 1,
            Extra = new Dictionary<string, object?> { ["Unknown"] = "x" },
        };

        var toml = TomlSerializer.Serialize(model);
        Assert.That(toml, Does.Contain("Known"));
        Assert.That(toml, Does.Contain("Unknown"));
    }

    [Test]
    public void Serialize_ThrowsOnExtensionDataKeyConflict()
    {
        var model = new ExtensionDataModel
        {
            Known = 1,
            Extra = new Dictionary<string, object?> { ["Known"] = 2 },
        };

        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(model));
    }

    [Test]
    public void Deserialize_InitializesNullExtensionDataDictionary_WhenSettable()
    {
        var model = TomlSerializer.Deserialize<NullableExtensionDataModel>("Known = 1\nUnknown = 2\n");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Extra, Is.Not.Null);
        Assert.That(model.Extra!.ContainsKey("Unknown"), Is.True);
        Assert.That(model.Extra["Unknown"], Is.EqualTo(2L));
    }
}

