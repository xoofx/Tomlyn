// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Tomlyn.Model.Accessors;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Model;

internal class ModelToTomlTransform
{
    private readonly object _rootObject;
    private readonly DynamicModelWriteContext _context;
    private readonly TextWriter _writer;
    private readonly List<ObjectPath> _paths;
    private readonly List<ObjectPath> _currentPaths;
    private ITomlMetadataProvider? _metadataProvider;

    public ModelToTomlTransform(object rootObject, DynamicModelWriteContext context)
    {
        _rootObject = rootObject;
        _context = context;
        _writer = context.Writer;
        _paths = new List<ObjectPath>();
        _currentPaths = new List<ObjectPath>();
    }

    public void Run()
    {
        var itemAccessor = _context.GetAccessor(_rootObject.GetType());
        if (itemAccessor is ObjectDynamicAccessor objectDynamicAccessor)
        {
            VisitObject(objectDynamicAccessor, _rootObject, false);
        }
        else
        {
            _context.Diagnostics.Error(new SourceSpan(), $"The root object must a class with properties or a dictionary. Cannot be of type {itemAccessor}.");
        }
    }

    private void PushName(string name, bool isTableArray)
    {
        _paths.Add(new ObjectPath(name, isTableArray));
    }

    private void WriteHeaderTable()
    {
        var name = _paths[_paths.Count - 1].Name;
        WriteLeadingTrivia(name);
        _writer.Write("[");
        WriteDottedKeys();
        _writer.Write("]");
        WriteTrailingTrivia(name);
        _writer.WriteLine();
        WriteTrailingTriviaAfterEndOfLine(name);
        _currentPaths.Clear();
        _currentPaths.AddRange(_paths);
    }

    private void WriteHeaderTableArray()
    {
        var name = _paths[_paths.Count - 1].Name;
        WriteLeadingTrivia(name);
        _writer.Write("[[");
        WriteDottedKeys();
        _writer.Write("]]");
        WriteTrailingTrivia(name);
        _writer.WriteLine();
        WriteTrailingTriviaAfterEndOfLine(name);
        _currentPaths.Clear();
        _currentPaths.AddRange(_paths);
    }

    private void WriteDottedKeys()
    {
        bool isFirst = true;
        foreach (var name in _paths)
        {
            if (!isFirst)
            {
                _writer.Write(".");
            }

            WriteKey(name.Name);
            isFirst = false;
        }
    }

    private void WriteKey(string name)
    {
        _writer.Write(EscapeKey(name));
    }

    private string EscapeKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return $"\"{name.EscapeForToml()}\"";
        
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

    private void EnsureScope()
    {
        if (IsCurrentScopeValid()) return;

        if (_paths.Count == 0)
        {
            _currentPaths.Clear();
        }
        else
        {
            var lastObjectPath = _paths[_paths.Count - 1];

            if (lastObjectPath.IsTableArray)
            {
                WriteHeaderTableArray();
            }
            else
            {
                WriteHeaderTable();
            }
        }
    }

    private bool IsCurrentScopeValid()
    {
        if (_paths.Count == _currentPaths.Count)
        {
            for (var index = 0; index < _paths.Count; index++)
            {
                var path1 = _paths[index];
                var path2 = _currentPaths[index];
                if (!path1.Equals(path2)) return false;
            }

            return true;
        }

        return false;
    }


    private void PopName()
    {
        _paths.RemoveAt(_paths.Count - 1);
    }

