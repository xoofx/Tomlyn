// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SharpToml.Tests
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
            var doc = Toml.ParseAndValidate(toml);

            Console.WriteLine();
            DisplayHeader("input");
            Console.WriteLine(toml);

            Console.WriteLine();
            DisplayHeader("round-trip");
            var docAsStr = doc.ToString();
            Console.WriteLine(docAsStr);

            if (doc.Diagnostics.Count > 0)
            {
                Console.WriteLine();
                DisplayHeader("messages");

                foreach (var syntaxMessage in doc.Diagnostics)
                {
                    Console.WriteLine(syntaxMessage);
                }

                if (type == ValidSpec)
                {
                    Assert.False(doc.HasErrors, "Unexpected parsing errors");
                }
            }

            if (type == InvalidSpec)
            {
                Assert.True(doc.HasErrors, "The TOML requires parsing/validation errors");
            }

            // Only in the case of a valid spec we check for rountrip
            if (type == ValidSpec)
            {
                Assert.AreEqual(toml, docAsStr, "The roundtrip doesn't match");
            }
        }

        private static void DisplayHeader(string name)
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

                var input = File.ReadAllText(file);

                string json = null;
                if (type == "valid")
                {
                    var jsonFile = Path.ChangeExtension(file, "json");
                    Assert.True(File.Exists(jsonFile), $"The json file `{jsonFile}` does not exist");
                    json = File.ReadAllText(jsonFile);
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