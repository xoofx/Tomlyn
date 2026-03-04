using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class BomInputTests
{
    [Test]
    public void TomlReader_StringWithBom_IsAccepted()
    {
        var reader = TomlReader.Create("\uFEFFa = 1\n");
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.PropertyName, Is.EqualTo("a"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetInt64(), Is.EqualTo(1L));
    }
}
