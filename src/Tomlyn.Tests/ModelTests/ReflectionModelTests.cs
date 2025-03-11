// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests
{
    public class ModelTests
    {
        [Test]
        public void TestSampleModel()
        {
            var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

            // Converts the TOML string to a `TomlTable`
            var model = Toml.ToModel(toml);
            Assert.AreEqual("this is a string", model["global"]);
        }

        [Test]
        public void TestUri()
        {
            var expected = new Uri("https://example.com");
            var text = @$"service_uri = ""{expected}""";
            var options = new TomlModelOptions
            {
                ConvertToModel = (value, type) => type == typeof(Uri) ? new Uri((string)value) : null,
                ConvertToToml = (value) => value is Uri uri ? uri.ToString() : null
            };
            var success = Toml.TryToModel<TestConfigWithUri>(text, out var result, out var diagnostics, null, options);
            
            Assert.True(success);
            Assert.AreEqual(expected, result?.ServiceUri);

            var tomlText = Toml.FromModel(result!, options);
            Assert.AreEqual($"service_uri = \"{expected}\"", tomlText.Trim());
        }

        private class TestConfigWithUri
        {
            public Uri? ServiceUri { get; set; }
        }

        [Test]
        public void TestEnum()
        {
            var text = @$"enum_value = ""EnumValue1""";
            var result = Toml.ToModel<TestConfigWithEnum>(text);
            Assert.AreEqual(EnumType.EnumValue1, result.EnumValue);

            var tomlText = Toml.FromModel(result);
            Assert.AreEqual(text, tomlText.TrimEnd());
        }

        private enum EnumType
        {
            EnumValue0,
            EnumValue1,
        }

        private class TestConfigWithEnum
        {
            public EnumType EnumValue { get; set; }
        }

        /// <summary>
        /// Serialize back and forth all integer/float primitives.
        /// </summary>
        [Test]
        public void TestPrimitives()
        {
            var model = new PrimitiveModel()
            {
                Int8Value = 1,
                Int16Value = 2,
                Int32Value = 3,
                Int64Value = 4,
                UInt8Value = 5,
                UInt16Value = 6,
                UInt32Value = 7,
                UInt64Value = 8,
                Float32Value = 2.5f,
                Float64Value = 2.5,
                DateTime = new DateTime(1970, 1, 1),
                DateTimeOffset = new DateTimeOffset(1980, 1, 1, 0, 23, 1, TimeSpan.FromHours(-2)),
#if NET6_0_OR_GREATER
                DateOnly = new DateOnly(1970, 5, 27),
                TimeOnly = new TimeOnly(7, 32, 0, 999),
#endif
                TomlDateTime = new TomlDateTime(new DateTimeOffset(new DateTime(1990, 11, 15)), 0, TomlDateTimeKind.LocalDateTime)
            };

            StandardTests.DisplayHeader("validation 1");
            ValidateModel(model);

            StandardTests.DisplayHeader("validation 2");
            model.Int8Value = sbyte.MinValue;
            model.Int16Value = short.MinValue;
            model.Int32Value = int.MinValue;
            model.Int64Value = long.MinValue;
            model.UInt8Value = byte.MaxValue;
            model.UInt16Value = ushort.MaxValue;
            model.UInt32Value = uint.MaxValue;
            model.UInt64Value = ulong.MaxValue;
            model.Float32Value = float.PositiveInfinity;
            model.Float64Value = double.PositiveInfinity;
            ValidateModel(model);

            static void ValidateModel(PrimitiveModel model)
            {
                var toml = Toml.FromModel(model);
                StandardTests.DisplayHeader("toml from model");
                Console.WriteLine(toml);
                var model2 = Toml.ToModel<PrimitiveModel>(toml);

                Console.WriteLine($"From Model:  {model}");
                Console.WriteLine($"From Model2: {model2}");

                Assert.AreEqual(model.ToString(), model2.ToString());
            }
        }

        /// <summary>
        /// Serialize back and forth all integer/float primitives with fields.
        /// </summary>
        [Test]
        public void TestPrimitiveFields()
        {
            var model = new PrimitiveFieldsModel()
            {
                Int8Value = 1,
                Int16Value = 2,
                Int32Value = 3,
                Int64Value = 4,
                UInt8Value = 5,
                UInt16Value = 6,
                UInt32Value = 7,
                UInt64Value = 8,
                Float32Value = 2.5f,
                Float64Value = 2.5,
                DateTime = new DateTime(1970, 1, 1),
                DateTimeOffset = new DateTimeOffset(1980, 1, 1, 0, 23, 1, TimeSpan.FromHours(-2)),
#if NET6_0_OR_GREATER
                DateOnly = new DateOnly(1970, 5, 27),
                TimeOnly = new TimeOnly(7, 32, 0, 999),
#endif
                TomlDateTime = new TomlDateTime(new DateTimeOffset(new DateTime(1990, 11, 15)), 0, TomlDateTimeKind.LocalDateTime)
            };

            StandardTests.DisplayHeader("validation 1");
            ValidateModel(model);

            StandardTests.DisplayHeader("validation 2");
            model.Int8Value = sbyte.MinValue;
            model.Int16Value = short.MinValue;
            model.Int32Value = int.MinValue;
            model.Int64Value = long.MinValue;
            model.UInt8Value = byte.MaxValue;
            model.UInt16Value = ushort.MaxValue;
            model.UInt32Value = uint.MaxValue;
            model.UInt64Value = ulong.MaxValue;
            model.Float32Value = float.PositiveInfinity;
            model.Float64Value = double.PositiveInfinity;
            ValidateModel(model);

            static void ValidateModel(PrimitiveFieldsModel model)
            {
                var options = new TomlModelOptions();
                options.IncludeFields = true;

                var toml = Toml.FromModel(model, options: options);
                StandardTests.DisplayHeader("toml from model");
                Console.WriteLine(toml);
                var model2 = Toml.ToModel<PrimitiveModel>(toml, options: options);

                Console.WriteLine($"From Model:  {model}");
                Console.WriteLine($"From Model2: {model2}");

                Assert.AreEqual(model.ToString(), model2.ToString());
            }
        }

        /// <summary>
        /// Serialize back and forth all numeric and struct primitives as nullable value types.
        /// </summary>
        [Test]
        public void TestNullableValueTypes()
        {
            var model = new NullableValueTypesModel()
            {
                Int8Value = 1,
                Int16Value = 2,
                Int32Value = 3,
                Int64Value = 4,
                UInt8Value = 5,
                UInt16Value = 6,
                UInt32Value = 7,
                UInt64Value = 8,
                Float32Value = 2.5f,
                Float64Value = 2.5,
                DateTime = new DateTime(1970, 1, 1),
                DateTimeOffset = new DateTimeOffset(1980, 1, 1, 0, 23, 1, TimeSpan.FromHours(-2)),
#if NET6_0_OR_GREATER
                DateOnly = new DateOnly(1970, 5, 27),
                TimeOnly = new TimeOnly(7, 32, 0, 999),
#endif
                TomlDateTime = new TomlDateTime(new DateTimeOffset(new DateTime(1990, 11, 15)), 0, TomlDateTimeKind.LocalDateTime)
            };

            StandardTests.DisplayHeader("validation 1");
            ValidateModel(model);

            StandardTests.DisplayHeader("validation 2");
            model.Int8Value = sbyte.MinValue;
            model.Int16Value = short.MinValue;
            model.Int32Value = int.MinValue;
            model.Int64Value = long.MinValue;
            model.UInt8Value = byte.MaxValue;
            model.UInt16Value = ushort.MaxValue;
            model.UInt32Value = uint.MaxValue;
            model.UInt64Value = ulong.MaxValue;
            model.Float32Value = float.PositiveInfinity;
            model.Float64Value = double.PositiveInfinity;
            ValidateModel(model);

            StandardTests.DisplayHeader("validation 3");
            model.Int8Value = null;
            model.Int16Value = null;
            model.Int32Value = null;
            model.Int64Value = null;
            model.UInt8Value = null;
            model.UInt16Value = null;
            model.UInt32Value = null;
            model.UInt64Value = null;
            model.Float32Value = null;
            model.Float64Value = null;
            model.DateTime = null;
            model.DateTimeOffset = null;
            model.DateOnly = null;
            model.TimeOnly = null;
            model.TomlDateTime = null;
            ValidateModel(model);

            static void ValidateModel(NullableValueTypesModel model)
            {
                var toml = Toml.FromModel(model);
                StandardTests.DisplayHeader("toml from model");
                Console.WriteLine(toml);
                var model2 = Toml.ToModel<NullableValueTypesModel>(toml);

                Console.WriteLine($"From Model:  {model}");
                Console.WriteLine($"From Model2: {model2}");

                Assert.AreEqual(model.ToString(), model2.ToString());
            }
        }

        /// <summary>
        /// Serialize back and forth all numeric and struct primitives as nullable value type fieldss.
        /// </summary>
        [Test]
        public void TestNullableValueTypeFields()
        {
            var model = new NullableValueTypeFieldsModel()
            {
                Int8Value = 1,
                Int16Value = 2,
                Int32Value = 3,
                Int64Value = 4,
                UInt8Value = 5,
                UInt16Value = 6,
                UInt32Value = 7,
                UInt64Value = 8,
                Float32Value = 2.5f,
                Float64Value = 2.5,
                DateTime = new DateTime(1970, 1, 1),
                DateTimeOffset = new DateTimeOffset(1980, 1, 1, 0, 23, 1, TimeSpan.FromHours(-2)),
#if NET6_0_OR_GREATER
                DateOnly = new DateOnly(1970, 5, 27),
                TimeOnly = new TimeOnly(7, 32, 0, 999),
#endif
                TomlDateTime = new TomlDateTime(new DateTimeOffset(new DateTime(1990, 11, 15)), 0, TomlDateTimeKind.LocalDateTime)
            };

            StandardTests.DisplayHeader("validation 1");
            ValidateModel(model);

            StandardTests.DisplayHeader("validation 2");
            model.Int8Value = sbyte.MinValue;
            model.Int16Value = short.MinValue;
            model.Int32Value = int.MinValue;
            model.Int64Value = long.MinValue;
            model.UInt8Value = byte.MaxValue;
            model.UInt16Value = ushort.MaxValue;
            model.UInt32Value = uint.MaxValue;
            model.UInt64Value = ulong.MaxValue;
            model.Float32Value = float.PositiveInfinity;
            model.Float64Value = double.PositiveInfinity;
            ValidateModel(model);

            StandardTests.DisplayHeader("validation 3");
            model.Int8Value = null;
            model.Int16Value = null;
            model.Int32Value = null;
            model.Int64Value = null;
            model.UInt8Value = null;
            model.UInt16Value = null;
            model.UInt32Value = null;
            model.UInt64Value = null;
            model.Float32Value = null;
            model.Float64Value = null;
            model.DateTime = null;
            model.DateTimeOffset = null;
            model.DateOnly = null;
            model.TimeOnly = null;
            model.TomlDateTime = null;
            ValidateModel(model);

            static void ValidateModel(NullableValueTypeFieldsModel model)
            {
                var options = new TomlModelOptions();
                options.IncludeFields = true;

                var toml = Toml.FromModel(model, options: options);
                StandardTests.DisplayHeader("toml from model");
                Console.WriteLine(toml);
                var model2 = Toml.ToModel<NullableValueTypesModel>(toml, options: options);

                Console.WriteLine($"From Model:  {model}");
                Console.WriteLine($"From Model2: {model2}");

                Assert.AreEqual(model.ToString(), model2.ToString());
            }
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

            var model = syntax.ToModel<SimpleModel>();

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

            model.SubModels[2].Value = 127;

            var toml = Toml.FromModel(model);
            StandardTests.DisplayHeader("toml from model");
            Console.WriteLine(toml);

            var model2 = Toml.ToModel(toml);
            var result = (model2["sub"] as TomlTableArray)?[2]?["value"];
            Assert.AreEqual(127, result);
        }

        [Test]
        public void TestReflectionFieldsModel()
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

            var options = new TomlModelOptions();
            options.IncludeFields = true;

            var model = syntax.ToModel<SimpleFieldsModel>(options);

            Assert.AreEqual("this is a name", model.Name);
            Assert.AreEqual(new List<string>() { "a", "b", "c", "1" }, model.Values);
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

            model.SubModels[2].Value = 127;

            var toml = Toml.FromModel(model, options: options);
            StandardTests.DisplayHeader("toml from model");
            Console.WriteLine(toml);

            var model2 = Toml.ToModel(toml, options: options);
            var result = (model2["sub"] as TomlTableArray)?[2]?["value"];
            Assert.AreEqual(127, result);
        }

        [Test]
        public void TestReflectionModelWithConvertName()
        {
            var input = @"Name = ""this is a name""
Values = [""a"", ""b"", ""c"", 1]

IntValues = 1
IntValue = 2
DoubleValue = 2.5

[[sub]]
Id = ""id1""
Publish = true

[[sub]]
Id = ""id2""
Publish = false

[[sub]]
Id = ""id3""";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            StandardTests.Dump(input, syntax, syntax.ToString());

            var options = new TomlModelOptions() { ConvertPropertyName = name => name };


            var model = syntax.ToModel<SimpleModel>(options);

            Assert.AreEqual("this is a name", model.Name);
            Assert.AreEqual(new List<string>() { "a", "b", "c", "1" }, model.Values);
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

            model.SubModels[2].Value = 127;

            var toml = Toml.FromModel(model, options);
            StandardTests.DisplayHeader("toml from model");
            Console.WriteLine(toml);

            var model2 = Toml.ToModel(toml, options: options);
            var result = (model2["sub"] as TomlTableArray)?[2]?["Value"];
            Assert.AreEqual(127, result);
        }

        [Test]
        public void TestReflectionFieldsModelWithConvertName()
        {
            var input = @"Name = ""this is a name""
Values = [""a"", ""b"", ""c"", 1]

IntValues = 1
IntValue = 2
DoubleValue = 2.5

[[sub]]
Id = ""id1""
Publish = true

[[sub]]
Id = ""id2""
Publish = false

[[sub]]
Id = ""id3""";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            StandardTests.Dump(input, syntax, syntax.ToString());

            var options = new TomlModelOptions() { ConvertPropertyName = name => name, ConvertFieldName = name => name, IncludeFields = true};

            var model = syntax.ToModel<SimpleFieldsModel>(options);

            Assert.AreEqual("this is a name", model.Name);
            Assert.AreEqual(new List<string>() { "a", "b", "c", "1" }, model.Values);
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

            model.SubModels[2].Value = 127;

            var toml = Toml.FromModel(model, options);
            StandardTests.DisplayHeader("toml from model");
            Console.WriteLine(toml);

            var model2 = Toml.ToModel(toml, options: options);
            var result = (model2["sub"] as TomlTableArray)?[2]?["Value"];
            Assert.AreEqual(127, result);
        }

        [Test]
        public void TestReflectionModelWithInlineTable()
        {
            var input = @"name = ""this is a name""
values = [""a"", ""b"", ""c"", 1]
intvalues = 1
intvalue = 2
doublevalue = 2.5
sub = [ { id = ""id1"", publish = true }, { id = ""id2"", publish = false }, { id = ""id3"" } ]
";
            var syntax = Toml.Parse(input);
            Assert.False(syntax.HasErrors, "The document should not have any errors");

            StandardTests.Dump(input, syntax, syntax.ToString());


            var options = new TomlModelOptions() { ConvertPropertyName = name => name.ToLowerInvariant(), ConvertFieldName = name => name.ToLowerInvariant() };
            var model = syntax.ToModel<SimpleModel>(options);

            Assert.AreEqual("this is a name", model.Name);
            Assert.AreEqual(new List<string>() { "a", "b", "c", "1" }, model.Values);
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

            model.SubModels[2].Value = 127;

            var toml = Toml.FromModel(model);
            StandardTests.DisplayHeader("toml from model");
            Console.WriteLine(toml);

            var model2 = Toml.ToModel(toml);
            var result = (model2["sub"] as TomlTableArray)?[2]?["value"];
            Assert.AreEqual(127, result);
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

            var result = syntax.TryToModel<SimpleModel>(out var model, out var diagnostics);

            foreach (var message in diagnostics)
            {
                Console.WriteLine(message);
            }

            Assert.False(result);
            Assert.NotNull(model);

            // Expecting 3 errors
            Assert.AreEqual(3, diagnostics.Count);

            Debug.Assert(model is not null);
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

        [Test]
        public void TestReflectionFieldsModelWithErrors()
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

            var options = new TomlModelOptions();
            options.IncludeFields = true;

            var result = syntax.TryToModel<SimpleFieldsModel>(out var model, out var diagnostics, options: options);

            foreach (var message in diagnostics)
            {
                Console.WriteLine(message);
            }

            Assert.False(result);
            Assert.NotNull(model);

            // Expecting 3 errors
            Assert.AreEqual(3, diagnostics.Count);

            Debug.Assert(model is not null);
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

        [Test]
        public void TestCommentRoundtripWithModel()
        {
            var input = @"
# This is comment before
name = 1 # This is a comment

# This is a comment before a table
[table] # a comment after an table
key = 1

# A Comment before a table array
[[array]] # a comment after a table array
key2 = 3
# This is a comment after a key
".ReplaceLineEndings();

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);
            var model = Toml.ToModel(input);
            var toml2 = Toml.FromModel(model);
            StandardTests.DisplayHeader("toml - write back");
            Console.WriteLine(toml2);

            Assert.AreEqual(input, toml2);
        }

        [Test]
        public void TestModelWithSpecialDictionary()
        {
            var input = @"[values]
[values.test]
a = 1
b = true
".ReplaceLineEndings().Trim();

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);
            var model = Toml.ToModel<ModelWithSpecialDictionary>(input);

            Assert.True(model.Values.ContainsKey("test"));

            var test = model.Values["test"];

            Assert.True(test.ContainsKey("a"));
            Assert.True(test.ContainsKey("b"));

            Assert.AreEqual(1L, test["a"]);
            Assert.AreEqual(true, test["b"]);

            var output = Toml.FromModel(model).ReplaceLineEndings().Trim();
            StandardTests.DisplayHeader("output");
            Console.WriteLine(output);

            Assert.AreEqual(output, input);
        }

        [Test]
        public void TestModelWithArray()
        {
            var input = @"values = ['1', '2', '3']";

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);
            var model = Toml.ToModel<ModelWithArray>(input);
            CollectionAssert.AreEqual(new string[] { "1", "2", "3"}, model.Values);
        }

        [Test]
        public void TestModelWithArrayField()
        {
            var input = @"values = ['1', '2', '3']";

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);

            var options = new TomlModelOptions();
            options.IncludeFields = true;

            var model = Toml.ToModel<ModelWithArray>(input, options: options);
            CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, model.Values);
        }

        [Test]
        public void TestModelWithFixedList()
        {
            var input = @"values = ['1', '2', '3']";

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);
            var model = Toml.ToModel<ModelWithFixedList>(input);
            CollectionAssert.AreEqual(new List<string> { "1", "2", "3" }, model.Values);
        }

        [Test]
        public void TestModelWithFixedListField()
        {
            var input = @"values = ['1', '2', '3']";

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);

            var options = new TomlModelOptions();
            options.IncludeFields = true;
            var model = Toml.ToModel<ModelWithFixedListField>(input, options: options);
            CollectionAssert.AreEqual(new List<string> { "1", "2", "3" }, model.Values);
        }

        [Test]
        public void TestModelWithMissingProperties()
        {
            var input = @"values = ['1', '2', '3']
some_thing_that_doesnt_exist = true
[object_that_doesnt_exist]
required = true";

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);
            var model = Toml.ToModel<SimpleModel>(input, options: new TomlModelOptions
            {
                IgnoreMissingProperties = true,
            });
            CollectionAssert.AreEqual(new List<string> { "1", "2", "3" }, model.Values);

            Assert.Throws<TomlException>(() => Toml.ToModel<SimpleModel>(input));

        }

        [Test]
        public void TestFieldsModelWithMissingProperties()
        {
            var input = @"values = ['1', '2', '3']
some_thing_that_doesnt_exist = true
[object_that_doesnt_exist]
required = true";

            StandardTests.DisplayHeader("input");
            Console.WriteLine(input);
            var model = Toml.ToModel<SimpleFieldsModel>(input, options: new TomlModelOptions
            {
                IgnoreMissingProperties = true,
                IncludeFields = true
            });
            CollectionAssert.AreEqual(new List<string> { "1", "2", "3" }, model.Values);

            Assert.Throws<TomlException>(() => Toml.ToModel<SimpleFieldsModel>(input, options: new TomlModelOptions
            {
                IgnoreMissingProperties = false,
                IncludeFields = true
            }));

        }

        public class SimpleModel
        {
            public SimpleModel()
            {
                Values = new List<string>();
                SubModels = new List<SimpleSubModel>();
            }

            public string? Name { get; set; }

            public List<string> Values { get; }

            public List<int>? IntValues { get; set; }

            public int IntValue { get; set; }

            public double DoubleValue { get; set; }

            [JsonPropertyName("sub")]
            public List<SimpleSubModel> SubModels { get; }
        }

        public class SimpleFieldsModel
        {
            public SimpleFieldsModel()
            {
                Values = new List<string>();
                SubModels = new List<SimpleSubModel>();
            }

            public string? Name { get; set; }

            public List<string> Values;

            public List<int>? IntValues;

            public int IntValue { get; set; }

            public double DoubleValue { get; set; }

            [JsonPropertyName("sub")]
            public List<SimpleSubModel> SubModels;
        }

        public class ModelWithArray
        {
            public string[]? Values { get; set; }
        }


        public class ModelWithArrayField
        {
            public string[]? Values;
        }

        public class ModelWithFixedList
        {
            public ModelWithFixedList()
            {
                Values = new List<string>();
            }

            public List<string> Values { get; }
        }

        public class ModelWithFixedListField
        {
            public ModelWithFixedListField()
            {
                Values = new List<string>();
            }

            public List<string> Values;
        }


        public class SimpleSubModel
        {
            public string? Id { get; set; }

            public bool Publish { get; set; }

            public int? Value { get; set; }
        }


        public class ModelWithSpecialDictionary
        {
            public ModelWithSpecialDictionary()
            {
                Values = new Dictionary<string, Dictionary<string, object>>();
            }

            public Dictionary<string, Dictionary<string, object>> Values { get; }
        }

        public class PrimitiveModel : IEquatable<PrimitiveModel>
        {
            public sbyte Int8Value { get; set; }
            public short Int16Value { get; set; }
            public int Int32Value { get; set; }
            public long Int64Value { get; set; }
            public byte UInt8Value { get; set; }
            public ushort UInt16Value { get; set; }
            public uint UInt32Value { get; set; }
            public ulong UInt64Value { get; set; }
            public float Float32Value { get; set; }
            public double Float64Value { get; set; }
            public DateTime DateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public DateOnly DateOnly { get; set; }
            public TimeOnly TimeOnly { get; set; }
            public TomlDateTime TomlDateTime { get; set; }

            public bool Equals(PrimitiveModel? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Int8Value == other.Int8Value && Int16Value == other.Int16Value && Int32Value == other.Int32Value && Int64Value == other.Int64Value && UInt8Value == other.UInt8Value && UInt16Value == other.UInt16Value && UInt32Value == other.UInt32Value && UInt64Value == other.UInt64Value && Float32Value.Equals(other.Float32Value) && Float64Value.Equals(other.Float64Value) && DateTime.Equals(other.DateTime) && DateTimeOffset.Equals(other.DateTimeOffset) && DateOnly.Equals(other.DateOnly) && TimeOnly.Equals(other.TimeOnly) && TomlDateTime.Equals(other.TomlDateTime);
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PrimitiveModel) obj);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(Int8Value);
                hashCode.Add(Int16Value);
                hashCode.Add(Int32Value);
                hashCode.Add(Int64Value);
                hashCode.Add(UInt8Value);
                hashCode.Add(UInt16Value);
                hashCode.Add(UInt32Value);
                hashCode.Add(UInt64Value);
                hashCode.Add(Float32Value);
                hashCode.Add(Float64Value);
                hashCode.Add(DateTime);
                hashCode.Add(DateTimeOffset);
                hashCode.Add(DateOnly);
                hashCode.Add(TimeOnly);
                hashCode.Add(TomlDateTime);
                return hashCode.ToHashCode();
            }

            public override string ToString()
            {
                return $"{nameof(Int8Value)}: {Int8Value}, {nameof(Int16Value)}: {Int16Value}, {nameof(Int32Value)}: {Int32Value}, {nameof(Int64Value)}: {Int64Value}, {nameof(UInt8Value)}: {UInt8Value}, {nameof(UInt16Value)}: {UInt16Value}, {nameof(UInt32Value)}: {UInt32Value}, {nameof(UInt64Value)}: {UInt64Value}, {nameof(Float32Value)}: {Float32Value}, {nameof(Float64Value)}: {Float64Value}, {nameof(DateTime)}: {DateTime.ToUniversalTime()}, {nameof(DateTimeOffset)}: {DateTimeOffset.ToUniversalTime()}, {nameof(DateOnly)}: {DateOnly}, {nameof(TimeOnly)}: {TimeOnly}, {nameof(TomlDateTime)}: {TomlDateTime}";
            }
        }

        public class PrimitiveFieldsModel : IEquatable<PrimitiveFieldsModel>
        {
            public sbyte Int8Value;
            public short Int16Value;
            public int Int32Value;
            public long Int64Value;
            public byte UInt8Value;
            public ushort UInt16Value;
            public uint UInt32Value;
            public ulong UInt64Value;
            public float Float32Value;
            public double Float64Value;
            public DateTime DateTime;
            public DateTimeOffset DateTimeOffset;
            public DateOnly DateOnly;
            public TimeOnly TimeOnly;
            public TomlDateTime TomlDateTime;

            public bool Equals(PrimitiveFieldsModel? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Int8Value == other.Int8Value && Int16Value == other.Int16Value && Int32Value == other.Int32Value && Int64Value == other.Int64Value && UInt8Value == other.UInt8Value && UInt16Value == other.UInt16Value && UInt32Value == other.UInt32Value && UInt64Value == other.UInt64Value && Float32Value.Equals(other.Float32Value) && Float64Value.Equals(other.Float64Value) && DateTime.Equals(other.DateTime) && DateTimeOffset.Equals(other.DateTimeOffset) && DateOnly.Equals(other.DateOnly) && TimeOnly.Equals(other.TimeOnly) && TomlDateTime.Equals(other.TomlDateTime);
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PrimitiveFieldsModel)obj);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(Int8Value);
                hashCode.Add(Int16Value);
                hashCode.Add(Int32Value);
                hashCode.Add(Int64Value);
                hashCode.Add(UInt8Value);
                hashCode.Add(UInt16Value);
                hashCode.Add(UInt32Value);
                hashCode.Add(UInt64Value);
                hashCode.Add(Float32Value);
                hashCode.Add(Float64Value);
                hashCode.Add(DateTime);
                hashCode.Add(DateTimeOffset);
                hashCode.Add(DateOnly);
                hashCode.Add(TimeOnly);
                hashCode.Add(TomlDateTime);
                return hashCode.ToHashCode();
            }

            public override string ToString()
            {
                return $"{nameof(Int8Value)}: {Int8Value}, {nameof(Int16Value)}: {Int16Value}, {nameof(Int32Value)}: {Int32Value}, {nameof(Int64Value)}: {Int64Value}, {nameof(UInt8Value)}: {UInt8Value}, {nameof(UInt16Value)}: {UInt16Value}, {nameof(UInt32Value)}: {UInt32Value}, {nameof(UInt64Value)}: {UInt64Value}, {nameof(Float32Value)}: {Float32Value}, {nameof(Float64Value)}: {Float64Value}, {nameof(DateTime)}: {DateTime.ToUniversalTime()}, {nameof(DateTimeOffset)}: {DateTimeOffset.ToUniversalTime()}, {nameof(DateOnly)}: {DateOnly}, {nameof(TimeOnly)}: {TimeOnly}, {nameof(TomlDateTime)}: {TomlDateTime}";
            }
        }

        public class NullableValueTypesModel : IEquatable<NullableValueTypesModel>
        {
            public sbyte? Int8Value { get; set; }
            public short? Int16Value { get; set; }
            public int? Int32Value { get; set; }
            public long? Int64Value { get; set; }
            public byte? UInt8Value { get; set; }
            public ushort? UInt16Value { get; set; }
            public uint? UInt32Value { get; set; }
            public ulong? UInt64Value { get; set; }
            public float? Float32Value { get; set; }
            public double? Float64Value { get; set; }
            public DateTime? DateTime { get; set; }
            public DateTimeOffset? DateTimeOffset { get; set; }
            public DateOnly? DateOnly { get; set; }
            public TimeOnly? TimeOnly { get; set; }
            public TomlDateTime? TomlDateTime { get; set; }

            public bool Equals(NullableValueTypesModel? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Int8Value == other.Int8Value
                    && Int16Value == other.Int16Value
                    && Int32Value == other.Int32Value
                    && Int64Value == other.Int64Value
                    && UInt8Value == other.UInt8Value
                    && UInt16Value == other.UInt16Value
                    && UInt32Value == other.UInt32Value
                    && UInt64Value == other.UInt64Value
                    && ((!Float32Value.HasValue && !other.Float32Value.HasValue) || Float32Value.Equals(other.Float32Value))
                    && ((!Float64Value.HasValue && !other.Float64Value.HasValue) || Float64Value.Equals(other.Float64Value))
                    && ((!DateTime.HasValue && !other.DateTime.HasValue) || DateTime.Equals(other.DateTime))
                    && ((!DateTimeOffset.HasValue && !other.DateTimeOffset.HasValue) || DateTimeOffset.Equals(other.DateTimeOffset))
                    && ((!DateOnly.HasValue && !other.DateOnly.HasValue) || DateOnly.Equals(other.DateOnly))
                    && ((!TimeOnly.HasValue && !other.TimeOnly.HasValue) || TimeOnly.Equals(other.TimeOnly))
                    && ((!TomlDateTime.HasValue && !other.TomlDateTime.HasValue) || TomlDateTime.Equals(other.TomlDateTime));
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PrimitiveModel) obj);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(Int8Value);
                hashCode.Add(Int16Value);
                hashCode.Add(Int32Value);
                hashCode.Add(Int64Value);
                hashCode.Add(UInt8Value);
                hashCode.Add(UInt16Value);
                hashCode.Add(UInt32Value);
                hashCode.Add(UInt64Value);
                hashCode.Add(Float32Value);
                hashCode.Add(Float64Value);
                hashCode.Add(DateTime);
                hashCode.Add(DateTimeOffset);
                hashCode.Add(DateOnly);
                hashCode.Add(TimeOnly);
                hashCode.Add(TomlDateTime);
                return hashCode.ToHashCode();
            }

            public override string ToString()
            {
                return $"{nameof(Int8Value)}: {Int8Value}, {nameof(Int16Value)}: {Int16Value}, {nameof(Int32Value)}: {Int32Value}, {nameof(Int64Value)}: {Int64Value}, {nameof(UInt8Value)}: {UInt8Value}, {nameof(UInt16Value)}: {UInt16Value}, {nameof(UInt32Value)}: {UInt32Value}, {nameof(UInt64Value)}: {UInt64Value}, {nameof(Float32Value)}: {Float32Value}, {nameof(Float64Value)}: {Float64Value}, {nameof(DateTime)}: {(DateTime.HasValue ? DateTime.Value.ToUniversalTime() : "null")}, {nameof(DateTimeOffset)}: {(DateTimeOffset.HasValue ? DateTimeOffset.Value.ToUniversalTime() : "null")}, {nameof(DateOnly)}: {DateOnly}, {nameof(TimeOnly)}: {TimeOnly}, {nameof(TomlDateTime)}: {TomlDateTime}";
            }
        }

        public class NullableValueTypeFieldsModel : IEquatable<NullableValueTypeFieldsModel>
        {
            public sbyte? Int8Value;
            public short? Int16Value;
            public int? Int32Value;
            public long? Int64Value;
            public byte? UInt8Value;
            public ushort? UInt16Value;
            public uint? UInt32Value;
            public ulong? UInt64Value;
            public float? Float32Value;
            public double? Float64Value;
            public DateTime? DateTime;
            public DateTimeOffset? DateTimeOffset;
            public DateOnly? DateOnly;
            public TimeOnly? TimeOnly;
            public TomlDateTime? TomlDateTime;

            public bool Equals(NullableValueTypeFieldsModel? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Int8Value == other.Int8Value
                    && Int16Value == other.Int16Value
                    && Int32Value == other.Int32Value
                    && Int64Value == other.Int64Value
                    && UInt8Value == other.UInt8Value
                    && UInt16Value == other.UInt16Value
                    && UInt32Value == other.UInt32Value
                    && UInt64Value == other.UInt64Value
                    && ((!Float32Value.HasValue && !other.Float32Value.HasValue) || Float32Value.Equals(other.Float32Value))
                    && ((!Float64Value.HasValue && !other.Float64Value.HasValue) || Float64Value.Equals(other.Float64Value))
                    && ((!DateTime.HasValue && !other.DateTime.HasValue) || DateTime.Equals(other.DateTime))
                    && ((!DateTimeOffset.HasValue && !other.DateTimeOffset.HasValue) || DateTimeOffset.Equals(other.DateTimeOffset))
                    && ((!DateOnly.HasValue && !other.DateOnly.HasValue) || DateOnly.Equals(other.DateOnly))
                    && ((!TimeOnly.HasValue && !other.TimeOnly.HasValue) || TimeOnly.Equals(other.TimeOnly))
                    && ((!TomlDateTime.HasValue && !other.TomlDateTime.HasValue) || TomlDateTime.Equals(other.TomlDateTime));
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PrimitiveModel)obj);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(Int8Value);
                hashCode.Add(Int16Value);
                hashCode.Add(Int32Value);
                hashCode.Add(Int64Value);
                hashCode.Add(UInt8Value);
                hashCode.Add(UInt16Value);
                hashCode.Add(UInt32Value);
                hashCode.Add(UInt64Value);
                hashCode.Add(Float32Value);
                hashCode.Add(Float64Value);
                hashCode.Add(DateTime);
                hashCode.Add(DateTimeOffset);
                hashCode.Add(DateOnly);
                hashCode.Add(TimeOnly);
                hashCode.Add(TomlDateTime);
                return hashCode.ToHashCode();
            }

            public override string ToString()
            {
                return $"{nameof(Int8Value)}: {Int8Value}, {nameof(Int16Value)}: {Int16Value}, {nameof(Int32Value)}: {Int32Value}, {nameof(Int64Value)}: {Int64Value}, {nameof(UInt8Value)}: {UInt8Value}, {nameof(UInt16Value)}: {UInt16Value}, {nameof(UInt32Value)}: {UInt32Value}, {nameof(UInt64Value)}: {UInt64Value}, {nameof(Float32Value)}: {Float32Value}, {nameof(Float64Value)}: {Float64Value}, {nameof(DateTime)}: {(DateTime.HasValue ? DateTime.Value.ToUniversalTime() : "null")}, {nameof(DateTimeOffset)}: {(DateTimeOffset.HasValue ? DateTimeOffset.Value.ToUniversalTime() : "null")}, {nameof(DateOnly)}: {DateOnly}, {nameof(TimeOnly)}: {TimeOnly}, {nameof(TomlDateTime)}: {TomlDateTime}";
            }
        }
    }
}