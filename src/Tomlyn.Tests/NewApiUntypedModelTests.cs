using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests;

public sealed class NewApiUntypedModelTests
{
    [Test]
    public void DeserializeTomlTable_PreservesTableArraysAsTomlTableArray()
    {
        var toml = """
            [[statuses]]
            id = 1
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml);

        Assert.That(table, Is.Not.Null);
        Assert.That(table!.TryGetValue("statuses", out var statuses), Is.True);
        Assert.That(statuses, Is.InstanceOf<TomlTableArray>());
        Assert.That(((TomlTableArray)statuses!).Count, Is.EqualTo(1));
    }

    [Test]
    public void DeserializeTomlTable_TableArrayChildTablesAttachToLastElement()
    {
        var toml = """
            [[statuses]]
            id = 1

            [statuses.metadata]
            result_type = "recent"
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml);

        Assert.That(table, Is.Not.Null);
        Assert.That(table!.TryGetValue("statuses", out var statuses), Is.True);
        Assert.That(statuses, Is.InstanceOf<TomlTableArray>());
        var tableArray = (TomlTableArray)statuses!;
        Assert.That(tableArray, Has.Count.EqualTo(1));
        Assert.That(tableArray[0].TryGetValue("metadata", out var metadata), Is.True);
        Assert.That(metadata, Is.InstanceOf<TomlTable>());
        Assert.That(((TomlTable)metadata!)["result_type"], Is.EqualTo("recent"));
    }
}
