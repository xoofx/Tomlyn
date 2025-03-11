// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests;

public class SerializationTests
{
    [Test]
    public void TestCrlfInMultilineString()
    {
        var model = new TomlTable
        {
            PropertiesMetadata = new TomlPropertiesMetadata()
        };
        model.PropertiesMetadata.SetProperty("property", new TomlPropertyMetadata
        {
            DisplayKind = TomlPropertyDisplayKind.StringLiteralMulti
        });

        model["property"] = "string\r\nwith\r\nnewlines";

        Assert.AreEqual("property = '''string\r\nwith\r\nnewlines'''" + Environment.NewLine, Toml.FromModel(model));
    }

    [Test]
    public void TestArrayWithPrimitives()
    {
        var model = new TomlTable()
        {
            ["mixed-array"] = new TomlArray()
            {
                new TomlTable() { ["a"] = 1 },
                2,  // If instead the second or final item in the array was the table, it works as expected.
                3
            }
        };

        var result = Toml.FromModel(model).ReplaceLineEndings("\n").Trim();
        Console.WriteLine(result);
        Assert.AreEqual("mixed-array = [{a = 1}, 2, 3]", result);
    }
}