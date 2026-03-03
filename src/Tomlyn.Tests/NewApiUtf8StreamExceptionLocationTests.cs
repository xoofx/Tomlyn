using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests;

public sealed class NewApiUtf8StreamExceptionLocationTests
{
    [Test]
    public void Deserialize_Utf8Bytes_InvalidUtf8_ThrowsTomlExceptionWithByteOffset()
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

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<TomlTable>(bytes));
        Assert.NotNull(ex);
        Assert.NotNull(ex!.Span);

        Assert.AreEqual(5, ex.Offset, "Offset must be the byte position of the invalid sequence.");
        Assert.AreEqual(1, ex.Line);
        Assert.AreEqual(6, ex.Column);
    }

    [Test]
    public void Deserialize_Utf8Bytes_WithBom_Succeeds()
    {
        var bytes = new byte[]
        {
            0xEF, 0xBB, 0xBF, // UTF-8 BOM
            (byte)'a', (byte)' ', (byte)'=', (byte)' ', (byte)'1', (byte)'\n',
        };

        var table = TomlSerializer.Deserialize<TomlTable>(bytes);

        Assert.NotNull(table);
        Assert.AreEqual(1L, (long)table!["a"]);
    }
}
