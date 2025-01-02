﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Tomlyn.Model;

namespace Tomlyn.Benchmarks;

[MemoryDiagnoser(false)]
public class BasicBenchmark
{
    private const string TomlString = """
                             # This is a TOML document.

                             title = "TOML Example"

                             [owner]
                             name = "Tom Preston-Werner"
                             dob = 1979-05-27T07:32:00-08:00 # First class dates

                             [database]
                             server = "192.168.1.1"
                             ports = [ 8000, 8001, 8002 ]
                             connection_max = 5000
                             enabled = true

                             [servers]

                               # Indentation (tabs and/or spaces) is allowed but not required
                               [servers.alpha]
                               ip = "10.0.0.1"
                               dc = "eqdc10"

                               [servers.beta]
                               ip = "10.0.0.2"
                               dc = "eqdc10"

                             [clients]
                             data = [ ["gamma", "delta"], [1, 2] ]

                             # Line breaks are OK when inside arrays
                             hosts = [
                               "alpha",
                               "omega"
                             ]
                             """;

    private static readonly TomlTable TomlObject = Toml.ToModel(TomlString);

    [Benchmark]
    public TomlTable StringToModel() => Toml.ToModel(TomlString);

    [Benchmark]
    public string ModelToString() => Toml.FromModel(TomlObject);
}