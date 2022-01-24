// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Tomlyn.Syntax;

namespace Tomlyn.Model.Accessors;

internal class DictionaryDynamicAccessor : ObjectDynamicAccessor
{
    private readonly DictionaryAccessor _dictionaryAccessor;

    public DictionaryDynamicAccessor(DynamicModelReadContext context, Type type, Type keyType, Type valueType) : base(context, type)
    {
        if (TargetType == typeof(TomlTable))
        {
            _dictionaryAccessor = TomlTableAccessor.Instance;
        }
        else
        {
            if (keyType == typeof(string) && valueType == typeof(object))
            {
                _dictionaryAccessor = FastDictionaryAccessor.Instance;
            }
            else
            {
                _dictionaryAccessor = new SlowDictionaryAccessor(context, TargetType, keyType, valueType);
            }
        }
    }

    public override IEnumerable<KeyValuePair<string, object?>> GetProperties(object obj)
    {
        return _dictionaryAccessor.GetElements(obj);
    }

    public override bool TryGetPropertyValue(SourceSpan span, object obj, string name, out object? value)
    {
        value = null;
        if (Context.TryConvertValue(span, name, _dictionaryAccessor.KeyType, out var itemKey))
        {
            return _dictionaryAccessor.TryGetValue(obj, itemKey!, out value);
        }

        Context.Diagnostics.Error(span, $"Cannot convert string key {name} to dictionary key {_dictionaryAccessor.KeyType.FullName} on object type {TargetType.FullName}");
        return false;
    }

    public override bool TrySetPropertyValue(SourceSpan span, object obj, string name, object? value)
    {
        string? errorMessage;
        try
        {
            if (Context.TryConvertValue(span, name, _dictionaryAccessor.KeyType, out var itemKey))
            {
                if (Context.TryConvertValue(span, value, _dictionaryAccessor.ValueType, out var itemValue))
                {
                    _dictionaryAccessor.SetKeyValue(obj, itemKey!, itemValue!);
                    return true;
                }
                errorMessage = $"Cannot convert string key {name} to dictionary key {_dictionaryAccessor.KeyType.FullName} on object type {TargetType.FullName}";
            }
            else
            {
                errorMessage = $"Cannot convert string key {name} to dictionary key {_dictionaryAccessor.KeyType.FullName} on object type {TargetType.FullName}";
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
        propertyType = _dictionaryAccessor.ValueType;
        return true;
    }

    public override bool TryCreateAndSetDefaultPropertyValue(SourceSpan span, object obj, string name, ObjectKind kind, out object? instance)
    {
        instance = null;
        string errorMessage;
        try
        {
            instance = Context.CreateInstance(_dictionaryAccessor.ValueType, kind);
            if (instance != null)
            {
                if (Context.TryConvertValue(span, name, _dictionaryAccessor.KeyType, out var key))
                {
                    _dictionaryAccessor.SetKeyValue(obj, key!, instance);
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

    private abstract class DictionaryAccessor
    {
        public DictionaryAccessor(Type keyType, Type valueType)
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        public Type KeyType { get; }

        public Type ValueType { get; }

        public abstract IEnumerable<KeyValuePair<string, object?>> GetElements(object dictionary);

        public abstract bool TryGetValue(object dictionary, object key, out object? value);

        public abstract void SetKeyValue(object dictionary, object key, object value);
    }

    private class TomlTableAccessor : DictionaryAccessor
    {
        public static readonly DictionaryAccessor Instance = new TomlTableAccessor();

        private TomlTableAccessor() : base(typeof(string), typeof(object))
        {
        }

        public override IEnumerable<KeyValuePair<string, object?>> GetElements(object dictionary)
        {
            return ((TomlTable)dictionary);
        }

        public override bool TryGetValue(object dictionary, object key, out object? value)
        {
            return ((TomlTable)dictionary).TryGetValue((string)key, out value);
        }

        public override void SetKeyValue(object dictionary, object key, object value)
        {
            ((TomlTable)dictionary)[(string)key] = value;
        }
    }

    private sealed class FastDictionaryAccessor : DictionaryAccessor
    {
        public static readonly DictionaryAccessor Instance = new FastDictionaryAccessor();

        private FastDictionaryAccessor() : base(typeof(string), typeof(object))
        {
        }

        public override IEnumerable<KeyValuePair<string, object?>> GetElements(object dictionary)
        {
            return ((IDictionary<string, object?>)dictionary);
        }

        public override bool TryGetValue(object dictionary, object key, out object? value)
        {
            return ((IDictionary<string, object?>)dictionary).TryGetValue((string)key, out value);
        }

        public override void SetKeyValue(object dictionary, object key, object value)
        {
            ((IDictionary<string, object?>)dictionary)[(string)key] = value;
        }
    }

    private sealed class SlowDictionaryAccessor : DictionaryAccessor
    {
        private readonly DynamicModelReadContext _context;
        private readonly PropertyInfo _propSetter;
        private readonly MethodInfo _methodTryGetValue;

        public SlowDictionaryAccessor(DynamicModelReadContext context, Type dictionaryType, Type keyType, Type valueType) : base(keyType, valueType)
        {
            _context = context;
            // Find the property setter
            PropertyInfo? propSetter = null;
            foreach (var prop in dictionaryType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                var indexParameters = prop.GetIndexParameters();
                if (indexParameters.Length != 1) continue;

                if (indexParameters[0].ParameterType == KeyType && prop.PropertyType == valueType)
                {
                    propSetter = prop;
                    break;
                }
            }

            Debug.Assert(propSetter is not null);
            _propSetter = propSetter!;

            MethodInfo? methodTryGetValue = null;
            foreach (var method in dictionaryType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                var parameters = method.GetParameters();
                if (method.Name == "TryGetValue" && method.ReturnType == typeof(bool) && parameters.Length == 2 && parameters[0].ParameterType == KeyType && parameters[1].IsOut && parameters[1].ParameterType == valueType)
                {
                    methodTryGetValue = method;
                    break;
                }
            }
            Debug.Assert(methodTryGetValue is not null);
            _methodTryGetValue = methodTryGetValue!;

        }

        public override IEnumerable<KeyValuePair<string, object?>> GetElements(object dictionary)
        {
            var it = (IEnumerable)dictionary;
            var enumerator = (IDictionaryEnumerator)it.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return new KeyValuePair<string, object?>((string)_context.ConvertTo!(enumerator.Key, typeof(string)), enumerator.Value);
            }
        }

        public override bool TryGetValue(object dictionary, object key, out object? value)
        {
            var parameters = new object?[2] { key, null };
            var result = (bool)_methodTryGetValue.Invoke(dictionary, parameters);
            value = parameters[1];
            return result;
        }

        public override void SetKeyValue(object dictionary, object key, object value)
        {
            _propSetter.SetValue(dictionary, value, new object[] { key } );
        }
    }
}