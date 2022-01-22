using System;
using System.Collections.Generic;

namespace Tomlyn.Model;

class AccessorRegistry
{
    private readonly TomlModelContext _context;
    private readonly Dictionary<Type, AccessorBase> _accessors;

    public AccessorRegistry(TomlModelContext context)
    {
        _context = context;
        _accessors = new Dictionary<Type, AccessorBase>();
    }

    public AccessorBase GetAccessor(Type type)
    {
        if (!_accessors.TryGetValue(type, out var accessor)) {
            if (ListAccessor.TryGetListAccessor(type, out var listAccessor))
            {
                accessor = listAccessor;
            }
            else
            {
                accessor = new ObjectAccessor(this, type, _context); ;
            }
            _accessors.Add(type, accessor!);
        }
        return accessor!;
    }

    public object? ConvertValue(object? value, Type changeType)
    {
        if (typeof(Enum).IsAssignableFrom(changeType) && value is string text)
        {
            // TryParse(Type enumType, string? value, bool ignoreCase, out object? result);
            value = Enum.Parse(changeType, text, true);
        }

        if (value is null) return value;

        if (changeType.IsInstanceOfType(value))
        {
            return value;
        }

        return Convert.ChangeType(value, changeType);
    }
}