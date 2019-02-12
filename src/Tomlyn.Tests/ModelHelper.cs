// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Tomlyn.Model;

namespace Tomlyn.Tests
{
    public static class ModelHelper
    {
        public static JToken ToJson(TomlObject obj)
        {
            switch (obj)
            {
                case TomlArray tomlArray:
                {
                    var value = new JArray();
                    var isLikeTableArray = (tomlArray.Count > 0 && tomlArray[0] is TomlTable);
                    foreach (var element in tomlArray.GetTomlEnumerator())
                    {
                        value.Add(ToJson(element));
                    }
                    return isLikeTableArray ? (JToken)value : new JObject()
                    {
                        {"type", "array"},
                        {"value", value}
                    };
                }
                case TomlBoolean tomlBoolean:
                    return new JObject
                    {
                        {"type", "bool"},
                        { "value", tomlBoolean.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}
                    };
                case TomlDateTime tomlDateTime:
                    return new JObject
                    {
                        {"type", "datetime"},
                        { "value", tomlDateTime.ToString()}
                    };
                case TomlFloat tomlFloat:
                    return new JObject
                    {
                        {"type", "float"},
                        { "value", AppendDecimalPoint(tomlFloat.Value.ToString("g16", CultureInfo.InvariantCulture), true)}
                    };
                case TomlInteger tomlInteger:
                    return new JObject
                    {
                        {"type", "integer"},
                        { "value", tomlInteger.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}
                    };
                case TomlString tomlString:
                    return new JObject
                    {
                        {"type", "string"},
                        { "value", tomlString.Value}
                    };
                case TomlTable tomlTable:
                {
                    var json = new JObject();
                    // For the test we order by string key
                    foreach (var keyPair in tomlTable.GetTomlEnumerator())
                    {
                        json.Add(keyPair.Key, ToJson(keyPair.Value));
                    }
                    return json;
                }
                case TomlTableArray tomlTableArray:
                {
                    var json = new JArray();
                    foreach (var element in tomlTableArray)
                    {
                        json.Add(ToJson(element));
                    }
                    return json;
                }
            }
            throw new NotSupportedException($"The type element `{obj.GetType()}` is not supported");
        }

        private static string DateTimeToString(DateTime time)
        {
            time = time.ToUniversalTime();
            if (time.Millisecond == 0) return time.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture);
            return time.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture);
        }

        private static string AppendDecimalPoint(string text, bool hasNaN)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                // Do not append a decimal point if floating point type value
                // - is in exponential form, or
                // - already has a decimal point
                if (c == 'e' || c == 'E' || c == '.')
                {
                    return text;
                }
            }
            // Special cases for floating point type supporting NaN and Infinity
            if (hasNaN && (string.Equals(text, "NaN") || text.Contains("Infinity")))
                return text;

            return text + ".0";
        }
    }
}