using CsToml;

namespace Tomlyn.Benchmarks;

[TomlSerializedObject]
public sealed partial class BenchmarkOwner
{
    [TomlValueOnSerialized]
    public string Name { get; set; } = string.Empty;

    [TomlValueOnSerialized]
    public string Organization { get; set; } = string.Empty;
}

[TomlSerializedObject]
public sealed partial class BenchmarkEndpoint
{
    [TomlValueOnSerialized]
    public string Name { get; set; } = string.Empty;

    [TomlValueOnSerialized]
    public string Url { get; set; } = string.Empty;

    [TomlValueOnSerialized]
    public bool Enabled { get; set; }
}

[TomlSerializedObject]
public sealed partial class BenchmarkService
{
    [TomlValueOnSerialized]
    public string Name { get; set; } = string.Empty;

    [TomlValueOnSerialized]
    public string Host { get; set; } = string.Empty;

    [TomlValueOnSerialized]
    public int Port { get; set; }

    [TomlValueOnSerialized]
    public BenchmarkEndpoint[] Endpoints { get; set; } = [];
}

[TomlSerializedObject]
public sealed partial class BenchmarkDocument
{
    [TomlValueOnSerialized]
    public string Title { get; set; } = string.Empty;

    [TomlValueOnSerialized]
    public BenchmarkOwner Owner { get; set; } = new();

    [TomlValueOnSerialized]
    public BenchmarkService[] Services { get; set; } = [];
}

