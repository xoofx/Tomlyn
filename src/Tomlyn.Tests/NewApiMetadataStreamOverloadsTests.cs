using System.IO;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiMetadataStreamOverloadsTests
{
    private sealed class BuiltInContext : TomlSerializerContext
    {
        public override TomlTypeInfo? GetTypeInfo(System.Type type, TomlSerializerOptions options)
        {
            if (type == typeof(int)) return GetBuiltInTypeInfo<int>(options);
            if (type == typeof(TomlTable)) return GetBuiltInTypeInfo<TomlTable>(options);
            return null;
        }
    }

    [Test]
    public void Serialize_Stream_ObjectTypeInfo_WritesToml()
    {
        var context = new BuiltInContext();
        var options = context.Options with { RootValueHandling = TomlRootValueHandling.WrapInRootKey };

        var typeInfo = context.GetTypeInfo(typeof(int), options);
        Assert.NotNull(typeInfo);

        using var stream = new MemoryStream();
        TomlSerializer.Serialize(stream, 123, typeInfo!);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var toml = reader.ReadToEnd();

        Assert.That(toml, Does.Contain("value"));
        Assert.That(toml, Does.Contain("123"));
    }

    [Test]
    public void Deserialize_Utf8Bytes_ObjectTypeInfo_UsesByteOffsets()
    {
        var context = new BuiltInContext();
        var typeInfo = context.GetTypeInfo(typeof(TomlTable), context.Options);
        Assert.NotNull(typeInfo);

        var bytes = new byte[]
        {
            (byte)'a',
            (byte)' ',
            (byte)'=',
            (byte)' ',
            (byte)'"',
            0xC3,
            0x28,
            (byte)'"',
        };

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize(bytes, typeInfo!));
        Assert.NotNull(ex);
        Assert.AreEqual(5, ex!.Offset);
    }

    [Test]
    public void Deserialize_String_ObjectTypeInfo_Works()
    {
        var context = new BuiltInContext();
        var typeInfo = context.GetTypeInfo(typeof(TomlTable), context.Options);
        Assert.NotNull(typeInfo);

        var table = (TomlTable)TomlSerializer.Deserialize("a = 1", typeInfo!)!;
        Assert.AreEqual(1L, (long)table["a"]);
    }
}
