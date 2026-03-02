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
            using System.Text.Json.Serialization;
            using Tomlyn.Serialization;

            [TomlSourceGenerationOptions(IndentSize = 0)]
            [JsonSerializable(typeof(Person))]
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
            using System.Text.Json.Serialization;
            using Tomlyn;
            using Tomlyn.Serialization;

            [TomlSourceGenerationOptions(NewLine = (TomlNewLineKind)42)]
            [JsonSerializable(typeof(Person))]
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
            using System.Text.Json.Serialization;
            using Tomlyn.Serialization;

            public sealed class NotAConverter { public NotAConverter() { } }

            [TomlSourceGenerationOptions(Converters = new [] { typeof(NotAConverter) })]
            [JsonSerializable(typeof(Person))]
            internal partial class Ctx : TomlSerializerContext { }

            public sealed class Person { public string Name { get; set; } = ""; }
            """;

        var diagnostics = RunGenerator(source);
        Assert.That(diagnostics.Any(d => d.Id == "TOMLYN002"), Is.True);
    }

    private static ImmutableArray<Diagnostic> RunGenerator(string source)
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

        var outputDiagnostics = outputCompilation.GetDiagnostics();
        return generatorDiagnostics.Concat(outputDiagnostics).ToImmutableArray();
    }

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
