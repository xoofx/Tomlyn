using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class OutOfOrderSubtableRoot
{
    [JsonPropertyName("msbuild")]
    public OutOfOrderSubtableMsBuild MSBuild { get; } = new();

    [JsonPropertyName("github")]
    public OutOfOrderSubtableGitHub GitHub { get; } = new();
}

public sealed class OutOfOrderSubtableMsBuild
{
    public string Project { get; set; } = string.Empty;

    public Dictionary<string, object> Properties { get; } = new();
}

public sealed class OutOfOrderSubtableGitHub
{
    public string User { get; set; } = string.Empty;

    public string Repo { get; set; } = string.Empty;
}

public sealed class NestedTableArrayRoot
{
    public List<NestedTableArrayCommand>? Command { get; set; }
}

public sealed class NestedTableArrayCommand
{
    public string? CommandId { get; set; }

    public List<NestedTableArraySlash>? Slash { get; set; }
}

public sealed class NestedTableArraySlash
{
    public string? Path { get; set; }

    public string? Help { get; set; }

    public NestedTableArrayArgs? Args { get; set; }
}

public sealed class NestedTableArrayArgs
{
    public string? Surface { get; set; }
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate)]
[TomlSerializable(typeof(OutOfOrderSubtableRoot))]
[TomlSerializable(typeof(NestedTableArrayRoot))]
internal partial class TestOutOfOrderSubtableContext : TomlSerializerContext
{
}

public class NewApiOutOfOrderSubtableTests
{
    private const string SampleToml =
        """
        [msbuild]
        project = "HelloWorld.csproj"

        [github]
        user = "u"
        repo = "r"

        [msbuild.properties]
        PublishReadyToRun = false
        """;

    private const string NestedTableArrayToml =
        """
        intent_catalog_schema_version = 2

        [[command]]
        command_id = "open_file"

        [[command.slash]]
        path = "/file open"
        help = "Open file"
        args = { surface = "editor" }

        [[command]]
        command_id = "open_folder"

        [[command.slash]]
        path = "/file pick"
        help = "Pick file"
        """;

    [Test]
    public void Reflection_AllowsOutOfOrderSubtableExtensions()
    {
        var result = TomlSerializer.Deserialize<OutOfOrderSubtableRoot>(SampleToml, new TomlSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            SourceName = "repro.toml",
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.MSBuild.Project, Is.EqualTo("HelloWorld.csproj"));
        Assert.That(result.GitHub.User, Is.EqualTo("u"));
        Assert.That(result.GitHub.Repo, Is.EqualTo("r"));
        Assert.That(result.MSBuild.Properties["PublishReadyToRun"], Is.EqualTo(false));
    }

