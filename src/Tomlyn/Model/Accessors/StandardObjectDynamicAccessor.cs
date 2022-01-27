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
    private readonly List<KeyValuePair<string, PropertyInfo>> _orderedProps;

    public StandardObjectDynamicAccessor(DynamicModelReadContext context, Type type) : base(context, type)
    {
        _props = new Dictionary<string, PropertyInfo>();
        _orderedProps = new List<KeyValuePair<string, PropertyInfo>>();
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
    }

    public override IEnumerable<KeyValuePair<string, object?>> GetProperties(object obj)
    {
        foreach (var prop in _orderedProps)
        {
            yield return new KeyValuePair<string, object?>(prop.Key, prop.Value.GetValue(obj));
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
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error while trying to set property {name} was not found on object type {TargetType.FullName}. Reason: {ex.Message}";
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
        Context.Diagnostics.Error(span, $"The property {name} was not found on object type {TargetType.FullName}");
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
            errorMessage = $"Unable to set the property {name} on object type {TargetType.FullName}.";
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error when creating object for property {name} on object type {TargetType.FullName}. Reason: {ex.Message}";
        }

        Context.Diagnostics.Error(span, errorMessage);
        return false;
    }
}