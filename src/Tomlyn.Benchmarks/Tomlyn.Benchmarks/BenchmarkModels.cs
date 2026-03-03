namespace Tomlyn.Benchmarks;

public sealed class BenchmarkOwner
{
    public string Name { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;
}

public sealed class BenchmarkEndpoint
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool Enabled { get; set; }
}

public sealed class BenchmarkService
{
    public string Name { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public BenchmarkEndpoint[] Endpoints { get; set; } = [];
}

public sealed class BenchmarkDocument
{
    public string Title { get; set; } = string.Empty;

    public BenchmarkOwner Owner { get; set; } = new();

    public BenchmarkService[] Services { get; set; } = [];
}

