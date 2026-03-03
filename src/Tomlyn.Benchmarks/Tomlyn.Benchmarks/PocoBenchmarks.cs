using BenchmarkDotNet.Attributes;
using CsToml;
using Tomlet;

namespace Tomlyn.Benchmarks;

[MemoryDiagnoser]
public class PocoBenchmarks
{
    private BenchmarkDocument _document = null!;
    private string _documentToml = string.Empty;
    private byte[] _documentUtf8 = [];
    private CsTomlSerializerOptions _csTomlOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = BenchmarkDataFactory.CreateDocument(serviceCount: 200, endpointCountPerService: 12);
        _documentToml = BenchmarkDataFactory.CreateToml(_document);
        _documentUtf8 = BenchmarkDataFactory.ToUtf8Bytes(_documentToml);
        _csTomlOptions = CsTomlSerializerOptions.Default;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Serialize_Poco")]
    public string Tomlyn_Serialize_Poco()
        => TomlSerializer.Serialize(_document);

    [Benchmark]
    [BenchmarkCategory("Serialize_Poco")]
    public string Tomlet_Serialize_Poco()
        => TomletMain.TomlStringFrom(_document);

    [Benchmark]
    [BenchmarkCategory("Serialize_Poco")]
    public string CsToml_Serialize_Poco()
    {
        var result = CsTomlSerializer.Serialize(_document, _csTomlOptions);
        var text = result.ToString();
        result.Dispose();
        return text;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Deserialize_Poco")]
    public BenchmarkDocument Tomlyn_Deserialize_Poco()
        => TomlSerializer.Deserialize<BenchmarkDocument>(_documentToml)!;

    [Benchmark]
    [BenchmarkCategory("Deserialize_Poco")]
    public BenchmarkDocument Tomlet_Deserialize_Poco()
        => TomletMain.To<BenchmarkDocument>(_documentToml);

    [Benchmark]
    [BenchmarkCategory("Deserialize_Poco")]
    public BenchmarkDocument CsToml_Deserialize_Poco()
        => CsTomlSerializer.Deserialize<BenchmarkDocument>(_documentUtf8, _csTomlOptions);
}
