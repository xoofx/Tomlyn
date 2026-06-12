using CsToml.Formatter.Resolver;

namespace Tomlyn.Benchmarks;

internal static class CsTomlBenchmarkFormatters
{
    public static void EnsureRegistered()
    {
        TomlValueFormatterResolver.Register<BenchmarkDocument>();
    }
}
