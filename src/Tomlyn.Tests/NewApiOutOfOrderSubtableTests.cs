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

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate)]
[TomlSerializable(typeof(OutOfOrderSubtableRoot))]
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
}
