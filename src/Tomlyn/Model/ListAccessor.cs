using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Tomlyn.Model;

class ListAccessor : AccessorBase
{
    private readonly Type _elementType;
    private readonly MethodInfo _addMethod;

    private ListAccessor(Type type, Type elementType, MethodInfo addMethod) : base(type)
    {
        _elementType = elementType;
        _addMethod = addMethod;
    }

    public Type ElementType => _elementType;

    public void AddElement(object list, object? value)
    {
        _addMethod.Invoke(list, new [] { value });
    }

    public static bool TryGetListAccessor(Type type, out ListAccessor? accessor)
    {
        var listType = type;
        accessor = null;
        if (!typeof(IEnumerable).IsAssignableFrom(type) || type == typeof(string)) return false;

        var interfaces = type.GetInterfaces();
        Type? elementType = null;
        foreach (var i in interfaces)
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = i.GetGenericArguments()[0];
                break;
            }
        }

        if (elementType is null)
        {
            throw new InvalidOperationException("The collection ");
        }

        bool methodFound = false;
        Type? visitType = type;
        MethodInfo? addMethod = null;
        while (!methodFound && visitType is not null)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name == "Add" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == elementType)
                {
                    addMethod = method;
                    methodFound = true;
                    break;
                }
            }
            visitType = visitType.BaseType;
        }

        if (!methodFound)
        {
            throw new InvalidOperationException($"The collection {type.GetType().FullName} does not contain a method `void Add({elementType.FullName})`");
        }

        accessor = new ListAccessor(listType, elementType, addMethod!);
        return true;
    }
}