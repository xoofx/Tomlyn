using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Tomlyn;
using Tomlyn.Benchmarks;
using Tomlyn.Model;

if (args.Length == 1 && string.Equals(args[0], "--profile-tomlyn", StringComparison.Ordinal))
{
    var toml = BenchmarkDataFactory.CreateToml(BenchmarkDataFactory.CreateDocument(serviceCount: 200, endpointCountPerService: 12));
    for (var i = 0; i < 3; i++)
    {
        TomlSerializer.Deserialize<BenchmarkDocument>(toml, TomlynBenchmarkContext.Default.BenchmarkDocument);
    }

    for (var i = 0; i < 2500; i++)
    {
        TomlSerializer.Deserialize<BenchmarkDocument>(toml, TomlynBenchmarkContext.Default.BenchmarkDocument);
    }

    return;
}


var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.JoinSummary)
    .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared))
    .AddColumn(CategoriesColumn.Default)
    .AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config);

