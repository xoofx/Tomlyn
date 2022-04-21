using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Tomlyn.Model.Accessors;

internal class ListDynamicAccessor : DynamicAccessor
{
    private readonly PropertyInfo? _propIndexer;
    private readonly PropertyInfo? _propCount;
    private readonly MethodInfo? _addMethod;

    public ListDynamicAccessor(DynamicModelReadContext context, Type type, Type elementType) : base(context, type, ReflectionObjectKind.Collection)
    {
        ElementType = elementType;

        // For fast types, we won't try to bind with reflection
        if (IsFastType(type)) return;

        int requiredProp = 0;
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
        {
            var indexParameters = prop.GetIndexParameters();
            if (prop.Name == "Count" && indexParameters.Length == 0 && prop.PropertyType == typeof(int))
            {
                _propCount = prop;
                requiredProp |= 1;
                continue;
            }

            if (indexParameters.Length != 1) continue;

            if (indexParameters[0].ParameterType == typeof(int) && prop.PropertyType == elementType)
            {
                _propIndexer = prop;
                requiredProp |= 2;
            }

            // Stop if we have found all required properties
            if (requiredProp == 3) break;
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
            if (method.Name == "Add" && method.GetParameters().Length == 1 && method.ReturnType == typeof(void) && method.GetParameters()[0].ParameterType == elementType)
            {
                _addMethod = method;
                break;
            }
        }
    }

    private static bool IsFastType(Type type)
    {
        return typeof(IList).IsAssignableFrom(type) || typeof(TomlArray).IsAssignableFrom(type) || typeof(TomlTableArray).IsAssignableFrom(type);
    }

    public Type ElementType { get; }

    public IEnumerable<object?> GetElements(object obj)
    {
        if (obj is IEnumerable<object?> it)
        {
            return it;
        }
        else if (obj is IEnumerable oldIt)
        {
            return EnumToEnumObject(oldIt);
        }
        else
        {
            return EnumFromCollection(obj);
        }
    }

    private IEnumerable<object?> EnumFromCollection(object obj)
    {
        var count = GetCount(obj);
        for (int i = 0; i < count; i++)
        {
            yield return GetElementAt(obj, i);
        }
    }

    private static IEnumerable<object?> EnumToEnumObject(IEnumerable it)
    {
        foreach (var obj in it)
        {
            yield return obj;
        }
    }

    public int GetCount(object list)
    {
        switch (list)
        {
            case ICollection fastList:
                return fastList.Count;
            case TomlArray array:
                return array.Count;
            case TomlTableArray array:
                return array.Count;
            default:
                return (int)_propCount!.GetValue(list)!;
        }
    }
    
    public object? GetElementAt(object list, int index)
    {
        switch (list)
        {
            case IList fastList:
                return fastList[index];
            case TomlArray array:
                return array[index];
            case TomlTableArray array:
                return array[index];
            default:
                return _propIndexer!.GetValue(list, new object[] { index });
        }
    }

    public object? GetLastElement(object list)
    {
        switch (list)
        {
            case IList fastList:
                return fastList[fastList.Count - 1];
            case TomlArray array:
                return array[array.Count - 1];
            case TomlTableArray array:
                return array[array.Count - 1];
            default:
                return GetElementAt(list, GetCount(list) - 1);
        }
    }

    public void AddElement(object list, object? value)
    {
        switch (list)
        {
            case IList fastList:
                fastList.Add(value);
                break;
            case TomlArray array:
                array.Add(value);
                break;
            case TomlTableArray array:
                array.Add((TomlTable)value!);
                break;
            default:
                _addMethod!.Invoke(list, new[] { value });
                break;
        }
    }
}