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
