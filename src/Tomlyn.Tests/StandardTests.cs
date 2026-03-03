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
using Tomlyn.Helpers;
using Tomlyn.Model;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Tomlyn.Tests
{
    /// <summary>
    /// Tests against the official TOML specs of TOML https://github.com/BurntSushi/toml-test
    /// </summary>
    public class StandardTests
    {
        private const string InvalidSpec = "invalid";
        private const string ValidSpec = "valid";

        private const string RelativeTomlTestsDirectory = @"../../../../../ext/toml-test/tests";

        private static readonly string[] Toml11ValidButTomlTestMarksInvalid =
        [
            // TOML v1.1.0 additions (toml-test suite still marks them invalid).
            @"\invalid\datetime\no-secs.toml",            // minute-only times
            @"\invalid\local-datetime\no-secs.toml",
            @"\invalid\local-time\no-secs.toml",
            @"\invalid\string\basic-byte-escapes.toml",   // \xHH basic string escape
            @"\invalid\inline-table\trailing-comma.toml", // trailing commas in inline tables
            @"\invalid\inline-table\linebreak-01.toml",   // TOML 1.1 allows newlines in inline tables
            @"\invalid\inline-table\linebreak-02.toml",
            @"\invalid\inline-table\linebreak-03.toml",
            @"\invalid\inline-table\linebreak-04.toml",
        ];

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
            var doc = SyntaxParser.Parse(toml, inputName);
            var roundtrip = doc.ToString();
            switch (type)
            {
                case ValidSpec:
                    if (doc.HasErrors || toml != roundtrip)
                    {
                        Console.WriteLine($"Testing {inputName}");
                        Dump(toml, doc, roundtrip);
                    }
                    Assert.False(doc.HasErrors, "Unexpected parsing errors");
                    // Only in the case of a valid spec we check for rountrip
                    Assert.AreEqual(toml, roundtrip, "The roundtrip doesn't match");

                    // Read the original json (toml-test encodes datetimes as strings; avoid Json.NET date coercion).
                    var expectedJson = (JObject)NormalizeJson(ParseTomlTestJson(json))!;
                    // Convert to the untyped model.
                    var model = TomlSerializer.Deserialize<TomlTable>(toml);
                    // Convert the model into the expected json
                    var computedJson = ModelHelper.ToJson(model);
                    AssertTomlTestJsonEquivalent(expectedJson, computedJson, inputName, toml, doc, roundtrip);

                    var tomlFromModel = TomlSerializer.Serialize(model);


                    var model2 = TomlSerializer.Deserialize<TomlTable>(tomlFromModel);
                    var computedJson2 = ModelHelper.ToJson(model2);
                    AssertTomlTestJsonEquivalent(expectedJson, computedJson2, inputName, toml, doc, roundtrip, tomlFromModel);
                    break;
                case InvalidSpec:
                    if (!doc.HasErrors)
                    {
                        Console.WriteLine($"Testing {inputName}");
                        Dump(toml, doc, roundtrip);
                    }

                    Assert.True(doc.HasErrors, "The TOML requires parsing/validation errors");
                    break;
            }

            {
                var docUtf8 = SyntaxParser.Parse(Encoding.UTF8.GetBytes(toml), inputName);
                var roundtripUtf8 = docUtf8.ToString();
                if (roundtrip != roundtripUtf8)
                {
                    Console.WriteLine($"Testing {inputName}");
                    Dump(toml, doc, roundtrip);
                }
                Assert.AreEqual(roundtrip, roundtripUtf8, "The UTF8 version doesn't match with the UTF16 version");
            }
        }

        private static JToken? NormalizeJson(JToken? token)
        {
            if (token is JArray array)
            {
                var newArray = new JArray();
                foreach (var item in array)
                {
                    newArray.Add(NormalizeJson(item)!);
                }

                return newArray;
            }
            else if (token is JObject obj)
            {
                var newObject = new JObject();

                var items = new List<KeyValuePair<string, JToken?>>();
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
                        return new JValue(TomlFormatHelper.ToString(doubleValue));
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
                if (type == InvalidSpec)
                {
                    for (var i = 0; i < Toml11ValidButTomlTestMarksInvalid.Length; i++)
                    {
                        if (file.EndsWith(Toml11ValidButTomlTestMarksInvalid[i], StringComparison.OrdinalIgnoreCase))
                        {
                            goto next_file;
                        }
                    }
                }

                var functionName = Path.GetFileName(file);

                var input = Encoding.UTF8.GetString(File.ReadAllBytes(file));

                string? json = null;
                if (type == "valid")
                {
                    var jsonFile = Path.ChangeExtension(file, "json");
                    Assert.True(File.Exists(jsonFile), $"The json file `{jsonFile}` does not exist");
                    json = File.ReadAllText(jsonFile, Encoding.UTF8);
                }
                tests.Add(new TestCaseData(functionName, input, json));

                next_file: ;
            }
            return tests;
        }

        private static JObject ParseTomlTestJson(string json)
        {
            using var reader = new JsonTextReader(new StringReader(json))
            {
                DateParseHandling = DateParseHandling.None,
            };

            return JObject.Load(reader);
        }

        private static void AssertTomlTestJsonEquivalent(JToken expected, JToken actual, string inputName, string toml, DocumentSyntax doc, string roundtrip, string? tomlFromModel = null)
        {
            if (TryCompareTomlTestJson(expected, actual, out var difference))
            {
                return;
            }

            Console.WriteLine($"Testing {inputName}");
            Dump(toml, doc, roundtrip);
            DisplayHeader("json");
            Console.WriteLine(actual.ToString(Formatting.Indented));
            DisplayHeader("expected json");
            Console.WriteLine(expected.ToString(Formatting.Indented));
            if (tomlFromModel is not null)
            {
                DisplayHeader("toml from model");
                Console.WriteLine(tomlFromModel);
            }

            Assert.Fail($"TOML test JSON mismatch: {difference}");
        }

        private static bool TryCompareTomlTestJson(JToken expected, JToken actual, out string difference)
        {
            var path = "$";
            if (TryCompareTomlTestJsonCore(expected, actual, ref path, out difference))
            {
                return true;
            }

            difference = $"{path}: {difference}";
            return false;
        }

        private static bool TryCompareTomlTestJsonCore(JToken expected, JToken actual, ref string path, out string difference)
        {
            if (expected.Type != actual.Type)
            {
                difference = $"Expected token type {expected.Type} but was {actual.Type}.";
                return false;
            }

            switch (expected.Type)
            {
                case JTokenType.Object:
                    return TryCompareTomlTestJsonObject((JObject)expected, (JObject)actual, ref path, out difference);
                case JTokenType.Array:
                    return TryCompareTomlTestJsonArray((JArray)expected, (JArray)actual, ref path, out difference);
                default:
                    if (JToken.DeepEquals(expected, actual))
                    {
                        difference = string.Empty;
                        return true;
                    }

                    difference = $"Expected `{expected}` but was `{actual}`.";
                    return false;
            }
        }

        private static bool TryCompareTomlTestJsonObject(JObject expected, JObject actual, ref string path, out string difference)
        {
            if (TryGetTomlTestTypedValue(expected, out var expectedType, out var expectedValue))
            {
                if (!TryGetTomlTestTypedValue(actual, out var actualType, out var actualValue))
                {
                    difference = "Expected a typed TOML value object.";
                    return false;
                }

                if (!string.Equals(expectedType, actualType, StringComparison.Ordinal))
                {
                    difference = $"Expected type `{expectedType}` but was `{actualType}`.";
                    return false;
                }

                return expectedType switch
                {
                    "string" => CompareString(expectedValue, actualValue, out difference),
                    "bool" => CompareBool(expectedValue, actualValue, out difference),
                    "integer" => CompareInteger(expectedValue, actualValue, out difference),
                    "float" => CompareFloat(expectedValue, actualValue, out difference),
                    "datetime" => CompareDateTime(expectedValue, actualValue, out difference),
                    "array" => TryCompareTomlTestJsonCore(expectedValue, actualValue, ref path, out difference),
                    _ => TryCompareTomlTestJsonCore(expectedValue, actualValue, ref path, out difference),
                };
            }

            var expectedProperties = expected.Properties().ToList();
            var actualProperties = actual.Properties().ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);

            foreach (var expectedProperty in expectedProperties)
            {
                if (!actualProperties.TryGetValue(expectedProperty.Name, out var actualValue))
                {
                    difference = $"Missing property `{expectedProperty.Name}`.";
                    return false;
                }

                var previous = path;
                path = $"{path}.{expectedProperty.Name}";
                if (!TryCompareTomlTestJsonCore(expectedProperty.Value, actualValue, ref path, out difference))
                {
                    return false;
                }
                path = previous;
            }

            if (actualProperties.Count != expectedProperties.Count)
            {
                var extra = actualProperties.Keys.Except(expectedProperties.Select(p => p.Name), StringComparer.Ordinal).FirstOrDefault();
                difference = extra is null ? "Object size mismatch." : $"Unexpected property `{extra}`.";
                return false;
            }

            difference = string.Empty;
            return true;
        }

        private static bool TryGetTomlTestTypedValue(JObject obj, out string type, out JToken value)
        {
            type = string.Empty;
            value = default!;

            if (!obj.TryGetValue("type", out var typeToken) || !obj.TryGetValue("value", out var valueToken))
            {
                return false;
            }

            if (typeToken.Type != JTokenType.String)
            {
                return false;
            }

            type = typeToken.Value<string>() ?? string.Empty;
            value = valueToken;
            return type.Length > 0;
        }

        private static bool TryCompareTomlTestJsonArray(JArray expected, JArray actual, ref string path, out string difference)
        {
            if (expected.Count != actual.Count)
            {
                difference = $"Expected array length {expected.Count} but was {actual.Count}.";
                return false;
            }

            for (var index = 0; index < expected.Count; index++)
            {
                var previous = path;
                path = $"{path}[{index}]";
                if (!TryCompareTomlTestJsonCore(expected[index], actual[index], ref path, out difference))
                {
                    return false;
                }
                path = previous;
            }

            difference = string.Empty;
            return true;
        }

        private static bool CompareString(JToken expected, JToken actual, out string difference)
        {
            var expectedValue = expected.Value<string>();
            var actualValue = actual.Value<string>();
            if (string.Equals(expectedValue, actualValue, StringComparison.Ordinal))
            {
                difference = string.Empty;
                return true;
            }

            difference = $"Expected string `{expectedValue}` but was `{actualValue}`.";
            return false;
        }

        private static bool CompareBool(JToken expected, JToken actual, out string difference)
        {
            if (!TryParseBool(expected.Value<string>(), out var expectedValue) ||
                !TryParseBool(actual.Value<string>(), out var actualValue))
            {
                difference = "Invalid boolean representation.";
                return false;
            }

            if (expectedValue == actualValue)
            {
                difference = string.Empty;
                return true;
            }

            difference = $"Expected bool `{expectedValue}` but was `{actualValue}`.";
            return false;
        }

        private static bool CompareInteger(JToken expected, JToken actual, out string difference)
        {
            if (!long.TryParse(expected.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var expectedValue) ||
                !long.TryParse(actual.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var actualValue))
            {
                difference = "Invalid integer representation.";
                return false;
            }

            if (expectedValue == actualValue)
            {
                difference = string.Empty;
                return true;
            }

            difference = $"Expected integer `{expectedValue}` but was `{actualValue}`.";
            return false;
        }

        private static bool CompareFloat(JToken expected, JToken actual, out string difference)
        {
            if (!TryParseFloat(expected.Value<string>(), out var expectedValue) ||
                !TryParseFloat(actual.Value<string>(), out var actualValue))
            {
                difference = "Invalid float representation.";
                return false;
            }

            if (double.IsNaN(expectedValue) && double.IsNaN(actualValue))
            {
                difference = string.Empty;
                return true;
            }

            if (expectedValue.Equals(actualValue))
            {
                difference = string.Empty;
                return true;
            }

            difference = $"Expected float `{expected}` but was `{actual}`.";
            return false;
        }

        private static bool CompareDateTime(JToken expected, JToken actual, out string difference)
        {
            var expectedText = expected.Value<string>();
            var actualText = actual.Value<string>();

            if (string.Equals(expectedText, actualText, StringComparison.Ordinal))
            {
                difference = string.Empty;
                return true;
            }

            if (expectedText is not null && actualText is not null)
            {
                var expectedNormalized = NormalizeTomlTestDateTimeText(expectedText);
                var actualNormalized = NormalizeTomlTestDateTimeText(actualText);
                if (string.Equals(expectedNormalized, actualNormalized, StringComparison.Ordinal))
                {
                    difference = string.Empty;
                    return true;
                }
            }

            difference = $"Expected datetime `{expectedText}` but was `{actualText}`.";
            return false;
        }

        private static string NormalizeTomlTestDateTimeText(string text)
        {
            // toml-test encodes timestamps as RFC3339 strings, often normalizing fractional seconds
            // to millisecond precision (e.g. ".600"). Tomlyn may preserve the original precision
            // (e.g. ".6"). Normalize by trimming trailing zeros from the fractional part.
            var dotIndex = text.IndexOf('.');
            if (dotIndex < 0)
            {
                return text;
            }

            var index = dotIndex + 1;
            while (index < text.Length && char.IsDigit(text[index]))
            {
                index++;
            }

            if (index == dotIndex + 1)
            {
                return text;
            }

            var fracEnd = index;
            while (fracEnd > dotIndex + 1 && text[fracEnd - 1] == '0')
            {
                fracEnd--;
            }

            if (fracEnd == dotIndex + 1)
            {
                // Fraction becomes empty: remove the '.' as well.
                return text.Substring(0, dotIndex) + text.Substring(index);
            }

            if (fracEnd == index)
            {
                return text;
            }

            return text.Substring(0, fracEnd) + text.Substring(index);
        }

        private static bool TryParseBool(string? text, out bool value)
        {
            if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryParseFloat(string? text, out double value)
        {
            if (text is null)
            {
                value = default;
                return false;
            }

            if (string.Equals(text, "nan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "+nan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "-nan", StringComparison.OrdinalIgnoreCase))
            {
                value = double.NaN;
                return true;
            }

            if (string.Equals(text, "inf", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "+inf", StringComparison.OrdinalIgnoreCase))
            {
                value = double.PositiveInfinity;
                return true;
            }

            if (string.Equals(text, "-inf", StringComparison.OrdinalIgnoreCase))
            {
                value = double.NegativeInfinity;
                return true;
            }

            // toml-test values are typically JSON numbers-as-strings, sometimes using normalized exponent forms.
            // Double parsing already accepts both "1e06" and "1e+06".
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string BaseDirectory
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                return Path.GetDirectoryName(assembly.Location) ?? Environment.CurrentDirectory;
            }
        }
    }
}
