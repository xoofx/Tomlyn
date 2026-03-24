using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Tomlyn.Serialization;
using Tomlyn.SourceGeneration;

namespace Tomlyn.Tests;

public sealed class SourceGenerationDiagnosticsTests
{
    [Test]
    public void Generator_ReportsInvalidIndentSize()
    {
        var source = """
            #nullable enable
            using Tomlyn.Serialization;

            [TomlSourceGenerationOptions(IndentSize = 0)]
            [TomlSerializable(typeof(Person))]
            internal partial class Ctx : TomlSerializerContext { }

            public sealed class Person { public string Name { get; set; } = ""; }
            """;

        var diagnostics = RunGenerator(source);
        Assert.That(diagnostics.Any(d => d.Id == "TOMLYN005"), Is.True);
    }

    [Test]
    public void Generator_ReportsInvalidEnumOption()
    {
        var source = """
            #nullable enable
            using Tomlyn;
            using Tomlyn.Serialization;

            [TomlSourceGenerationOptions(NewLine = (TomlNewLineKind)42)]
            [TomlSerializable(typeof(Person))]
            internal partial class Ctx : TomlSerializerContext { }

            public sealed class Person { public string Name { get; set; } = ""; }
            """;

        var diagnostics = RunGenerator(source);
        Assert.That(diagnostics.Any(d => d.Id == "TOMLYN005"), Is.True);
    }

    [Test]
    public void Generator_ReportsInvalidMaxDepth()
    {
        var source = """
            #nullable enable
            using Tomlyn.Serialization;

            [TomlSourceGenerationOptions(MaxDepth = -1)]
            [TomlSerializable(typeof(Person))]
            internal partial class Ctx : TomlSerializerContext { }

            public sealed class Person { public string Name { get; set; } = ""; }
            """;

        var diagnostics = RunGenerator(source);
        Assert.That(diagnostics.Any(d => d.Id == "TOMLYN005"), Is.True);
    }

    [Test]
    public void Generator_ReportsInvalidConverterType()
    {
        var source = """
            #nullable enable
            using System;
            using Tomlyn.Serialization;

            public sealed class NotAConverter { public NotAConverter() { } }

            [TomlSourceGenerationOptions(Converters = new [] { typeof(NotAConverter) })]
            [TomlSerializable(typeof(Person))]
            internal partial class Ctx : TomlSerializerContext { }

            public sealed class Person { public string Name { get; set; } = ""; }
            """;

        var diagnostics = RunGenerator(source);
        Assert.That(diagnostics.Any(d => d.Id == "TOMLYN002"), Is.True);
    }

    [Test]
    public void Generator_WarnsForJsonSerializableUsage()
    {
        var source = """
            #nullable enable
            using System.Text.Json.Serialization;
            using Tomlyn.Serialization;

            [JsonSerializable(typeof(Person))]
            internal partial class Ctx : TomlSerializerContext { }

            public sealed class Person { public string Name { get; set; } = ""; }
            """;

        var diagnostics = RunGenerator(source);
        Assert.That(diagnostics.Any(d => d.Id == "TOMLYN008"), Is.True);
    }

