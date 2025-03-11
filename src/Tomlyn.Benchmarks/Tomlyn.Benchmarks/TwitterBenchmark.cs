// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Tomlyn.Model;

namespace Tomlyn.Benchmarks;

[MemoryDiagnoser(false)]
public class TwitterBenchmark
{
    // twitter.json -> twitter.toml
    private string? _tomlString;
    private TomlTable? _tomlObject;

    [GlobalSetup]
    public async Task Setup()
    {
        _tomlString = await File.ReadAllTextAsync("./twitter.toml");
        _tomlObject = Toml.ToModel(_tomlString);
    }

    [Benchmark]
    public TomlTable StringToModel() => Toml.ToModel(_tomlString!);

    [Benchmark]
    public string ModelToString() => Toml.FromModel(_tomlObject!);
}