    private bool VisitObject(ObjectDynamicAccessor accessor, object currentObject, bool inline)
    {
        bool hasElements = false;
        var isFirst = true;

        var previousMetadata = _metadataProvider;
        _metadataProvider = currentObject as ITomlMetadataProvider;
        try
        {

            var properties = accessor.GetProperties(currentObject);

            // Probe inline for each key
            // If we require a key to be inlined, inline the rest
            // unless for the last key, if it doesn't need to be inline, we keep it as it is
            bool propInline = inline;

            object? lastValue = null;
            bool lastInline = false;
            if (!inline)
            {
                properties = new List<KeyValuePair<string, object?>>(properties);

                foreach (var prop in properties)
                {
                    lastValue = prop.Value;
                    bool isRequiringInline = IsRequiringInline(prop.Value);
                    lastInline = isRequiringInline;
                    if (isRequiringInline)
                    {
                        propInline = true;
                    }
                }
            }

            foreach (var prop in properties)
            {
                // Skip any null properties
                if (prop.Value is null) continue;
                var name = prop.Key;

                if (inline && !isFirst)
                {
                    _writer.Write(", ");
                }

                bool isLastValue = lastValue is not null && ReferenceEquals(lastValue, prop.Value);
                var propToInline = (!isLastValue || lastInline) && propInline;

                // If we switch from non inline to inline, ensure that the scope is here
                if (!inline && propToInline)
                {
                    EnsureScope();
                }

                if (!inline)
                {
                    WriteLeadingTrivia(name);
                }

                WriteKeyValue(name, prop.Value, propToInline);
                if (!inline)
                {
                    WriteTrailingTrivia(name);
                    _writer.WriteLine();
                    WriteTrailingTriviaAfterEndOfLine(name);
                }

                hasElements = true;
                isFirst = false;
            }
        }
        finally
        {
            _metadataProvider = previousMetadata;
        }

        return hasElements;
    }

    private void VisitList(ListDynamicAccessor accessor, object currentObject, bool inline)
    {
        bool isFirst = true;
        foreach (var value in accessor.GetElements(currentObject))
        {
            // Skip any null value
            if (value is null) continue; // TODO: should emit an error?

            var itemAccessor = _context.GetAccessor(value.GetType());

            if (inline)
            {
                if (!isFirst)
                {
                    _writer.Write(", ");
                }

                WriteValueInline(itemAccessor, value);
                isFirst = false;
            }
            else
            {
                WriteHeaderTableArray();
                VisitObject((ObjectDynamicAccessor)itemAccessor, value, false);
            }
        }
    }

