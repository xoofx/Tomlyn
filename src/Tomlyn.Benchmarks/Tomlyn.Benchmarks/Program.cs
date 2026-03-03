using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.JoinSummary)
    .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared))
    .AddColumn(CategoriesColumn.Default)
    .AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config);