    [Test]
    public void Generator_ReportsInvalidTypeInfoPropertyName()
    {
        var source = """
            #nullable enable
            using Tomlyn.Serialization;

            [TomlSerializable(typeof(Person), TypeInfoPropertyName = "not-valid")]
            internal partial class Ctx : TomlSerializerContext { }

            public sealed class Person { public string Name { get; set; } = ""; }
            """;

        var diagnostics = RunGenerator(source);
        Assert.That(diagnostics.Any(d => d.Id == "TOMLYN005" && d.GetMessage().Contains("TypeInfoPropertyName", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Generator_PreservesNullableReferenceLocals()
    {
        var source = """
            #nullable enable
            using System.Text.Json.Serialization;
            using Tomlyn.Serialization;

            public sealed class InitOptions
            {
                public string? NullableMock { get; init; }
                public string NonNullableMock { get; init; } = string.Empty;
            }

            public sealed class CtorOptions
            {
                [JsonConstructor]
                public CtorOptions(string? nullableMock, string nonNullableMock)
                {
                    NullableMock = nullableMock;
                    NonNullableMock = nonNullableMock;
                }

                public string? NullableMock { get; }
                public string NonNullableMock { get; }
            }

            [TomlSerializable(typeof(InitOptions))]
            [TomlSerializable(typeof(CtorOptions))]
            internal partial class Ctx : TomlSerializerContext { }
            """;

        var result = RunGeneratorTest(source);
        var generatedSource = string.Join(Environment.NewLine, result.GeneratedSources);

        Assert.That(result.Diagnostics.Any(d => d.Id is "CS8600" or "CS8601"), Is.False);
        StringAssert.Contains("string? __memberValue0 = default;", generatedSource);
        StringAssert.Contains("string __memberValue1 = default!;", generatedSource);
        StringAssert.Contains("string? __arg0 = default;", generatedSource);
        StringAssert.Contains("string __arg1 = default!;", generatedSource);
    }

    [Test]
    public void Generator_DoesNotReferenceInternalRuntimeHelpers_FromConsumerAssembly()
    {
        var source = """
            #nullable enable
            using System.Text.Json.Serialization;
            using Tomlyn.Serialization;

            public sealed class Root
            {
                [JsonPropertyName("child")]
                public Child Child { get; } = new();
            }

            public sealed class Child
            {
                public string Name { get; set; } = "";
            }

            [TomlSourceGenerationOptions(PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate)]
            [TomlSerializable(typeof(Root))]
            internal partial class Ctx : TomlSerializerContext { }
            """;

        var result = RunGeneratorTest(source);
        var generatedSource = string.Join(Environment.NewLine, result.GeneratedSources);

        Assert.That(result.Diagnostics.Any(d => d.Id == "CS0122"), Is.False);
        StringAssert.DoesNotContain("TomlTableHeaderExtensionHelper", generatedSource);
    }

    private static ImmutableArray<Diagnostic> RunGenerator(string source)
        => RunGeneratorTest(source).Diagnostics;

    private static GeneratorTestResult RunGeneratorTest(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tomlyn.SourceGeneration.Tests.Input",
            syntaxTrees: new[] { syntaxTree },
            references: CreateReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        IIncrementalGenerator generator = new TomlSerializerContextGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        var inputSyntaxTrees = compilation.SyntaxTrees.ToHashSet();
        var generatedSources = driver.GetRunResult()
            .Results
            .SelectMany(static result => result.GeneratedSources)
            .Select(static generated => generated.SourceText.ToString())
            .ToImmutableArray();
        var outputDiagnostics = outputCompilation.GetDiagnostics().ToImmutableArray();
        var generatedCodeWarnings = outputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Warning &&
                        d.Location.SourceTree is { } tree &&
                        !inputSyntaxTrees.Contains(tree))
            .ToImmutableArray();

        if (!generatedCodeWarnings.IsDefaultOrEmpty)
        {
            Assert.Fail(
                "Generated code produced compiler warnings:" + Environment.NewLine +
                string.Join(Environment.NewLine, generatedCodeWarnings.Select(static d => d.ToString())));
        }

        return new GeneratorTestResult(generatorDiagnostics.Concat(outputDiagnostics).ToImmutableArray(), generatedSources);
    }

    private readonly record struct GeneratorTestResult(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<string> GeneratedSources);

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var references = new List<MetadataReference>();

        void Add(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            if (unique.Add(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // Prefer the trusted platform assemblies list to get a coherent reference set.
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is { Length: > 0 } tpaValue)
        {
            foreach (var path in tpaValue.Split(Path.PathSeparator))
            {
                Add(path);
            }
        }
        else
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                Add(assembly.Location);
            }
        }

        Add(typeof(object).Assembly.Location);
        Add(typeof(Enumerable).Assembly.Location);
        Add(typeof(List<>).Assembly.Location);
        Add(typeof(JsonSerializableAttribute).Assembly.Location);
        Add(typeof(TomlSerializerContext).Assembly.Location);

        return references;
    }
}