    private void WriteKeyValue(string name, object? value, bool inline)
    {
        if (value is null) return;

        var accessor = _context.GetAccessor(value.GetType());

        switch (accessor)
        {
            case ListDynamicAccessor listDynamicAccessor:
            {
                bool wasInline = inline;
                // Switch to inline if the object is a primitive
                if (!inline)
                {
                    inline = IsRequiringInline(listDynamicAccessor, value, 1);
                }

                if (inline)
                {
                    if (!wasInline) EnsureScope();
                    WriteKey(name);
                    _writer.Write(" = [");
                    VisitList(listDynamicAccessor, value, true);
                    _writer.Write("]");
                }
                else
                {
                    PushName(name, true);
                    VisitList(listDynamicAccessor, value, false);
                    PopName();
                }
            }
                break;
            case ObjectDynamicAccessor objectAccessor:
                if (inline)
                {
                    WriteKey(name);
                    _writer.Write(" = {");
                    VisitObject(objectAccessor, value, true);
                    _writer.Write("}");
                }
                else
                {
                    PushName(name, false);
                    var hasElements = VisitObject(objectAccessor, value, false);
                    if (!hasElements)
                    {
                        // Force to have a scope to create the object
                        EnsureScope();
                    }
                    PopName();
                }
                break;
            case PrimitiveDynamicAccessor primitiveDynamicAccessor:
                EnsureScope();
                WriteKey(name);
                _writer.Write(" = ");
                WritePrimitive(value, GetDisplayKind(name));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(accessor));
        }
    }

    private TomlPropertyDisplayKind GetDisplayKind(string name)
    {
        var kind = TomlPropertyDisplayKind.Default;
        if (_metadataProvider is not null && _metadataProvider.PropertiesMetadata is not null && _metadataProvider.PropertiesMetadata.TryGetProperty(name, out var propertyMetadata))
        {
            kind = propertyMetadata.DisplayKind;
        }

        return kind;
    }

    private bool IsRequiringInline(object? value)
    {
        if (value is null) return false;

        var accessor = _context.GetAccessor(value.GetType());

        switch (accessor)
        {
            case ListDynamicAccessor listDynamicAccessor:
                    return IsRequiringInline(listDynamicAccessor, value, 1);
            default:
                return false;
        }
    }

    private bool IsRequiringInline(ListDynamicAccessor accessor, object value, int parentConsecutiveList)
    {
        foreach (var element in accessor.GetElements(value))
        {
            if (element is null) continue; // TODO: should this log an error?
            var elementAccessor = _context.GetAccessor(element.GetType());

            if (elementAccessor is PrimitiveDynamicAccessor) return true;

            if (elementAccessor is ListDynamicAccessor listDynamicAccessor)
            {
                return IsRequiringInline(listDynamicAccessor, element, parentConsecutiveList + 1);

            }
            else if (elementAccessor is ObjectDynamicAccessor objAccessor)
            {
                // Case of an array-of-array of table
                if (parentConsecutiveList > 1) return true;
                return IsRequiringInline(objAccessor, element);
            }
        }

        // Specially for empty list
        return parentConsecutiveList > 1;
    }

    private bool IsRequiringInline(ObjectDynamicAccessor accessor, object value)
    {
        foreach (var prop in accessor.GetProperties(value))
        {
            var propValue = prop.Value;
            if (propValue is null)
            {
                continue;
            }

            var propValueAccessor = _context.GetAccessor(propValue.GetType());
            if (propValueAccessor is ListDynamicAccessor listDynamicAccessor)
            {
                return IsRequiringInline(listDynamicAccessor, propValue, 1);

            }
            else if (propValueAccessor is ObjectDynamicAccessor objAccessor)
            {
                return IsRequiringInline(objAccessor, propValue);
            }
        }

        return false;
    }

    private void WriteLeadingTrivia(string name)
    {
        if (_metadataProvider?.PropertiesMetadata is null || !_metadataProvider.PropertiesMetadata.TryGetProperty(name, out var propertyMetadata) || propertyMetadata.LeadingTrivia is null) return;

        foreach (var trivia in propertyMetadata.LeadingTrivia)
        {
            if (trivia.Text is not null) _writer.Write(trivia.Text);
        }
    }

    private void WriteTrailingTrivia(string name)
    {
        if (_metadataProvider?.PropertiesMetadata is null || !_metadataProvider.PropertiesMetadata.TryGetProperty(name, out var propertyMetadata) || propertyMetadata.TrailingTrivia is null) return;

        foreach (var trivia in propertyMetadata.TrailingTrivia)
        {
            if (trivia.Text is not null) _writer.Write(trivia.Text);
        }
    }

    private void WriteTrailingTriviaAfterEndOfLine(string name)
    {
        if (_metadataProvider?.PropertiesMetadata is null || !_metadataProvider.PropertiesMetadata.TryGetProperty(name, out var propertyMetadata) || propertyMetadata.TrailingTriviaAfterEndOfLine is null) return;

        foreach (var trivia in propertyMetadata.TrailingTriviaAfterEndOfLine)
        {
            if (trivia.Text is not null) _writer.Write(trivia.Text);
        }
    }

    private void WriteValueInline(DynamicAccessor accessor, object? value)
    {
        if (value is null) return;

        switch (accessor)
        {
            case ListDynamicAccessor listDynamicAccessor:
                _writer.Write("[");
                    VisitList(listDynamicAccessor, value, true);
                _writer.Write("]");
                break;
            case ObjectDynamicAccessor objectAccessor:
                _writer.Write("{");
                VisitObject(objectAccessor, value, true);
                _writer.Write("}");
                break;
            case PrimitiveDynamicAccessor primitiveDynamicAccessor:
                WritePrimitive(value, TomlPropertyDisplayKind.Default);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(accessor));
        }
    }

    private void WritePrimitive(object primitive, TomlPropertyDisplayKind displayKind)
    {
        if (primitive is bool b)
        {
            _writer.Write(b ? "true" : "false");
        }
        else if (primitive is string s)
        {
            _writer.Write($"\"{s.EscapeForToml()}\"");
        }
        else if (primitive is int i32)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{i32:x8}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString(i32, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString(i32, 2)}");
                    break;
                default:
                    _writer.Write(i32.ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }
        else if (primitive is long i64)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{i64:x16}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString(i64, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString(i64, 2)}");
                    break;
                default:
                    _writer.Write(i64.ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }
        else if (primitive is uint u32)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{u32:x8}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString(u32, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString(u32, 2)}");
                    break;
                default:
                    _writer.Write(u32.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            _writer.Write(u32.ToString(CultureInfo.InvariantCulture));
        }
        else if (primitive is ulong u64)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{u64:x16}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString((long)u64, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString((long)u64, 2)}");
                    break;
                default:
                    _writer.Write(u64.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            _writer.Write(u64.ToString(CultureInfo.InvariantCulture));
        }
        else if (primitive is sbyte i8)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{i8:x2}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString(i8, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString(i8, 2)}");
                    break;
                default:
                    _writer.Write(i8.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            _writer.Write(i8.ToString(CultureInfo.InvariantCulture));
        }
        else if (primitive is byte u8)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{u8:x2}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString(u8, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString(u8, 2)}");
                    break;
                default:
                    _writer.Write(u8.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            _writer.Write(u8.ToString(CultureInfo.InvariantCulture));
        }
        else if (primitive is short i16)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{i16:x2}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString(i16, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString(i16, 2)}");
                    break;
                default:
                    _writer.Write(i16.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            _writer.Write(i16.ToString(CultureInfo.InvariantCulture));
        }
        else if (primitive is ushort u16)
        {
            switch (displayKind)
            {
                case TomlPropertyDisplayKind.IntegerHexadecimal:
                    _writer.Write($"0x{u16:x2}");
                    break;
                case TomlPropertyDisplayKind.IntegerOctal:
                    _writer.Write($"0o{Convert.ToString(u16, 8)}");
                    break;
                case TomlPropertyDisplayKind.IntegerBinary:
                    _writer.Write($"0b{Convert.ToString(u16, 2)}");
                    break;
                default:
                    _writer.Write(u16.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            _writer.Write(u16.ToString(CultureInfo.InvariantCulture));
        }
        else if (primitive is float f32)
        {
            _writer.Write(FloatToString(f32));
        }
        else if (primitive is double f64)
        {
            _writer.Write(DoubleToString(f64));
        }
        else if (primitive is TomlDateTime tomlDateTime)
        {
            _writer.Write(tomlDateTime);
        }
        else if (primitive is DateTime dateTime)
        {
            WriteDateTime(new DateTimeValue(dateTime, 0, DateTimeValueOffsetKind.None), displayKind);
        }
        else if (primitive is DateTimeOffset dateTimeOffset)
        {
            WriteDateTime(new DateTimeValue(dateTimeOffset, 0, DateTimeValueOffsetKind.Zero), displayKind);
        }
        else if (primitive is DateTimeValue dateTimeValue)
        {
            WriteDateTime(dateTimeValue, displayKind);
        }
        else
        {
            // Unexpected
            throw new InvalidOperationException($"Invalid primitive {primitive.GetType().FullName}");
        }
    }

    private void WriteDateTime(DateTimeValue dateTimeValue, TomlPropertyDisplayKind displayKind)
    {
        _writer.Write(dateTimeValue.ToString(displayKind));
    }

    private static string FloatToString(float value)
    {
        if (float.IsNaN(value))
        {
            return "nan";
        }
        if (float.IsPositiveInfinity(value))
        {
            return "+inf";
        }
        if (float.IsNegativeInfinity(value))
        {
            return "-inf";
        }
        return AppendDecimalPoint(value.ToString("g8", CultureInfo.InvariantCulture));
    }

    private static string DoubleToString(double value)
    {
        if (double.IsNaN(value))
        {
            return "nan";
        }
        if (double.IsPositiveInfinity(value))
        {
            return "+inf";
        }
        if (double.IsNegativeInfinity(value))
        {
            return "-inf";
        }
        return AppendDecimalPoint(value.ToString("g16", CultureInfo.InvariantCulture));
    }

    private static string AppendDecimalPoint(string text)
    {
        if (text == "0") return "0.0";
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
        return text + ".0";
    }

    private record struct ObjectPath(string Name, bool IsTableArray);
}