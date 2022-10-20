// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Tomlyn.Syntax;

namespace Tomlyn.Model.Accessors;

internal class StandardObjectDynamicAccessor : ObjectDynamicAccessor
{
    private readonly Dictionary<string, PropertyInfo> _props;
    private readonly Dictionary<string, FieldInfo> _fields;

    private readonly List<KeyValuePair<string, PropertyInfo>> _orderedProps;
    private readonly List<KeyValuePair<string, FieldInfo>> _orderedFields;

    public StandardObjectDynamicAccessor(DynamicModelReadContext context, Type type, ReflectionObjectKind kind) : base(context, type, kind)
    {
        _props = new Dictionary<string, PropertyInfo>();
        _fields = new Dictionary<string, FieldInfo>();
        _orderedProps = new List<KeyValuePair<string, PropertyInfo>>();
        _orderedFields = new List<KeyValuePair<string, FieldInfo>>();

        Initialize();
    }

    private void Initialize()
    {
        foreach (var prop in TargetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
            var name = Context.GetPropertyName(prop);
            // If the property name is null, the property can be ignored
            if (name is null) continue;

            // Skip any properties that we can't read
            if (!prop.CanRead) continue;

            // Skip properties that are string or value types and are read-only
            if ((prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType) && !prop.CanWrite)
            {
                continue;
            }

            if (!_props.ContainsKey(name))
            {
                _props[name] = prop;
                _orderedProps.Add(new KeyValuePair<string, PropertyInfo>(name, prop));
            }
        }

        if (Context.IncludeFields)
        {
            foreach (var field in TargetType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var name = Context.GetFieldName(field);
                // If the field name is null, the field can be ignored
                if (name is null) continue;

                // Skip fields that are string or value types and are read-only
                if ((field.FieldType == typeof(string) || field.FieldType.IsValueType) && field.IsInitOnly)
                {
                    continue;
                }

                if (!_fields.ContainsKey(name))
                {
                    _fields[name] = field;
                    _orderedFields.Add(new KeyValuePair<string, FieldInfo>(name, field));
                }
            }
        }
    }

    public override IEnumerable<KeyValuePair<string, object?>> GetProperties(object obj)
    {
        foreach (var prop in _orderedProps)
        {
            yield return new KeyValuePair<string, object?>(prop.Key, prop.Value.GetValue(obj));
        }

        if (Context.IncludeFields)
        {
            foreach (var field in _orderedFields)
            {
                yield return new KeyValuePair<string, object?>(field.Key, field.Value.GetValue(obj));
            }
        }
    }

    public override bool TryGetPropertyValue(SourceSpan span, object obj, string name, out object? value)
    {
        value = null;
        if (_props.TryGetValue(name, out var prop))
        {
            value = prop.GetValue(obj);
            return true;
        }

        if (Context.IncludeFields && _fields.TryGetValue(name, out var field))
        {
            value = field.GetValue(obj);
            return true;
        }

        return false;
    }