    [Test]
    public void SourceGenerated_AllowsOutOfOrderSubtableExtensions()
    {
        var context = TestOutOfOrderSubtableContext.Default;
        var result = TomlSerializer.Deserialize(SampleToml, context.OutOfOrderSubtableRoot);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.MSBuild.Project, Is.EqualTo("HelloWorld.csproj"));
        Assert.That(result.GitHub.User, Is.EqualTo("u"));
        Assert.That(result.GitHub.Repo, Is.EqualTo("r"));
        Assert.That(result.MSBuild.Properties["PublishReadyToRun"], Is.EqualTo(false));
    }

    [Test]
    public void TypedDictionary_AllowsOutOfOrderSubtableExtensions()
    {
        var result = TomlSerializer.Deserialize<Dictionary<string, object>>(SampleToml, new TomlSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            SourceName = "repro.toml",
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TryGetValue("msbuild", out var rawMsBuild), Is.True);
        Assert.That(rawMsBuild, Is.TypeOf<TomlTable>());

        var msbuild = (TomlTable)rawMsBuild!;
        Assert.That(msbuild["project"], Is.EqualTo("HelloWorld.csproj"));
        Assert.That(msbuild["properties"], Is.TypeOf<TomlTable>());

        var properties = (TomlTable)msbuild["properties"];
        Assert.That(properties["PublishReadyToRun"], Is.EqualTo(false));
    }

    [Test]
    public void Reflection_AllowsNestedTableArraysInsideTableArrays()
    {
        var result = TomlSerializer.Deserialize<NestedTableArrayRoot>(NestedTableArrayToml, new TomlSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            SourceName = "issue-124.toml",
        });

        AssertNestedTableArrayResult(result);
    }

    [Test]
    public void SourceGenerated_AllowsNestedTableArraysInsideTableArrays()
    {
        var context = TestOutOfOrderSubtableContext.Default;
        var result = TomlSerializer.Deserialize(NestedTableArrayToml, context.NestedTableArrayRoot);

        AssertNestedTableArrayResult(result);
    }

    private const string ReopenedTableArrayToml =
        """
        [[file]]
        n = 1

        [[dir]]
        n = 2

        [[file]]
        n = 3
        """;

    [Test]
    public void TomlTable_KeepsTableArrayElementsReopenedAfterAnotherTable()
    {
        var table = TomlSerializer.Deserialize<TomlTable>(ReopenedTableArrayToml)!;

        Assert.That(GetN((TomlTableArray)table["file"]), Is.EqualTo(new long[] { 1, 3 }));
        Assert.That(GetN((TomlTableArray)table["dir"]), Is.EqualTo(new long[] { 2 }));
    }

    [Test]
    public void TomlTable_KeepsDottedTableArrayElementsReopenedAfterSiblingTable()
    {
        const string toml =
            """
            [[a.file]]
            n = 1

            [a.other]
            n = 2

            [[a.file]]
            n = 3
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml)!;

        var a = (TomlTable)table["a"];
        Assert.That(GetN((TomlTableArray)a["file"]), Is.EqualTo(new long[] { 1, 3 }));
        Assert.That(((TomlTable)a["other"])["n"], Is.EqualTo(2L));
    }

    [Test]
    public void TomlTable_KeepsNestedTableArrayElementsReopenedAfterSiblingSubtable()
    {
        const string toml =
            """
            [[x]]

            [[x.c]]
            n = 1

            [x.p]
            n = 2

            [[x.c]]
            n = 3
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml)!;

        var x = (TomlTableArray)table["x"];
        Assert.That(x, Has.Count.EqualTo(1));
        Assert.That(GetN((TomlTableArray)x[0]["c"]), Is.EqualTo(new long[] { 1, 3 }));
    }

    [Test]
    public void TomlTable_KeepsAdjacentTableArrayElements()
    {
        const string toml =
            """
            [[file]]
            n = 1

            [[file]]
            n = 2
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml)!;

        Assert.That(GetN((TomlTableArray)table["file"]), Is.EqualTo(new long[] { 1, 2 }));
    }

    [Test]
    public void TomlTable_KeepsTableArrayElementsSeparatedBySubtableOfCurrentElement()
    {
        const string toml =
            """
            [[file]]
            n = 1

            [file.meta]
            m = 1

            [[file]]
            n = 2
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml)!;

        var file = (TomlTableArray)table["file"];
        Assert.That(GetN(file), Is.EqualTo(new long[] { 1, 2 }));
        Assert.That(((TomlTable)file[0]["meta"])["m"], Is.EqualTo(1L));
        Assert.That(file[1].ContainsKey("meta"), Is.False);
    }

    [Test]
    public void TomlTable_AddsSubtableToLastElementOfArrayReopenedByStandardHeader()
    {
        const string toml =
            """
            [[x]]
            n = 1

            [[x]]
            n = 2

            [y]

            [x.sub]
            m = 3
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml)!;

        var x = (TomlTableArray)table["x"];
        Assert.That(GetN(x), Is.EqualTo(new long[] { 1, 2 }));
        Assert.That(x[0].ContainsKey("sub"), Is.False);
        Assert.That(((TomlTable)x[1]["sub"])["m"], Is.EqualTo(3L));
    }

    [Test]
    public void TomlTable_KeepsFirstHeaderCommentOfReopenedTableArray()
    {
        var options = new TomlSerializerOptions { MetadataStore = new TomlMetadataStore() };
        const string toml =
            """
            # First files
            [[file]]
            n = 1

            [[dir]]
            n = 2

            # More files
            [[file]]
            n = 3
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml, options)!;
        var written = TomlSerializer.Serialize(table, options);

        Assert.That(written, Does.Contain("# First files"));
        Assert.That(written, Does.Not.Contain("# More files"));
    }

    [Test]
    public void TomlTable_KeepsFirstHeaderCommentOfTableReopenedBySubtable()
    {
        var options = new TomlSerializerOptions { MetadataStore = new TomlMetadataStore() };
        const string toml =
            """
            # Parent description
            [a]
            k = 1

            [b]
            m = 2

            # Child description
            [a.c]
            z = 3
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml, options)!;
        var written = TomlSerializer.Serialize(table, options);

        Assert.That(written, Does.Contain("# Parent description"));
        Assert.That(written, Does.Contain("# Child description"));
    }

    [Test]
    public void TomlTable_RecordsMetadataOfParentHeaderDefinedAfterAnotherTable()
    {
        var store = new TomlMetadataStore();
        var options = new TomlSerializerOptions { MetadataStore = store };
        const string toml =
            """
            [[a.items]]
            id = 1

            [other]

            # Parent description
            [a] # Parent note
            tag = 'x'
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml, options)!;
        var written = TomlSerializer.Serialize(table, options);

        Assert.That(store.TryGetProperties(table, out var properties), Is.True);
        Assert.That(properties!.TryGetProperty("a", out var a), Is.True);
        Assert.That(a!.Span.Start.Line, Is.EqualTo(6));
        Assert.That(a.LeadingTrivia, Has.Some.Matches<TomlSyntaxTriviaMetadata>(trivia => trivia.Text == "# Parent description"));
        Assert.That(a.TrailingTrivia, Has.Some.Matches<TomlSyntaxTriviaMetadata>(trivia => trivia.Text == "# Parent note"));
        Assert.That(written, Does.Contain("# Parent description"));
        Assert.That(written, Does.Contain("# Parent note"));
    }

    [Test]
    public void TomlTable_AddsHeadersToLatestElementsOfNestedTableArrays()
    {
        const string toml =
            """
            [[x]]
            id = 1

            [[x.c]]
            n = 1

            [x.p]
            q = 1

            [[x.c]]
            n = 2

            [[x]]
            id = 2

            [[x.c]]
            n = 3

            [x.p]
            q = 2

            [[x.c]]
            n = 4

            [x.r]

            [x.c.sub]
            m = 5
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml)!;

        var x = (TomlTableArray)table["x"];
        Assert.That(x, Has.Count.EqualTo(2));
        Assert.That(GetN((TomlTableArray)x[0]["c"]), Is.EqualTo(new long[] { 1, 2 }));
        Assert.That(((TomlTable)x[0]["p"])["q"], Is.EqualTo(1L));
        var c = (TomlTableArray)x[1]["c"];
        Assert.That(GetN(c), Is.EqualTo(new long[] { 3, 4 }));
        Assert.That(((TomlTable)x[1]["p"])["q"], Is.EqualTo(2L));
        Assert.That(c[0].ContainsKey("sub"), Is.False);
        Assert.That(((TomlTable)c[1]["sub"])["m"], Is.EqualTo(5L));
    }

    [Test]
    public void TomlTable_RepeatedScalarKeyKeepsItsOwnLineFormat()
    {
        // TOML forbids a repeated key: this checks the format recorded when duplicates are allowed to overwrite.
        var options = new TomlSerializerOptions
        {
            MetadataStore = new TomlMetadataStore(),
            DuplicateKeyHandling = TomlDuplicateKeyHandling.LastWins,
        };
        const string toml =
            """
            a = 0x10
            a = 'lit'
            """;

        var table = TomlSerializer.Deserialize<TomlTable>(toml, options)!;
        var written = TomlSerializer.Serialize(table, options);

        Assert.That(written, Does.Contain("a = 'lit'"));
    }

    private static List<long> GetN(TomlTableArray tableArray)
    {
        var values = new List<long>();
        foreach (var element in tableArray)
        {
            values.Add((long)element["n"]);
        }

        return values;
    }

    private static void AssertNestedTableArrayResult(NestedTableArrayRoot? result)
    {
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Command, Has.Count.EqualTo(2));

        var firstCommand = result.Command![0];
        Assert.That(firstCommand.CommandId, Is.EqualTo("open_file"));
        Assert.That(firstCommand.Slash, Has.Count.EqualTo(1));
        var firstSlash = firstCommand.Slash![0];
        Assert.That(firstSlash.Path, Is.EqualTo("/file open"));
        Assert.That(firstSlash.Help, Is.EqualTo("Open file"));
        Assert.That(firstSlash.Args, Is.Not.Null);
        Assert.That(firstSlash.Args!.Surface, Is.EqualTo("editor"));

        var secondCommand = result.Command[1];
        Assert.That(secondCommand.CommandId, Is.EqualTo("open_folder"));
        Assert.That(secondCommand.Slash, Has.Count.EqualTo(1));
        var secondSlash = secondCommand.Slash![0];
        Assert.That(secondSlash.Path, Is.EqualTo("/file pick"));
        Assert.That(secondSlash.Help, Is.EqualTo("Pick file"));
    }
}
