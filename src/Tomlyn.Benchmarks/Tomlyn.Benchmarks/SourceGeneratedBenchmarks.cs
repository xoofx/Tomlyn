using BenchmarkDotNet.Attributes;
using CsToml;
using Tomlet;
using Tomlyn.Serialization;

namespace Tomlyn.Benchmarks;

[MemoryDiagnoser]
public class SourceGeneratedBenchmarks
{
    private BenchmarkDocument _document = null!;
    private string _documentToml = string.Empty;
    private byte[] _documentUtf8 = [];
    private CsTomlSerializerOptions _csTomlOptions = null!;

    private TomlTypeInfo<BenchmarkDocument> _tomlynTypeInfo = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = BenchmarkDataFactory.CreateDocument(serviceCount: 200, endpointCountPerService: 12);

        var context = TomlynBenchmarkContext.Default;
        _tomlynTypeInfo = context.BenchmarkDocument;

        _documentToml = BenchmarkDataFactory.CreateToml(_document);
        _documentUtf8 = BenchmarkDataFactory.ToUtf8Bytes(_documentToml);
        _csTomlOptions = CsTomlSerializerOptions.Default;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Serialize_SourceGenerated")]
    public string Tomlyn_SourceGenerated_Serialize()
        => TomlSerializer.Serialize(_document, _tomlynTypeInfo);

    [Benchmark]
    [BenchmarkCategory("Serialize_SourceGenerated")]
    public string Tomlet_Serialize_SourceGenerated()
        => TomletMain.TomlStringFrom(_document);

    [Benchmark]
    [BenchmarkCategory("Serialize_SourceGenerated")]
    public string CsToml_Serialize_SourceGenerated()
    {
        var result = CsTomlSerializer.Serialize(_document, _csTomlOptions);
        var text = result.ToString();
        result.Dispose();
        return text;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Deserialize_SourceGenerated")]
    public BenchmarkDocument Tomlyn_SourceGenerated_Deserialize()
        => TomlSerializer.Deserialize(_documentToml, _tomlynTypeInfo)!;

    [Benchmark]
    [BenchmarkCategory("Deserialize_SourceGenerated")]
    public BenchmarkDocument Tomlet_Deserialize_SourceGenerated()
        => TomletMain.To<BenchmarkDocument>(_documentToml);

    [Benchmark]
    [BenchmarkCategory("Deserialize_SourceGenerated")]
    public BenchmarkDocument CsToml_Deserialize_SourceGenerated()
        => CsTomlSerializer.Deserialize<BenchmarkDocument>(_documentUtf8, _csTomlOptions);
}
