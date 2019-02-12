// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
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
                    var srcJson = JObject.Parse(json);
                    // Convert the syntax tree into a model
                    var model = doc.ToModel();
                    // Convert the model into the expected json
                    var modelJson = ModelHelper.ToJson(model);

                    // Write back the result to a string
                    var srcJsonAsString = srcJson.ToString(Formatting.Indented);
                    var dstJsonAsString = modelJson.ToString(Formatting.Indented);

                    DisplayHeader("json");
                    Console.WriteLine(srcJsonAsString);

                    DisplayHeader("expected json");
                    Console.WriteLine(dstJsonAsString);

                    Assert.AreEqual(srcJsonAsString, dstJsonAsString);
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

                var input = File.ReadAllText(file, Encoding.UTF8);

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
                var codebase = new Uri(assembly.CodeBase);
                var path = codebase.LocalPath;
                return Path.GetDirectoryName(path);
            }
        }
    }
}