using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Tomlyn.Helpers;
using Tomlyn.Model;
using Tomlyn.Text;

namespace Tomlyn.Serialization.Internal;

internal static class TomlModelTextWriter
{
    public static void WriteDocument(TextWriter writer, TomlTable root, TomlSerializerOptions options)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(root, nameof(root));
        ArgumentGuard.ThrowIfNull(options, nameof(options));

        var state = new State(writer, options);
        state.WriteTableBody(root, path: null, headerKind: HeaderKind.None);
    }

    private enum HeaderKind
    {
        None = 0,
        Table = 1,
        TableArrayElement = 2,
    }

    private sealed class State
    {
        private readonly TextWriter _writer;
        private readonly TomlSerializerOptions _options;
        private readonly string _newLine;

        public State(TextWriter writer, TomlSerializerOptions options)
        {
            _writer = writer;
            _options = options;
            _newLine = options.NewLine == TomlNewLineKind.CrLf ? "\r\n" : "\n";
        }

        public void WriteTableBody(TomlTable table, List<string>? path, HeaderKind headerKind)
        {
            if (headerKind != HeaderKind.None)
            {
                WriteHeader(path!, headerKind, table.PropertiesMetadata);
            }

            // 1) scalar/array/inline-table key-values
            var scalarKeys = new List<(string Key, object Value)>();
            var tableKeys = new List<(string Key, TomlTable Value)>();
            var tableArrayKeys = new List<(string Key, TomlTableArray Value)>();

            foreach (var pair in table)
            {
                if (pair.Value is TomlTableArray tableArray)
                {
                    if (_options.TableArrayStyle == TomlTableArrayStyle.InlineArrayOfTables &&
                        TryWriteTableArrayInline(pair.Key, tableArray, table.PropertiesMetadata))
                    {
                        continue;
                    }

                    tableArrayKeys.Add((pair.Key, tableArray));
                    continue;
                }

                if (pair.Value is TomlTable subTable && !ShouldInlineTable(subTable))
                {
                    tableKeys.Add((pair.Key, subTable));
                    continue;
                }

                scalarKeys.Add((pair.Key, pair.Value));
            }

            WriteKeyValuePairs(table, scalarKeys);

            // 2) named tables
            for (var i = 0; i < tableKeys.Count; i++)
            {
                var (key, value) = tableKeys[i];
                var childPath = AppendPath(path, key);
                WriteTableBody(value, childPath, HeaderKind.Table);
            }

            // 3) table arrays
            for (var i = 0; i < tableArrayKeys.Count; i++)
            {
                var (key, value) = tableArrayKeys[i];
                WriteTableArray(value, AppendPath(path, key));
            }
        }

        private void WriteTableArray(TomlTableArray tableArray, List<string> path)
        {
            for (var index = 0; index < tableArray.Count; index++)
            {
                if (index > 0)
                {
                    WriteNewLine();
                }

                WriteTableBody(tableArray[index], path, HeaderKind.TableArrayElement);
            }
        }

        private void WriteKeyValuePairs(TomlTable table, List<(string Key, object Value)> pairs)
        {
            for (var i = 0; i < pairs.Count; i++)
            {
                var (key, value) = pairs[i];
                WriteLeadingTrivia(table.PropertiesMetadata, key);

                WriteKey(key);
                _writer.Write(" = ");

                var displayKind = TomlPropertyDisplayKind.Default;
                if (table.PropertiesMetadata is { } metadata &&
                    metadata.TryGetProperty(key, out var propertyMetadata) &&
                    propertyMetadata is not null)
                {
                    displayKind = propertyMetadata.DisplayKind;
                }

                WriteValue(value, displayKind);
                WriteTrailingTrivia(table.PropertiesMetadata, key);
                WriteNewLine();
                WriteTrailingTriviaAfterEndOfLine(table.PropertiesMetadata, key);
            }
        }

        private bool ShouldInlineTable(TomlTable table)
        {
            if (table.Kind == ObjectKind.InlineTable)
            {
                return true;
            }

            return _options.InlineTablePolicy switch
            {
                TomlInlineTablePolicy.Always => IsInlineableTable(table, maxMemberCount: int.MaxValue),
                TomlInlineTablePolicy.WhenSmall => IsInlineableTable(table, maxMemberCount: 4),
                _ => false,
            };
        }

        private static bool IsInlineableTable(TomlTable table, int maxMemberCount)
        {
            if (table.Count > maxMemberCount)
            {
                return false;
            }

            foreach (var pair in table)
            {
                if (pair.Value is TomlTableArray)
                {
                    return false;
                }

                if (pair.Value is TomlTable child && !IsInlineableTable(child, maxMemberCount: int.MaxValue))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryWriteTableArrayInline(string key, TomlTableArray tableArray, TomlPropertiesMetadata? metadata)
        {
            for (var i = 0; i < tableArray.Count; i++)
            {
                if (!IsInlineableTable(tableArray[i], maxMemberCount: int.MaxValue))
                {
                    return false;
                }
            }

            WriteLeadingTrivia(metadata, key);
            WriteKey(key);
            _writer.Write(" = ");

            _writer.Write("[");
            for (var i = 0; i < tableArray.Count; i++)
            {
                if (i > 0)
                {
                    _writer.Write(", ");
                }

                WriteInlineTable(tableArray[i]);
            }

            _writer.Write("]");
            WriteTrailingTrivia(metadata, key);
            WriteNewLine();
            WriteTrailingTriviaAfterEndOfLine(metadata, key);
            return true;
        }

        private void WriteHeader(List<string> path, HeaderKind kind, TomlPropertiesMetadata? metadata)
        {
            var name = path[path.Count - 1];
            WriteLeadingTrivia(metadata, name);

            if (kind == HeaderKind.TableArrayElement)
            {
                _writer.Write("[[");
            }
            else
            {
                _writer.Write("[");
            }

            for (var i = 0; i < path.Count; i++)
            {
                if (i > 0)
                {
                    _writer.Write(".");
                }

                WriteKey(path[i]);
            }

            if (kind == HeaderKind.TableArrayElement)
            {
                _writer.Write("]]");
            }
            else
            {
                _writer.Write("]");
            }

            WriteTrailingTrivia(metadata, name);
            WriteNewLine();
            WriteTrailingTriviaAfterEndOfLine(metadata, name);
        }

        private void WriteInlineTable(TomlTable table)
        {
            _writer.Write("{");

            var first = true;
            foreach (var pair in table)
            {
                if (!first)
                {
                    _writer.Write(", ");
                }

                first = false;
                WriteKey(pair.Key);
                _writer.Write(" = ");

                if (pair.Value is TomlTable nestedTable)
                {
                    if (!IsInlineableTable(nestedTable, maxMemberCount: int.MaxValue))
                    {
                        throw new TomlException("Inline tables cannot contain non-inline tables or table arrays.");
                    }

                    WriteInlineTable(nestedTable);
                    continue;
                }

                WriteValue(pair.Value, TomlPropertyDisplayKind.Default);
            }

            _writer.Write("}");
        }

        private void WriteValue(object value, TomlPropertyDisplayKind displayKind)
        {
            if (value is null)
            {
                throw new TomlException("TOML does not support null values.");
            }

            switch (value)
            {
                case string s:
                    _writer.Write(TomlFormatHelper.ToString(s, displayKind));
                    return;
                case bool b:
                    _writer.Write(TomlFormatHelper.ToString(b));
                    return;
                case sbyte i8:
                    _writer.Write(TomlFormatHelper.ToString(i8, displayKind));
                    return;
                case byte u8:
                    _writer.Write(TomlFormatHelper.ToString(u8, displayKind));
                    return;
                case short i16:
                    _writer.Write(TomlFormatHelper.ToString(i16, displayKind));
                    return;
                case ushort u16:
                    _writer.Write(TomlFormatHelper.ToString(u16, displayKind));
                    return;
                case int i32:
                    _writer.Write(TomlFormatHelper.ToString(i32, displayKind));
                    return;
                case uint u32:
                    _writer.Write(TomlFormatHelper.ToString(u32, displayKind));
                    return;
                case long i64:
                    _writer.Write(TomlFormatHelper.ToString(i64, displayKind));
                    return;
                case ulong u64:
                    _writer.Write(TomlFormatHelper.ToString(u64, displayKind));
                    return;
                case float f32:
                    _writer.Write(TomlFormatHelper.ToString(f32));
                    return;
                case double f64:
                    _writer.Write(TomlFormatHelper.ToString(f64));
                    return;
                case decimal dec:
                    _writer.Write(ToTomlFloatString(dec));
                    return;
                case TomlDateTime dt:
                    _writer.Write(TomlFormatHelper.ToString(dt));
                    return;
                case DateTime dateTime:
                    _writer.Write(TomlFormatHelper.ToString(dateTime, displayKind));
                    return;
                case DateTimeOffset dateTimeOffset:
                    _writer.Write(TomlFormatHelper.ToString(dateTimeOffset, displayKind));
                    return;
#if NET6_0_OR_GREATER
                case DateOnly dateOnly:
                    _writer.Write(TomlFormatHelper.ToString(dateOnly, displayKind));
                    return;
                case TimeOnly timeOnly:
                    _writer.Write(TomlFormatHelper.ToString(timeOnly, displayKind));
                    return;
#endif
                case TomlArray array:
                    WriteArray(array);
                    return;
                case TomlTable inlineTable when ShouldInlineTable(inlineTable):
                    WriteInlineTable(inlineTable);
                    return;
                case TomlTable:
                    throw new TomlException("Non-inline tables must be emitted as table headers.");
                case TomlTableArray:
                    throw new TomlException("Table arrays must be emitted as table headers or inline array of tables.");
                default:
                    if (value is Enum enumValue)
                    {
                        _writer.Write(TomlFormatHelper.ToString(enumValue.ToString(), displayKind));
                        return;
                    }

                    throw new TomlException($"Unsupported TOML value `{value.GetType().FullName}`.");
            }
        }

        private void WriteArray(TomlArray array)
        {
            _writer.Write("[");
            for (var i = 0; i < array.Count; i++)
            {
                if (i > 0)
                {
                    _writer.Write(", ");
                }

                var value = array[i]!;
                if (value is TomlTable nestedTable)
                {
                    if (!IsInlineableTable(nestedTable, maxMemberCount: int.MaxValue))
                    {
                        throw new TomlException("Arrays cannot contain non-inline tables or table arrays.");
                    }

                    WriteInlineTable(nestedTable);
                    continue;
                }

                WriteValue(value, TomlPropertyDisplayKind.Default);
            }
            _writer.Write("]");
        }

        private void WriteKey(string name)
        {
            _writer.Write(EscapeKey(name));
        }

        private static string EscapeKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return $"\"{name.EscapeForToml()}\"";
            }

            // A-Za-z0-9_-
            foreach (var c in name)
            {
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '-') || c == '.')
                {
                    return $"\"{name.EscapeForToml()}\"";
                }
            }

            return name;
        }

        private static string ToTomlFloatString(decimal value)
        {
            // TOML has no decimal type; represent `decimal` as a float.
            var text = value.ToString("G", CultureInfo.InvariantCulture);
            if (text.IndexOf('.') >= 0 || text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0)
            {
                return text;
            }

            return text + ".0";
        }

        private void WriteNewLine()
        {
            _writer.Write(_newLine);
        }

        private static List<string> AppendPath(List<string>? path, string name)
        {
            if (path is null)
            {
                return new List<string> { name };
            }

            var list = new List<string>(path.Count + 1);
            list.AddRange(path);
            list.Add(name);
            return list;
        }

        private void WriteLeadingTrivia(TomlPropertiesMetadata? metadata, string key)
        {
            if (metadata is null || !metadata.TryGetProperty(key, out var propertyMetadata) || propertyMetadata?.LeadingTrivia is null)
            {
                return;
            }

            foreach (var trivia in propertyMetadata.LeadingTrivia)
            {
                if (trivia.Text is not null)
                {
                    _writer.Write(trivia.Text);
                }
            }
        }

        private void WriteTrailingTrivia(TomlPropertiesMetadata? metadata, string key)
        {
            if (metadata is null || !metadata.TryGetProperty(key, out var propertyMetadata) || propertyMetadata?.TrailingTrivia is null)
            {
                return;
            }

            foreach (var trivia in propertyMetadata.TrailingTrivia)
            {
                if (trivia.Text is not null)
                {
                    _writer.Write(trivia.Text);
                }
            }
        }

        private void WriteTrailingTriviaAfterEndOfLine(TomlPropertiesMetadata? metadata, string key)
        {
            if (metadata is null || !metadata.TryGetProperty(key, out var propertyMetadata) || propertyMetadata?.TrailingTriviaAfterEndOfLine is null)
            {
                return;
            }

            foreach (var trivia in propertyMetadata.TrailingTriviaAfterEndOfLine)
            {
                if (trivia.Text is not null)
                {
                    _writer.Write(trivia.Text);
                }
            }
        }
    }
}
