using System.IO;
using System.Text;
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
    public void Deserialize_Stream_ObjectTypeInfo_Works()
    {
        var context = new BuiltInContext();
        var typeInfo = context.GetTypeInfo(typeof(TomlTable), context.Options);
        Assert.NotNull(typeInfo);

        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: true))
        {
            writer.Write("a = 1\n");
        }

        stream.Position = 0;
        var table = (TomlTable)TomlSerializer.Deserialize(stream, typeInfo!)!;
        Assert.AreEqual(1L, (long)table["a"]);
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
