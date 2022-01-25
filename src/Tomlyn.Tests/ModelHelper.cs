// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using Tomlyn.Helpers;
using Tomlyn.Model;

namespace Tomlyn.Tests
{
    public static class ModelHelper
    {
        public static JToken ToJson(object? obj)
        {
            switch (obj)
            {
                case TomlArray tomlArray:
                {
                    var value = new JArray();
                    var isLikeTableArray = (tomlArray.Count > 0 && tomlArray[0] is TomlTable);
                    foreach (var element in tomlArray)
                    {
                        value.Add(ToJson(element));
                    }

                    return value;
                }
                case bool tomlBoolean:
                    return new JObject
                    {
                        {"type", "bool"},
                        { "value", tomlBoolean.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}
                    };
                case TomlDateTime tomlDateTime:
                    string kindStr = "";
                    switch (tomlDateTime.Kind)
                    {
                        case TomlDateTimeKind.OffsetDateTimeByZ:
                        case TomlDateTimeKind.OffsetDateTimeByNumber:
                            kindStr = "datetime";
                            break;
                        case TomlDateTimeKind.LocalDateTime:
                            kindStr = "datetime-local";
                            break;
                        case TomlDateTimeKind.LocalDate:
                            kindStr = "date-local";
                            break;
                        case TomlDateTimeKind.LocalTime:
                            kindStr = "time-local";
                            break;
                    }
                    return new JObject
                    {
                        {"type", kindStr},
                        { "value", tomlDateTime.ToString()}
                    };
                case double tomlFloat:
                    return new JObject
                    {
                        {"type", "float"},
                        { "value",  tomlFloat == 0.0 ? "0" : TomlFormatHelper.ToString(tomlFloat)}
                    };
                case long tomlInteger:
                    return new JObject
                    {
                        {"type", "integer"},
                        { "value", tomlInteger.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}
                    };
                case string tomlString:
                    return new JObject
                    {
                        {"type", "string"},
                        { "value", tomlString}
                    };
                case TomlTable tomlTable:
                {
                    var json = new JObject();
                    // For the test we order by string key
                    foreach (var keyPair in tomlTable.OrderBy(x => x.Key))
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
            throw new NotSupportedException($"The type element `{obj?.GetType()}` is not supported");
        }
    }
}