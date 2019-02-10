// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using NUnit.Framework;
using SharpToml.Model;

namespace SharpToml.Tests
{
    public class ModelTests
    {
        [Test]
        public static void TestBasic()
        {
            var input = @"a = 1
b = true
c.d = 'yo'
";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            var model = syntax.ToModel();

            Assert.True(model.ContainsKey("a"));
            Assert.True(model.ContainsKey("b"));
            Assert.True(model.ContainsKey("c"));
            Assert.AreEqual(3, model.Count);
            Assert.AreEqual((long)1, model["a"]);
            Assert.AreEqual(true, model["b"]);
            Assert.IsInstanceOf<TomlTable>(model["c"]);
            var subTable = ((TomlTable) model["c"]);
            Assert.True(subTable.ContainsKey("d"));
            Assert.AreEqual(1, subTable.Count);
            Assert.AreEqual("yo", subTable["d"]);
        }

        [Test]
        public static void TestTable()
        {
            var input = @"a = 1
[b]
c = 1
d = true
";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            var model = syntax.ToModel();

            Assert.True(model.ContainsKey("a"));
            Assert.True(model.ContainsKey("b"));
            Assert.AreEqual(2, model.Count);
            Assert.AreEqual((long)1, model["a"]);
            Assert.IsInstanceOf<TomlTable>(model["b"]);
            var subTable = ((TomlTable)model["b"]);
            Assert.True(subTable.ContainsKey("c"));
            Assert.True(subTable.ContainsKey("d"));
            Assert.AreEqual(2, subTable.Count);
            Assert.AreEqual((long)1, subTable["c"]);
            Assert.AreEqual((bool)true, subTable["d"]);
        }


        [Test]
        public static void TestTableArray()
        {
            var input = @"a = 1
[[b]]
a = 1
b = true
[[b]]
c = 1
d = true
";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            var model = syntax.ToModel();

            Assert.True(model.ContainsKey("a"));
            Assert.True(model.ContainsKey("b"));
            Assert.AreEqual(2, model.Count);
            Assert.AreEqual((long)1, model["a"]);
            Assert.IsInstanceOf<TomlTableArray>(model["b"]);
            var tableArray = ((TomlTableArray)model["b"]);
            Assert.AreEqual(2, tableArray.Count);

            // first [[b]]
            Assert.IsInstanceOf<TomlTable>(tableArray[0]);
            var bTable0 = tableArray[0];
            Assert.True(bTable0.ContainsKey("a"));
            Assert.True(bTable0.ContainsKey("b"));
            Assert.AreEqual((long)1, bTable0["a"]);
            Assert.AreEqual((bool)true, bTable0["b"]);

            // second [[b]]
            Assert.IsInstanceOf<TomlTable>(tableArray[1]);
            var bTable1 = tableArray[1];
            Assert.True(bTable1.ContainsKey("c"));
            Assert.True(bTable1.ContainsKey("d"));
            Assert.AreEqual((long)1, bTable1["c"]);
            Assert.AreEqual((bool)true, bTable1["d"]);
        }



    }
}