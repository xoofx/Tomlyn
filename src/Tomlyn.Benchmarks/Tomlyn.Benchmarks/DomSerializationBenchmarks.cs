using BenchmarkDotNet.Attributes;
using CsToml;
using Tomlet;
using Tomlyn.Model;

namespace Tomlyn.Benchmarks;

[MemoryDiagnoser]
public class DomSerializationBenchmarks
{
    private Tomlet.TomlParser _tomletParser = null!;
    private string _smallToml = string.Empty;
    private string _largeToml = string.Empty;
    private byte[] _smallUtf8 = [];
    private byte[] _largeUtf8 = [];

    private TomlTable _tomlynSmall = null!;
    private TomlTable _tomlynLarge = null!;
    private Tommy.TomlTable _tommySmall = null!;
    private Tommy.TomlTable _tommyLarge = null!;
    private Tomlet.Models.TomlDocument _tomletSmall = null!;
    private Tomlet.Models.TomlDocument _tomletLarge = null!;
    private TomlDocument _csTomlSmall = null!;
    private TomlDocument _csTomlLarge = null!;
    private CsTomlSerializerOptions _csTomlOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tomletParser = new Tomlet.TomlParser();
        _smallToml = BenchmarkDataFactory.SmallToml;
        _largeToml = File.ReadAllText("twitter.toml");
        _smallUtf8 = BenchmarkDataFactory.ToUtf8Bytes(_smallToml);
        _largeUtf8 = BenchmarkDataFactory.ToUtf8Bytes(_largeToml);

        _tomlynSmall = TomlSerializer.Deserialize<TomlTable>(_smallToml)!;
        _tomlynLarge = TomlSerializer.Deserialize<TomlTable>(_largeToml)!;

        using (var reader = new StringReader(_smallToml))
        {
            _tommySmall = Tommy.TOML.Parse(reader);
        }

        using (var reader = new StringReader(_largeToml))
        {
            _tommyLarge = Tommy.TOML.Parse(reader);
        }

        _tomletSmall = _tomletParser.Parse(_smallToml);
        _tomletLarge = _tomletParser.Parse(_largeToml);

        _csTomlOptions = CsTomlSerializerOptions.Default;
        _csTomlSmall = CsTomlSerializer.Deserialize<TomlDocument>(_smallUtf8, _csTomlOptions);
        _csTomlLarge = CsTomlSerializer.Deserialize<TomlDocument>(_largeUtf8, _csTomlOptions);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Serialize_Dom_Small")]
    public string Tomlyn_Serialize_Dom_Small()
        => TomlSerializer.Serialize(_tomlynSmall);

    [Benchmark]
    [BenchmarkCategory("Serialize_Dom_Small")]
    public string Tommy_Serialize_Dom_Small()
        => _tommySmall.ToString();

    [Benchmark]
    [BenchmarkCategory("Serialize_Dom_Small")]
    public string Tomlet_Serialize_Dom_Small()
        => _tomletSmall.SerializedValue;

    [Benchmark]
    [BenchmarkCategory("Serialize_Dom_Small")]
    public string CsToml_Serialize_Dom_Small()
    {
        var result = CsTomlSerializer.Serialize(_csTomlSmall, _csTomlOptions);
        var text = result.ToString();
        result.Dispose();
        return text;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Serialize_Dom_Large")]
    public string Tomlyn_Serialize_Dom_Large()
        => TomlSerializer.Serialize(_tomlynLarge);

    [Benchmark]
    [BenchmarkCategory("Serialize_Dom_Large")]
    public string Tommy_Serialize_Dom_Large()
        => _tommyLarge.ToString();

    [Benchmark]
    [BenchmarkCategory("Serialize_Dom_Large")]
    public string Tomlet_Serialize_Dom_Large()
        => _tomletLarge.SerializedValue;

    [Benchmark]
    [BenchmarkCategory("Serialize_Dom_Large")]
    public string CsToml_Serialize_Dom_Large()
    {
        var result = CsTomlSerializer.Serialize(_csTomlLarge, _csTomlOptions);
        var text = result.ToString();
        result.Dispose();
        return text;
    }
}
