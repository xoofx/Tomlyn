using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using Tomlyn.Model.Accessors;
using Tomlyn.Syntax;

namespace Tomlyn.Model;

internal class DynamicModelReadContext
{
    private readonly Dictionary<Type, DynamicAccessor> _accessors;

    public DynamicModelReadContext(TomlModelOptions options)
    {
        GetPropertyName = options.GetPropertyName;
        ConvertPropertyName = options.ConvertPropertyName;
        CreateInstance = options.CreateInstance;
        ConvertToModel = options.ConvertToModel;
        IgnoreMissingProperties = options.IgnoreMissingProperties;
        Diagnostics = new DiagnosticsBag();
        _accessors = new Dictionary<Type, DynamicAccessor>();
    }

    public Func<PropertyInfo, string?> GetPropertyName { get; set; }

    public Func<string, string> ConvertPropertyName { get; set; }

    public Func<Type, ObjectKind, object> CreateInstance { get; set; }

    public Func<object, Type, object?>? ConvertToModel { get; set; }

    public DiagnosticsBag Diagnostics { get; }

    public bool IgnoreMissingProperties { get; set; }
    
    public DynamicAccessor GetAccessor(Type type)
    {
        if (!_accessors.TryGetValue(type, out var accessor))
        {
            var reflectionInfo = ReflectionObjectInfo.Get(type);
            switch (reflectionInfo.Kind)
            {
                case ReflectionObjectKind.Primitive:
                    accessor = new PrimitiveDynamicAccessor(this, type, false);
                    break;
                case ReflectionObjectKind.NullablePrimitive:
                    accessor = new PrimitiveDynamicAccessor(this, reflectionInfo.GenericArgument1!, true);
                    break;
                case ReflectionObjectKind.NullableStruct:
                    accessor = new StandardObjectDynamicAccessor(this, reflectionInfo.GenericArgument1!, ReflectionObjectKind.Struct); ;
                    break;
                case ReflectionObjectKind.Collection:
                    accessor = new ListDynamicAccessor(this, type, reflectionInfo.GenericArgument1!);
                    break;
                case ReflectionObjectKind.Dictionary:
                    accessor = new DictionaryDynamicAccessor(this, type, reflectionInfo.GenericArgument1!, reflectionInfo.GenericArgument2!); ;
                    break;
                case ReflectionObjectKind.Struct:
                case ReflectionObjectKind.Object:
                    accessor = new StandardObjectDynamicAccessor(this, type, reflectionInfo.Kind); ;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported {reflectionInfo.Kind}");
            }
            _accessors.Add(type, accessor!);
        }
        return accessor!;
    }

    public bool TryConvertValue(SourceSpan span, object? value, Type changeType, out object? outputValue)
    {
        if (value is null || changeType.IsInstanceOfType(value))
        {
            outputValue = value;
            return true;
        }

        var unwrapped = Nullable.GetUnderlyingType(changeType);
        if (unwrapped != null)
        {
            return TryConvertValue(span, value, unwrapped, out outputValue);
        }

        string errorMessage;
        try
        {
            if (typeof(Enum).IsAssignableFrom(changeType) && value is string text)
            {
                    value = Enum.Parse(changeType, text, true);
                    outputValue = value;
                    return true;
            }

            if (value is IConvertible)
            {
                // Try to bit-cast between primitive of same size, otherwise we will overflow
                // As TOML only support long (instead of ulong), we need to make sure we can
                // serialize/deserialize back primitive values
                if (value.GetType().IsPrimitive && changeType.IsPrimitive)
                {
                    switch (value)
                    {
                        case sbyte i8:
                            if (changeType == typeof(byte))
                            {
                                outputValue = unchecked((byte)i8);
                                return true;
                            }
                            break;
                        case short i16:
                            if (changeType == typeof(ushort))
                            {
                                outputValue = unchecked((ushort)i16);
                                return true;
                            }
                            break;
                        case int i32:
                            if (changeType == typeof(uint))
                            {
                                outputValue = unchecked((uint) i32);
                                return true;
                            }
                            break;
                        case long i64:
                            if (changeType == typeof(ulong))
                            {
                                outputValue = unchecked((ulong)i64);
                                return true;
                            }
                            break;
                        case byte u8:
                            if (changeType == typeof(sbyte))
                            {
                                outputValue = unchecked((sbyte)u8);
                                return true;
                            }
                            break;
                        case ushort u16:
                            if (changeType == typeof(short))
                            {
                                outputValue = unchecked((short)u16);
                                return true;
                            }
                            break;
                        case uint u32:
                            if (changeType == typeof(int))
                            {
                                outputValue = unchecked((int)u32);
                                return true;
                            }
                            break;
                        case ulong u64:
                            if (changeType == typeof(long))
                            {
                                outputValue = unchecked((long)u64);
                                return true;
                            }
                            break;
                    }
                }

                try
                {
                    outputValue = Convert.ChangeType(value, changeType);
                    return true;
                }
                catch (Exception) when(ConvertToModel is not null)
                {
                    // ignore
                }
            }

            if (ConvertToModel is not null)
            {
                var convertedValue = ConvertToModel(value, changeType);
                outputValue = convertedValue;
                if (convertedValue is not null)
                {
                    return true;
                }
            }

            errorMessage = $"Unsupported type to convert {value.GetType().FullName} to type {changeType.FullName}.";
        }
        catch (Exception ex)
        {
            errorMessage = $"Exception while trying to convert {value.GetType().FullName} to type {changeType.FullName}. Reason: {ex.Message}";
        }

        outputValue = null;
        Diagnostics.Error(span, errorMessage);
        return false;
    }
}