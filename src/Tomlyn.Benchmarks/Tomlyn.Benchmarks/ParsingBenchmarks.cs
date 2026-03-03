using BenchmarkDotNet.Attributes;
using CsToml;
using Tomlet;
using Tomlyn.Model;

namespace Tomlyn.Benchmarks;

[MemoryDiagnoser]
public class ParsingBenchmarks
{
    private Tomlet.TomlParser _tomletParser = null!;
    private string _smallToml = string.Empty;
    private string _largeToml = string.Empty;
    private byte[] _smallUtf8 = [];
    private byte[] _largeUtf8 = [];
    private CsTomlSerializerOptions _csTomlOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tomletParser = new Tomlet.TomlParser();
        _smallToml = BenchmarkDataFactory.SmallToml;
        _largeToml = File.ReadAllText("twitter.toml");
        _smallUtf8 = BenchmarkDataFactory.ToUtf8Bytes(_smallToml);
        _largeUtf8 = BenchmarkDataFactory.ToUtf8Bytes(_largeToml);
        _csTomlOptions = CsTomlSerializerOptions.Default;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parse_Small")]
    public TomlTable Tomlyn_Parse_Small()
        => TomlSerializer.Deserialize<TomlTable>(_smallToml)!;

    [Benchmark]
    [BenchmarkCategory("Parse_Small")]
    public Tommy.TomlTable Tommy_Parse_Small()
    {
        using var reader = new StringReader(_smallToml);
        return Tommy.TOML.Parse(reader);
    }

    [Benchmark]
    [BenchmarkCategory("Parse_Small")]
    public Tomlet.Models.TomlDocument Tomlet_Parse_Small()
        => _tomletParser.Parse(_smallToml);

    [Benchmark]
    [BenchmarkCategory("Parse_Small")]
    public TomlDocument CsToml_Parse_Small()
        => CsTomlSerializer.Deserialize<TomlDocument>(_smallUtf8, _csTomlOptions);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parse_Large")]
    public TomlTable Tomlyn_Parse_Large()
        => TomlSerializer.Deserialize<TomlTable>(_largeToml)!;

    [Benchmark]
    [BenchmarkCategory("Parse_Large")]
    public Tommy.TomlTable Tommy_Parse_Large()
    {
        using var reader = new StringReader(_largeToml);
        return Tommy.TOML.Parse(reader);
    }

    [Benchmark]
    [BenchmarkCategory("Parse_Large")]
    public Tomlet.Models.TomlDocument Tomlet_Parse_Large()
        => _tomletParser.Parse(_largeToml);

    [Benchmark]
    [BenchmarkCategory("Parse_Large")]
    public TomlDocument CsToml_Parse_Large()
        => CsTomlSerializer.Deserialize<TomlDocument>(_largeUtf8, _csTomlOptions);
}
