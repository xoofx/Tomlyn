using System;
using System.Text;
using NUnit.Framework;
using Tomlyn.Parsing;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class Utf8InputValidationTests
{
    [Test]
    public void TomlParser_Utf8_InvalidContinuationByte_ThrowsWithByteOffset()
    {
        var prefix = Encoding.ASCII.GetBytes("value = \"");
        var suffix = Encoding.ASCII.GetBytes("\"\n");

        var payload = new byte[prefix.Length + 2 + suffix.Length];
        Buffer.BlockCopy(prefix, 0, payload, 0, prefix.Length);
        payload[prefix.Length] = 0xC2;
        payload[prefix.Length + 1] = 0x20; // invalid continuation byte (must be 0x80..0xBF)
        Buffer.BlockCopy(suffix, 0, payload, prefix.Length + 2, suffix.Length);

        var options = new TomlSerializerOptions
        {
            SourceName = "test.toml",
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
        };

        var parser = TomlParser.Create(payload, options);
        var ex = Assert.Throws<TomlException>(() =>
        {
            while (parser.MoveNext())
            {
            }
        });

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Span.HasValue, Is.True);
        Assert.That(ex.Span!.Value.Offset, Is.EqualTo(prefix.Length));
        Assert.That(ex.Diagnostics.Count, Is.GreaterThan(0));
        Assert.That(ex.Diagnostics[0].Message, Does.Contain("byte offset"));
    }

    [Test]
    public void TomlReader_Utf8_WithBom_IsAccepted()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.ASCII.GetBytes("a = 1\n");
        var payload = new byte[bom.Length + content.Length];
        Buffer.BlockCopy(bom, 0, payload, 0, bom.Length);
        Buffer.BlockCopy(content, 0, payload, bom.Length, content.Length);

        var reader = TomlReader.Create(payload);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.PropertyName, Is.EqualTo("a"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetInt64(), Is.EqualTo(1L));
    }
}
