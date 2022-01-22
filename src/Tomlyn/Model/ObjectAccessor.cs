using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Tomlyn.Syntax;

namespace Tomlyn.Model;

class ObjectAccessor : AccessorBase
{
    private readonly Dictionary<string, PropertyInfo> _props;
    private readonly AccessorRegistry _registry;
    private readonly TomlModelContext _context;

    public ObjectAccessor(AccessorRegistry registry, Type type, TomlModelContext context) : base(type)
    {
        _props = new Dictionary<string, PropertyInfo>();
        _registry = registry;
        _context = context;
        Initialize();
    }

    private void Initialize()
    {
        Type? type = TargetType;
        while (type is not null)
        {
            foreach (var prop in TargetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = _context.GetPropertyName(prop);
                if (!_props.ContainsKey(name))
                {
                    _props[name] = prop;
                }
            }

            type = type.BaseType;
        }
    }

    public bool TryGetPropertyValue(object obj, string name, out object? value)
    {
        value = null;
        if (_props.TryGetValue(name, out var prop))
        {
            value = prop.GetValue(obj);
            return true;
        }

        return false;
    }

    public bool TrySetPropertyValue(SourceSpan span, object obj, string name, object? value)
    {
        if (_props.TryGetValue(name, out var prop))
        {
            var accessor = _registry.GetAccessor(prop.PropertyType);
            if (accessor is ListAccessor listAccessor)
            {
                // Coerce the value
                var listValue = prop.GetValue(obj);
                if (value is not null && prop.PropertyType.IsInstanceOfType(value))
                {
                    if (prop.SetMethod is not null)
                    {
                        prop.SetValue(obj, _registry.ConvertValue(value, prop.PropertyType));
                    }
                    else
                    {
                        foreach (var item in (IEnumerable)value)
                        {
                            listAccessor.AddElement(listValue, item);
                        }
                    }
                }
                else
                {
                    if (listValue is null)
                    {
                        listValue = _context.CreateInstance(listAccessor.TargetType);
                        prop.SetValue(obj, listValue);
                    }

                    listAccessor.AddElement(listValue, _registry.ConvertValue(value, listAccessor.ElementType));
                }
            }
            else
            {
                // Coerce the value
                prop.SetValue(obj, _registry.ConvertValue(value, prop.PropertyType));
            }
            return true;

        }
        else
        {
            _context.Diagnostics.Error(span, $"The property {name} was not found on object type {TargetType.FullName}");
            return false;
        }
    }

    public bool TryGetPropertyType(SourceSpan span, string name, out Type? propertyType)
    {
        propertyType = null;
        if (_props.TryGetValue(name, out var prop))
        {

            propertyType = prop.PropertyType;
            return true;
        }
        _context.Diagnostics.Error(span,$"The property {name} was not found on object type {TargetType.FullName}");
        return false;
    }

    public object? CreateAndSetDefaultPropertyValue(SourceSpan span, object obj, string name)
    {
        object? newObject = null;
        if (_props.TryGetValue(name, out var prop))
        {
            newObject = _context.CreateInstance(prop.PropertyType);
            prop.SetValue(obj, _context.CreateInstance(prop.PropertyType));
        }
        else
        {
            _context.Diagnostics.Error(span, $"The property {name} was not found on object type {TargetType.FullName}");
        }

        return newObject;
    }
}