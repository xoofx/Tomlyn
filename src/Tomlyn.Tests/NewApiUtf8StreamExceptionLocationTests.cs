using System.IO;
using System.Text;
using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests;

public sealed class NewApiUtf8StreamExceptionLocationTests
{
    [Test]
    public void Deserialize_Stream_SyntaxError_ThrowsTomlExceptionWithLocation()
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: true))
        {
            writer.Write("a = [\n");
        }

        stream.Position = 0;
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<TomlTable>(stream));
        Assert.NotNull(ex);
        Assert.NotNull(ex!.Span);

        Assert.That(ex.Line, Is.EqualTo(2));
        Assert.That(ex.Column, Is.GreaterThan(0));
    }

    [Test]
    public void Deserialize_Stream_WithBom_Succeeds()
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), bufferSize: 1024, leaveOpen: true))
        {
            writer.Write("a = 1\n");
        }

        stream.Position = 0;
        var table = TomlSerializer.Deserialize<TomlTable>(stream);

        Assert.NotNull(table);
        Assert.AreEqual(1L, (long)table!["a"]);
    }
}
