using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace Tomlyn.Benchmarks;

[JsonSerializable(typeof(BenchmarkDocument))]
internal partial class TomlynBenchmarkContext : TomlSerializerContext
{
}
