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
                    string kindStr = "";
                    switch (tomlDateTime.Kind)
                    {
                        case ObjectKind.OffsetDateTime:
                            kindStr = "datetime";
                            break;
                        case ObjectKind.LocalDateTime:
                            kindStr = "datetime-local";
                            break;
                        case ObjectKind.LocalDate:
                            kindStr = "date";
                            break;
                        case ObjectKind.LocalTime:
                            kindStr = "time";
                            break;
                    }
                    return new JObject
                    {
                        {"type", kindStr},
                        { "value", tomlDateTime.ToString()}
                    };
                case TomlFloat tomlFloat:
                    return new JObject
                    {
                        {"type", "float"},
                        { "value", tomlFloat.ToString()}
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
    }
}