using System.IO;
using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests;

public sealed class NewApiUtf8StreamExceptionLocationTests
{
    [Test]
    public void Deserialize_Utf8Stream_InvalidUtf8_ThrowsTomlExceptionWithByteOffset()
    {
        // a = "<invalid utf-8>"
        var bytes = new byte[]
        {
            (byte)'a',
            (byte)' ',
            (byte)'=',
            (byte)' ',
            (byte)'"',
            0xC3, // invalid sequence start (expects continuation byte in 0x80..0xBF, but 0x28 is '(')
            0x28,
            (byte)'"',
        };

        using var stream = new MemoryStream(bytes);

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<TomlTable>(stream));
        Assert.NotNull(ex);
        Assert.NotNull(ex!.Span);

        Assert.AreEqual(5, ex.Offset, "Offset must be the byte position of the invalid sequence.");
        Assert.AreEqual(1, ex.Line);
        Assert.AreEqual(6, ex.Column);
    }

    [Test]
    public void Deserialize_Utf8Stream_WithBom_Succeeds()
    {
        var bytes = new byte[]
        {
            0xEF, 0xBB, 0xBF, // UTF-8 BOM
            (byte)'a', (byte)' ', (byte)'=', (byte)' ', (byte)'1', (byte)'\n',
        };

        using var stream = new MemoryStream(bytes);
        var table = TomlSerializer.Deserialize<TomlTable>(stream);

        Assert.NotNull(table);
        Assert.AreEqual(1L, (long)table!["a"]);
    }
}

