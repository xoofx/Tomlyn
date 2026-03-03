using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Tomlyn;
using Tomlyn.Model;

if (args.Length == 1 && string.Equals(args[0], "--profile-tomlyn", StringComparison.Ordinal))
{
    var tomlPath = Path.Combine(AppContext.BaseDirectory, "twitter.toml");
    var toml = File.ReadAllText(tomlPath);
    for (var i = 0; i < 3; i++)
    {
        TomlSerializer.Deserialize<TomlTable>(toml);
    }

    for (var i = 0; i < 250; i++)
    {
        TomlSerializer.Deserialize<TomlTable>(toml);
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
