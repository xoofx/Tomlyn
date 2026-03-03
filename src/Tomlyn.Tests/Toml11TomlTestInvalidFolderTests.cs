using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Tomlyn.Tests;

public sealed class Toml11TomlTestInvalidFolderTests
{
    private const string RelativeTomlTestsDirectory = @"../../../../../ext/toml-test/tests";

    [TestCaseSource(nameof(ListToml11InvalidFolderExtensions), Category = "toml-test")]
    public static void Toml11Extensions_SyntaxParser_Roundtrips(string inputName, string toml)
    {
        var doc = SyntaxParser.Parse(toml, inputName);
        var roundtrip = doc.ToString();

        if (doc.HasErrors || toml != roundtrip)
        {
            Console.WriteLine($"Testing {inputName}");
            StandardTests.Dump(toml, doc, roundtrip);
        }

        Assert.False(doc.HasErrors, "TOML 1.1 extension should parse without errors.");
        Assert.AreEqual(toml, roundtrip, "Syntax roundtrip should preserve input for full fidelity.");

        var docUtf8 = SyntaxParser.Parse(Encoding.UTF8.GetBytes(toml), inputName);
        Assert.False(docUtf8.HasErrors, "UTF8 input should parse without errors.");
        Assert.AreEqual(roundtrip, docUtf8.ToString(), "UTF8 and UTF16 syntax roundtrips must match.");
    }

    [TestCaseSource(nameof(ListToml11InvalidFolderExtensions), Category = "toml-test")]
    public static void Toml11Extensions_UntypedModel_RoundtripsSemantics(string inputName, string toml)
    {
        var model = TomlSerializer.Deserialize<TomlTable>(toml)!;
        var tomlFromModel = TomlSerializer.Serialize(model);
        var model2 = TomlSerializer.Deserialize<TomlTable>(tomlFromModel)!;

        var json1 = ModelHelper.ToJson(model);
        var json2 = ModelHelper.ToJson(model2);
        Assert.True(JToken.DeepEquals(json1, json2), $"Untyped model must roundtrip semantics for {inputName}.");
    }

    [Test]
    public static void MinuteOnlyOffsetDateTime_DeserializesToTomlDateTime()
    {
        var model = TomlSerializer.Deserialize<TomlTable>("no-secs = 1987-07-05T17:45Z")!;
        Assert.True(model.TryGetValue("no-secs", out var raw));
        Assert.NotNull(raw);
        var value = (TomlDateTime)raw!;

        Assert.AreEqual(TomlDateTimeKind.OffsetDateTimeByZ, value.Kind);
        Assert.AreEqual(17, value.DateTime.UtcDateTime.Hour);
        Assert.AreEqual(45, value.DateTime.UtcDateTime.Minute);
        Assert.AreEqual(0, value.DateTime.UtcDateTime.Second);
    }

    [Test]
    public static void MinuteOnlyLocalDateTime_DeserializesToTomlDateTime()
    {
        var model = TomlSerializer.Deserialize<TomlTable>("no-secs = 1987-07-05T17:45")!;
        Assert.True(model.TryGetValue("no-secs", out var raw));
        Assert.NotNull(raw);
        var value = (TomlDateTime)raw!;

        Assert.AreEqual(TomlDateTimeKind.LocalDateTime, value.Kind);
        Assert.AreEqual(17, value.DateTime.DateTime.Hour);
        Assert.AreEqual(45, value.DateTime.DateTime.Minute);
        Assert.AreEqual(0, value.DateTime.DateTime.Second);
    }

    [Test]
    public static void MinuteOnlyLocalTime_DeserializesToTomlDateTime()
    {
        var model = TomlSerializer.Deserialize<TomlTable>("no-secs = 17:45")!;
        Assert.True(model.TryGetValue("no-secs", out var raw));
        Assert.NotNull(raw);
        var value = (TomlDateTime)raw!;

        Assert.AreEqual(TomlDateTimeKind.LocalTime, value.Kind);
        Assert.AreEqual(17, value.DateTime.DateTime.Hour);
        Assert.AreEqual(45, value.DateTime.DateTime.Minute);
        Assert.AreEqual(0, value.DateTime.DateTime.Second);
    }

    [Test]
    public static void BasicString_HexByteEscape_Deserializes()
    {
        var model = TomlSerializer.Deserialize<TomlTable>("answer = \"\\x33\"")!;
        Assert.True(model.TryGetValue("answer", out var raw));
        Assert.NotNull(raw);
        Assert.AreEqual("3", (string)raw!);
    }

    [Test]
    public static void InlineTable_TrailingComma_Deserializes()
    {
        var model = TomlSerializer.Deserialize<TomlTable>("abc = { abc = 123, }")!;
        Assert.True(model.TryGetValue("abc", out var raw));
        Assert.NotNull(raw);
        var inline = (TomlTable)raw!;
        Assert.AreEqual(123L, (long)inline["abc"]);
    }

    [Test]
    public static void InlineTable_Multiline_Deserializes()
    {
        var model = TomlSerializer.Deserialize<TomlTable>("t = {a=1,\n b=2}\n")!;
        Assert.True(model.TryGetValue("t", out var raw));
        Assert.NotNull(raw);
        var inline = (TomlTable)raw!;
        Assert.AreEqual(1L, (long)inline["a"]);
        Assert.AreEqual(2L, (long)inline["b"]);
    }

    public static IEnumerable ListToml11InvalidFolderExtensions()
    {
        var directory = Path.GetFullPath(Path.Combine(BaseDirectory, RelativeTomlTestsDirectory));
        Assert.True(Directory.Exists(directory), $"The folder `{directory}` does not exist");

        var tests = new List<TestCaseData>();
        foreach (var relativePath in Toml11ValidButTomlTestMarksInvalid)
        {
            var file = Path.Combine(directory, relativePath.TrimStart('\\', '/').Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(file), $"The TOML test file `{file}` does not exist");

            var functionName = Path.GetFileName(file);
            var input = Encoding.UTF8.GetString(File.ReadAllBytes(file));
            tests.Add(new TestCaseData(functionName, input));
        }

        return tests;
    }

    private static readonly string[] Toml11ValidButTomlTestMarksInvalid =
    [
        @"invalid\datetime\no-secs.toml",
        @"invalid\local-datetime\no-secs.toml",
        @"invalid\local-time\no-secs.toml",
        @"invalid\string\basic-byte-escapes.toml",
        @"invalid\inline-table\trailing-comma.toml",
        @"invalid\inline-table\linebreak-01.toml",
        @"invalid\inline-table\linebreak-02.toml",
        @"invalid\inline-table\linebreak-03.toml",
        @"invalid\inline-table\linebreak-04.toml",
    ];

    private static string BaseDirectory
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            return Path.GetDirectoryName(assembly.Location) ?? Environment.CurrentDirectory;
        }
    }
}
