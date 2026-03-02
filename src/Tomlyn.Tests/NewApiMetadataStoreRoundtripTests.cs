using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public class NewApiMetadataStoreRoundtripTests
{
    private sealed class Sample
    {
        public int Value { get; set; }
    }

    [Test]
    public void MetadataStore_DeserializeThenSerialize_PreservesTriviaAndHexIntegers()
    {
        var store = new TomlMetadataStore();
        var options = new TomlSerializerOptions
        {
            MetadataStore = store,
        };

        var sample = TomlSerializer.Deserialize<Sample>(
            """
            # Leading comment
            Value = 0x2A # Trailing comment
            """,
            options);

        var toml = TomlSerializer.Serialize(sample, options);

        Assert.That(toml, Does.Contain("# Leading comment"));
        Assert.That(toml, Does.Contain("# Trailing comment"));
        Assert.That(toml, Does.Contain("Value = 0x"));
    }
}
