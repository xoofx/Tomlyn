// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace Tomlyn.Tests
{
    public class StandardTests
    {
        private const string InvalidSpec = "invalid";
        private const string ValidSpec = "valid";

        private const string RelativeTomlTestsDirectory = @"../../../../../ext/toml-test/tests";

        [TestCaseSource("ListTomlFiles", new object[] { ValidSpec }, Category = "toml-test")]
        public static void SpecValid(string inputName, string toml, string json)
        {
            ValidateSpec(ValidSpec, inputName, toml, json);
        }

        [TestCaseSource("ListTomlFiles", new object[] { InvalidSpec }, Category = "toml-test")]
        public static void SpecInvalid(string inputName, string toml, string json)
        {

            ValidateSpec(InvalidSpec, inputName, toml, json);
        }

        private static void ValidateSpec(string type, string inputName, string toml, string json)
        {
            Console.WriteLine($"Testing {inputName}");
            var doc = Toml.Parse(toml, inputName);
            var roundtrip = doc.ToString();
            Dump(toml, doc, roundtrip);
            switch (type)
            {
                case ValidSpec:
                    Assert.False(doc.HasErrors, "Unexpected parsing errors");
                    // Only in the case of a valid spec we check for rountrip
                    Assert.AreEqual(toml, roundtrip, "The roundtrip doesn't match");

                    // Read the original json
                    var expectedJson = (JObject)NormalizeJson(JObject.Parse(json));
                    // Convert the syntax tree into a model
                    var model = doc.ToModel();
                    // Convert the model into the expected json
                    var computedJson = ModelHelper.ToJson(model);

                    // Write back the result to a string
                    var expectedJsonAsString = expectedJson.ToString(Formatting.Indented);
                    var computedJsonAsString = computedJson.ToString(Formatting.Indented);

                    DisplayHeader("json");
                    Console.WriteLine(computedJsonAsString);

                    DisplayHeader("expected json");
                    Console.WriteLine(expectedJsonAsString);

                    Assert.AreEqual(expectedJsonAsString, computedJsonAsString);

                    DisplayHeader("toml from model");
                    var tomlFromModel = Toml.ToString(model);
                    Console.WriteLine(tomlFromModel);

                    var model2 = Toml.ParseToModel<TomlTable>(tomlFromModel);
                    var computedJson2 = ModelHelper.ToJson(model2);
                    var computedJson2AsString = computedJson2.ToString(Formatting.Indented);

                    DisplayHeader("json2");
                    Console.WriteLine(computedJson2AsString);

                    Assert.AreEqual(expectedJsonAsString, computedJson2AsString);
                    break;
                case InvalidSpec:
                    Assert.True(doc.HasErrors, "The TOML requires parsing/validation errors");
                    break;
            }

            {
                var docUtf8 = Toml.Parse(Encoding.UTF8.GetBytes(toml), inputName);
                var roundtripUtf8 = doc.ToString();
                Assert.AreEqual(roundtrip, roundtripUtf8, "The UTF8 version doesn't match with the UTF16 version");
            }
        }

        private static JToken NormalizeJson(JToken token)
        {
            if (token is JArray array)
            {
                var newArray = new JArray();
                foreach (var item in array)
                {
                    newArray.Add(NormalizeJson(item));
                }

                return newArray;
            }
            else if (token is JObject obj)
            {
                var newObject = new JObject();

                var items = new List<KeyValuePair<string, JToken>>();
                foreach (var item in obj)
                {
                    items.Add(item);
                }

                foreach (var item in items.OrderBy(x => x.Key))
                {
                    newObject.Add(item.Key, NormalizeJson(item.Value));
                }

                return newObject;
            }
            else if (token is JValue value && value.Value is string str)
            {
                if (string.CompareOrdinal(str, "inf") == 0) return new JValue("+inf");
                if (str.Length > 0 && char.IsDigit(str[0]) && str.Contains('.') && str.Contains('e'))
                {
                    if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        return new JValue(new TomlFloat(doubleValue).ToString());
                    }
                }
            }

            return token;
        }


        public static void Dump(string input, DocumentSyntax doc, string roundtrip)
        {
            Console.WriteLine();
            DisplayHeader("input");
            Console.WriteLine(input);

            Console.WriteLine();
            DisplayHeader("round-trip");
            Console.WriteLine(roundtrip);

            if (doc.Diagnostics.Count > 0)
            {
                Console.WriteLine();
                DisplayHeader("messages");

                foreach (var syntaxMessage in doc.Diagnostics)
                {
                    Console.WriteLine(syntaxMessage);
                }
            }
        }

        public static void DisplayHeader(string name)
        {
            Console.WriteLine($"// ----------------------------------------------------------");
            Console.WriteLine($"// {name}");
            Console.WriteLine($"// ----------------------------------------------------------");
        }

        public static IEnumerable ListTomlFiles(string type)
        {
            var directory = Path.GetFullPath(Path.Combine(BaseDirectory, RelativeTomlTestsDirectory, type));

            Assert.True(Directory.Exists(directory), $"The folder `{directory}` does not exist");

            var tests = new List<TestCaseData>();
            foreach (var file in Directory.EnumerateFiles(directory, "*.toml", SearchOption.AllDirectories))
            {
                var functionName = Path.GetFileName(file);

                var input = Encoding.UTF8.GetString(File.ReadAllBytes(file));

                string json = null;
                if (type == "valid")
                {
                    var jsonFile = Path.ChangeExtension(file, "json");
                    Assert.True(File.Exists(jsonFile), $"The json file `{jsonFile}` does not exist");
                    json = File.ReadAllText(jsonFile, Encoding.UTF8);
                }
                tests.Add(new TestCaseData(functionName, input, json));
            }
            return tests;
        }

        private static string BaseDirectory
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                return Path.GetDirectoryName(assembly.Location);
            }
        }
    }
}