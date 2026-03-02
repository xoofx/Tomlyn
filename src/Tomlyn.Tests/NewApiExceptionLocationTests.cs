using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public class NewApiExceptionLocationTests
{
    [Test]
    public void Deserialize_InvalidScalarType_IncludesLocation()
    {
        var options = new TomlSerializerOptions
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
            SourceName = "test.toml",
        };

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<long>("value = \"abc\"\n", options));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(9));
        Assert.That(ex.Message, Does.Contain("test.toml(1,9)"));
    }

    [Test]
    public void Parse_SyntaxError_IncludesLocation()
    {
        var options = new TomlSerializerOptions { SourceName = "test.toml" };

        var reader = TomlReader.Create("a =\n", options);
        reader.Read(); // StartDocument
        reader.Read(); // StartTable
        reader.Read(); // PropertyName
        var ex = Assert.Throws<TomlException>(() => reader.Read()); // missing value
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Line, Is.GreaterThan(0));
        Assert.That(ex.Column, Is.GreaterThan(0));
        Assert.That(ex.Message, Does.Contain("test.toml("));
    }
}
