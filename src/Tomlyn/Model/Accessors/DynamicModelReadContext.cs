using System;
using System.Collections.Generic;
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
        CreateInstance = options.CreateInstance;
        ConvertTo = options.ConvertTo;
        Diagnostics = new DiagnosticsBag();
        _accessors = new Dictionary<Type, DynamicAccessor>();
    }

    public Func<PropertyInfo, string> GetPropertyName { get; set; }
    
    public Func<Type, ObjectKind, object> CreateInstance { get; set; }

    public Func<object, Type, object>? ConvertTo { get; set; }

    public DiagnosticsBag Diagnostics { get; }
    
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
                    accessor = new StandardObjectDynamicAccessor(this, reflectionInfo.GenericArgument1!); ;
                    break;
                case ReflectionObjectKind.Collection:
                    accessor = new ListDynamicAccessor(this, type, reflectionInfo.GenericArgument1!);
                    break;
                case ReflectionObjectKind.Dictionary:
                    accessor = new DictionaryDynamicAccessor(this, type, reflectionInfo.GenericArgument1!, reflectionInfo.GenericArgument2!); ;
                    break;
                case ReflectionObjectKind.Struct:
                case ReflectionObjectKind.Object:
                    accessor = new StandardObjectDynamicAccessor(this, type); ;
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
                outputValue = Convert.ChangeType(value, changeType);
                return true;
            }

            if (ConvertTo is not null)
            {
                outputValue = ConvertTo(value, changeType);
                return true;
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