    public override bool TrySetPropertyValue(SourceSpan span, object obj, string name, object? value)
    {
        string errorMessage = "Unknown error";
        try
        {
            if (_props.TryGetValue(name, out var prop))
            {
                var accessor = Context.GetAccessor(prop.PropertyType);
                if (accessor is ListDynamicAccessor listAccessor)
                {
                    // Coerce the value
                    var listValue = prop.GetValue(obj);
                    if (value is not null && prop.PropertyType.IsInstanceOfType(value))
                    {
                        if (prop.SetMethod is not null)
                        {
                            if (Context.TryConvertValue(span, value, prop.PropertyType, out value))
                            {
                                prop.SetValue(obj, value);
                                return true;
                            }
                            else
                            {
                                errorMessage = $"The property value of type {value?.GetType().FullName} couldn't be converted to {prop.PropertyType} for the list property {TargetType.FullName}/{name}";
                            }
                        }
                        else if (listValue is not null)
                        {
                            foreach (var item in (IEnumerable)value)
                            {
                                listAccessor.AddElement(listValue, item);
                            }

                            return true;
                        }
                    }
                    else
                    {
                        if (listValue is null)
                        {
                            listValue = Context.CreateInstance(listAccessor.TargetType, ObjectKind.Array);
                            prop.SetValue(obj, listValue);
                        }

                        if (Context.TryConvertValue(span, value, listAccessor.ElementType, out value))
                        {
                            listAccessor.AddElement(listValue, value);
                            return true;
                        }
                        else
                        {
                            errorMessage = $"The property value of type {value?.GetType().FullName} couldn't be converted to {listAccessor.ElementType} for the list property {TargetType.FullName}/{name}";
                        }
                    }
                }
                else if (Context.TryConvertValue(span, value, prop.PropertyType, out var newValue))
                {
                    // Coerce the value
                    prop.SetValue(obj, newValue);
                    return true;
                }
                else
                {
                    errorMessage = $"The property value of type {value?.GetType().FullName} couldn't be converted to {prop.PropertyType} for the property {TargetType.FullName}/{name}";
                }
            }
            else
            {
                errorMessage = $"The property {name} was not found on object type {TargetType.FullName}";
            }

            if (Context.IncludeFields)
            {
                if (_fields.TryGetValue(name, out var field))
                {
                    var accessor = Context.GetAccessor(field.FieldType);
                    if (accessor is ListDynamicAccessor listAccessor)
                    {
                        // Coerce the value
                        var listValue = field.GetValue(obj);
                        if (value is not null && field.FieldType.IsInstanceOfType(value))
                        {
                            if (listValue is not null)
                            {
                                foreach (var item in (IEnumerable)value)
                                {
                                    listAccessor.AddElement(listValue, item);
                                }

                                return true;
                            }
                            else
                            {
                                if (Context.TryConvertValue(span, value, field.FieldType, out value))
                                {
                                    field.SetValue(obj, value);
                                    return true;
                                }
                                else
                                {
                                    errorMessage = $"The field value of type {value?.GetType().FullName} couldn't be converted to {field.FieldType} for the list property or field {TargetType.FullName}/{name}";
                                }
                            }
                        }
                        else
                        {
                            if (listValue is null)
                            {
                                listValue = Context.CreateInstance(listAccessor.TargetType, ObjectKind.Array);
                                field.SetValue(obj, listValue);
                            }

                            if (Context.TryConvertValue(span, value, listAccessor.ElementType, out value))
                            {
                                listAccessor.AddElement(listValue, value);
                                return true;
                            }
                            else
                            {
                                errorMessage = $"The field value of type {value?.GetType().FullName} couldn't be converted to {listAccessor.ElementType} for the list property or field {TargetType.FullName}/{name}";
                            }
                        }
                    }
                    else if (Context.TryConvertValue(span, value, field.FieldType, out var newValue))
                    {
                        // Coerce the value
                        field.SetValue(obj, newValue);
                        return true;
                    }
                    else
                    {
                        errorMessage = $"The field value of type {value?.GetType().FullName} couldn't be converted to {field.FieldHandle} for the field {TargetType.FullName}/{name}";
                    }
                }
                else
                {
                    errorMessage = $"The field {name} was not found on object type {TargetType.FullName}";
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = Context.IncludeFields
                ? $"Unexpected error while trying to set property or field {name} was not found on object type {TargetType.FullName}. Reason: {ex.Message}"
                : $"Unexpected error while trying to set property {name} was not found on object type {TargetType.FullName}. Reason: {ex.Message}";
        }

        Context.Diagnostics.Error(span, errorMessage);
        return false;
    }

    public override bool TryGetPropertyType(SourceSpan span, string name, out Type? propertyType)
    {
        propertyType = null;
        if (_props.TryGetValue(name, out var prop))
        {
            propertyType = prop.PropertyType;
            return true;
        }

        if (Context.IncludeFields)
        {
            if (_fields.TryGetValue(name, out var field))
            {
                propertyType = field.FieldType;
                return true;
            }
        }

        // Let's try to recover by using the configured ConvertPropertyName
        // but we emit a warning in that case.
        var otherName = Context.ConvertPropertyName(name);
        if (otherName != name && _props.TryGetValue(otherName, out prop))
        {
            propertyType = prop.PropertyType;
            Context.Diagnostics.Warning(span, $"The property `{name}` was not found, but `{otherName}` was. By default property names are lowered and split by _ by PascalCase letters. This behavior can be changed by passing a TomlModelOptions and specifying the TomlModelOptions.ConvertPropertyName delegate.");
            return true;
        }

        if (Context.IncludeFields)
        {
            // Let's try to recover by using the configured ConvertFieldName
            // but we emit a warning in that case.
            otherName = Context.ConvertFieldName(name);
            if (otherName != name && _fields.TryGetValue(otherName, out var field))
            {
                propertyType = field.FieldType;
                Context.Diagnostics.Warning(span, $"The field `{name}` was not found, but `{otherName}` was. By default field names are lowered and split by _ by PascalCase letters. This behavior can be changed by passing a TomlModelOptions and specifying the TomlModelOptions.ConvertFieldName delegate.");
                return true;
            }
        }

        // If configured to ignore missing properties on the target type,
        // return false to indicate it is missing but don't set an error
        if (!Context.IgnoreMissingProperties)
        {
            // Otherwise, it's an error.
            var errorMessage = Context.IncludeFields
                ? $"The property or field `{name}` was not found on object type {TargetType.FullName}"
                : $"The property `{name}` was not found on object type {TargetType.FullName}";

            Context.Diagnostics.Error(span, errorMessage);
        }
        return false;
    }

    public override bool TryCreateAndSetDefaultPropertyValue(SourceSpan span, object obj, string name, ObjectKind kind, out object? instance)
    {
        instance = null;
        string errorMessage;
        try
        {
            if (_props.TryGetValue(name, out var prop))
            {
                instance = Context.CreateInstance(prop.PropertyType, kind);
                if (instance is not null)
                {
                    prop.SetValue(obj, instance);
                    return true;
                }
            }

            if (Context.IncludeFields)
            {
                if (_fields.TryGetValue(name, out var field))
                {
                    instance = Context.CreateInstance(field.FieldType, kind);
                    if (instance is not null)
                    {
                        field.SetValue(obj, instance);
                        return true;
                    }
                }
            }

            errorMessage = Context.IncludeFields
                ? $"Unable to set the property or field {name} on object type {TargetType.FullName}."
                : $"Unable to set the property {name} on object type {TargetType.FullName}.";
        }
        catch (Exception ex)
        {
            errorMessage = Context.IncludeFields
                ? $"Unexpected error when creating object for property or field {name} on object type {TargetType.FullName}. Reason: {ex.Message}"
                : $"Unexpected error when creating object for property {name} on object type {TargetType.FullName}. Reason: {ex.Message}";
        }

        // If configured to ignore missing properties on the target type,
        // return false to indicate it is missing but don't set an error
        if (!Context.IgnoreMissingProperties)
        {
            Context.Diagnostics.Error(span, errorMessage);
        }
        return false;
    }
}