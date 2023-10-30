// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests
{
    public class BasicTests
    {
        [Test]
        public void TestHelloWorld()
        {
            var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

            var model = Toml.ToModel(toml);
            // Prints "this is a string"
            var global = model["global"];
            Console.WriteLine($"found global = \"{global}\"");
            Assert.AreEqual("this is a string", global);
            // Prints 1
            var key = ((TomlTable)model["my_table"]!)["key"];
            Console.WriteLine($"found key = {key}");
            Assert.AreEqual(1L, key);
            // Check list
            var list = (TomlArray)((TomlTable)model["my_table"]!)["list"]!;
            Console.WriteLine($"found list = {string.Join(", ", list)}");
            Assert.AreEqual(new TomlArray() { 4, 5, 6 }, list);
        }

        [Test]
        [TestCase(7, 32, 0, 0)]
        [TestCase(7, 32, 0, 999)]
        [TestCase(0, 32, 0, 0)]
        public void TestLocalTime(int hour, int minute, int second, int millisecond)
        {
            var toml = $@"time = {hour:D2}:{minute:D2}:{second:D2}.{millisecond:D3}";
            var localTime = (TomlDateTime)Toml.ToModel(toml)["time"];

            Assert.AreEqual(hour, localTime.DateTime.Hour);
            Assert.AreEqual(minute, localTime.DateTime.Minute);
            Assert.AreEqual(second, localTime.DateTime.Second);
            Assert.AreEqual(millisecond, localTime.DateTime.Millisecond);
        }

        [Test]
        public void TestHelloWorldWithCustomModel()
        {
            var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

            var model = Toml.ToModel<MyModel>(toml);
            // Prints "this is a string"
            Console.WriteLine($"found global = \"{model.Global}\"");
            Assert.AreEqual("this is a string", model.Global);
            // Prints 1
            var key = model.MyTable!.Key;
            Console.WriteLine($"found key = {key}");
            Assert.AreEqual(1L, key);
            // Check list
            var list = model.MyTable!.ListOfIntegers;
            Console.WriteLine($"found list = {string.Join(", ", list)}");
            Assert.AreEqual(new List<int>() { 4, 5, 6 }, list);

            model.MyTable.ThisPropertyIsIgnored = "This should not be printed";

            var toml2 = Toml.FromModel(model);
            StandardTests.DisplayHeader("toml from model");
            Console.WriteLine(toml2);

            AssertHelper.AreEqualNormalizeNewLine(toml, toml2);
        }

        [Test]
        public void TestTableArraysContainingPrimitiveArraysSerialize()
        {
            var test = @"[[table_array]]
primitive_list = [4, 5, 6]
";

            var model = Toml.ToModel(test);
            var tomlOut = Toml.FromModel(model);

            Assert.AreEqual(test, tomlOut);
        }

        class MyModel : ITomlMetadataProvider
        {
            public string? Global { get; set; }

            public MyTable? MyTable { get; set; }

            /// <summary>
            /// Allows to store comments and whitespaces
            /// </summary>
            TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
        }

        class MyTable : ITomlMetadataProvider
        {
            public MyTable()
            {
                ListOfIntegers = new List<int>();
            }

            public int Key { get; set; }

            public bool Value { get; set; }

            [DataMember(Name = "list")]
            public List<int> ListOfIntegers { get; }

            [IgnoreDataMember]
            public string? ThisPropertyIsIgnored { get; set; }

            /// <summary>
            /// Allows to store comments and whitespaces
            /// </summary>
            TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
        }

        [Test]
        public void TestEmptyComment()
        {
            var input = "#\n";
            var doc = Toml.Parse(input);
            var docAsStr = doc.ToString();
            Assert.AreEqual(input, docAsStr);
        }

        [Test]
        public void SimpleTest()
        {
            var test = @"[table-1]
key1 = ""some string""    # This is a comment
key2 = 123
Key3 = true
Key4 = false
Key5 = +inf

[table-2]
key1 = ""another string""
key2 = 456
";
            var doc = Toml.Parse(test);
            Assert.AreEqual(test, doc.ToString());
        }
    }
}