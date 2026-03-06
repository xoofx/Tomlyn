using Tomlyn.Serialization;

namespace Tomlyn.Benchmarks;

[TomlSerializable(typeof(BenchmarkDocument))]
internal partial class TomlynBenchmarkContext : TomlSerializerContext
{
}

