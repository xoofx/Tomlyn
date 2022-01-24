// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests
{
    public class ModelTests
    {
        [Test]
        public static void TestPrimitives()
        {
            var input = @"a = 1
b = true
c = 1.0
d = 'yo'
e = 1980-01-20
";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            var model = syntax.ToModel();

            Assert.True(model.ContainsKey("a"));
            Assert.True(model.ContainsKey("b"));
            Assert.True(model.ContainsKey("c"));
            Assert.True(model.ContainsKey("d"));
            Assert.True(model.ContainsKey("e"));

            Assert.AreEqual(5, model.Count);
            Assert.AreEqual((long)1, model["a"]);
            Assert.AreEqual(true, model["b"]);
            Assert.AreEqual(1.0, model["c"]);
            Assert.AreEqual("yo", model["d"]);
            Assert.AreEqual(new DateTimeValue(1980, 01, 20), model["e"]);
        }

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
        public void TestShortDateKey()
        {
            var input = "2018-01-01=123";

            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            var model = syntax.ToModel();
            Assert.True(model.ContainsKey("2018-01-01"));
        }


        [Test]
        public void TestFullDateKey()
        {
            var input = "\"1987-07-05T17:45:00Z\"=123";

            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            var model = syntax.ToModel();
            Assert.True(model.ContainsKey("1987-07-05T17:45:00Z"));
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

        [Test]
        public static void TestNestedTableArray()
        {
            var input = @"[[fruit]]
  name = 'apple'

  [fruit.physical]
    color = 'red'
    shape = 'round'

  [[fruit.variety]]
    name = 'red delicious'

  [[fruit.variety]]
    name = 'granny smith'

[[fruit]]
  name = 'banana'

  [[fruit.variety]]
    name = 'plantain'
";

            var json = @"{
  ""fruit"": [
    {
      ""name"": ""apple"",
      ""physical"": {
        ""color"": ""red"",
        ""shape"": ""round""
      },
      ""variety"": [
        {
          ""name"": ""red delicious""
        },
        {
          ""name"": ""granny smith""
        }
      ]
    },
    {
      ""name"": ""banana"",
      ""variety"": [
        {
          ""name"": ""plantain""
        }
      ]
    }
  ]
}";
            AssertJson(input, json);
        }


        [Test]
        public void TestArrayAndInline()
        {
            var input = @"points = [ { x = 1, y = 2, z = 3 },
           { x = 7, y = 8, z = 9 },
           { x = 2, y = 4, z = 8 } ]";

            var json = @"{
  ""points"": [
    {
      ""x"": 1,
      ""y"": 2,
      ""z"": 3
    },
    {
      ""x"": 7,
      ""y"": 8,
      ""z"": 9
    },
    {
      ""x"": 2,
      ""y"": 4,
      ""z"": 8
    }
  ]
}";
            AssertJson(input, json);
        }

        [Test]
        public void TestImplicitAndExplicitTable()
        {
            var input = @"[a.b.c]
answer = 42

[a]
better = 43
";
            var json = @"{
  ""a"": {
    ""b"": {
      ""c"": {
        ""answer"": 42
      }
    },
    ""better"": 43
  }
}";
            AssertJson(input, json);
        }

        [Test]
        public void TestReflectionModel()
        {
            var input = @"name = ""this is a name""
values = [""a"", ""b"", ""c"", 1]

int_values = 1
int_value = 2
double_value = 2.5

[[sub]]
id = ""id1""
publish = true

[[sub]]
id = ""id2""
publish = false

[[sub]]
id = ""id3""";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            StandardTests.Dump(input, syntax, syntax.ToString());

            var model = syntax.ToModel<TestModel>();

            Assert.AreEqual("this is a name", model.Name);
            Assert.AreEqual(new List<string>() {"a", "b", "c", "1"}, model.Values);
            Assert.AreEqual(new List<int>() { 1 }, model.IntValues);
            Assert.AreEqual(2, model.IntValue);
            Assert.AreEqual(2.5, model.DoubleValue);
            Assert.AreEqual(3, model.SubModels.Count);
            var sub = model.SubModels[0];
            Assert.AreEqual("id1", sub.Id);
            Assert.True(sub.Publish);
            sub = model.SubModels[1];
            Assert.AreEqual("id2", sub.Id);
            Assert.False(sub.Publish);
            sub = model.SubModels[2];
            Assert.AreEqual("id3", sub.Id);
            Assert.False(sub.Publish);


            var result = Toml.ToString(model);


        }

        [Test]
        public void TestReflectionModelWithErrors()
        {
            var input = @"name = ""this is a name""
values = [""a"", ""b"", ""c"", 1]

int_values1 = 1  # error
int_value = 2
double_value = 2.5

[[sub]]
id2 = ""id1"" # error
publish = true

[[sub]]
id = ""id2""
publish = false

[[sub]]
id3 = ""id3"" # error
"; 
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            StandardTests.Dump(input, syntax, syntax.ToString());

            var result = syntax.TryToModel<TestModel>(out var model, out var diagnostics);

            Assert.False(result);
            Assert.NotNull(diagnostics);
            Assert.NotNull(model);

            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine(diagnostic);
            }

            // Expecting 3 errors
            Assert.AreEqual(3, diagnostics.Count);

            // The model is still partially valid
            Assert.AreEqual("this is a name", model.Name);

            var diag = diagnostics[0];
            Assert.AreEqual(3, diag.Span.Start.Line);
            StringAssert.Contains("int_values1", diag.Message);
            diag = diagnostics[1];
            Assert.AreEqual(8, diag.Span.Start.Line);
            StringAssert.Contains("id2", diag.Message);
            diag = diagnostics[2];
            Assert.AreEqual(16, diag.Span.Start.Line);
            StringAssert.Contains("id3", diag.Message);
        }

        private static void AssertJson(string input, string expectedJson)
        {
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            StandardTests.Dump(input, syntax, syntax.ToString());

            var model = syntax.ToModel();

            var jsonResult = ToJson(model);

            StandardTests.DisplayHeader("json");
            Console.WriteLine(jsonResult);

            AssertHelper.AreEqualNormalizeNewLine(expectedJson, jsonResult);

            StandardTests.DisplayHeader("toml");
            var toml = Toml.ToString(model);
            Console.WriteLine(toml);

            StandardTests.DisplayHeader("json2");
            var model2 = Toml.ParseToModel<TomlTable>(toml);
            var json2 = ToJson(model2);
            Console.WriteLine(json2);
            AssertHelper.AreEqualNormalizeNewLine(expectedJson, json2);
        }

        private static string ToJson(object model)
        {
            var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented });
            var writer = new StringWriter();
            serializer.Serialize(writer, model);
            return writer.ToString();
        }

        public class TestModel
        {
            public TestModel()
            {
                Values = new List<string>();
                SubModels = new List<TestSubModel>();
            }

            public string Name { get; set; }
            
            public List<string> Values { get; }

            public List<int> IntValues { get; set; }

            public int IntValue { get; set; }

            public double DoubleValue { get; set; }

            [JsonPropertyName("sub")]
            public List<TestSubModel> SubModels { get; }
        }

        public class TestSubModel
        {
            public string Id { get; set; }

            public bool Publish { get; set; }
        }
    }
}