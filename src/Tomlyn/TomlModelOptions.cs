using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Tomlyn.Model;

namespace Tomlyn;

public class TomlModelOptions
{
    /// <summary>
    /// Default member renamer.
    /// </summary>
    public static readonly Func<PropertyInfo, string> DefaultGetPropertyName = DefaultGetPropertyNameImpl;

    public static readonly Func<Type, ObjectKind, object> DefaultCreateInstance = DefaultCreateInstanceImpl;

    public TomlModelOptions()
    {
        GetPropertyName = DefaultGetPropertyName;
        CreateInstance = DefaultCreateInstance;
    }
    
    public Func<PropertyInfo, string> GetPropertyName { get; set; }

    public Func<Type, ObjectKind, object> CreateInstance { get; set; }

    public Func<object, Type, object>? ConvertTo { get; set; }

    /// <summary>
    /// Default implementation for getting the property name
    /// </summary>
    private static string DefaultGetPropertyNameImpl(PropertyInfo prop)
    {
        string? name = null;
        foreach (var attribute in prop.GetCustomAttributes())
        {
            // Allow to dynamically bind to JsonPropertyNameAttribute even if we are not targeting netstandard2.0
            if (attribute.GetType().FullName == "System.Text.Json.Serialization.JsonPropertyNameAttribute")
            {
                var nameProperty = attribute.GetType().GetProperty("Name");
                if (nameProperty != null)
                {
                    name = nameProperty.GetValue(attribute) as string;
                }

                break;
            }

            if (attribute is DataMemberAttribute attr && !string.IsNullOrEmpty(attr.Name))
            {
                name = attr.Name;
                break;
            }
        }

        if (name is null)
        {
            name ??= prop.Name;
        }

        var builder = new StringBuilder();
        var pc = (char)0;
        foreach (var c in name)
        {
            if (char.IsUpper(c) && !char.IsUpper(pc) && pc != 0 && pc != '_')
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(c));
            pc = c;
        }

        return builder.ToString();
    }

    private static object DefaultCreateInstanceImpl(Type type, ObjectKind kind)
    {
        if (type == typeof(object))
        {
            switch (kind)
            {
                case ObjectKind.Table:
                case ObjectKind.InlineTable:
                    return new TomlTable(kind == ObjectKind.InlineTable);
                case ObjectKind.TableArray:
                    return new TomlTableArray();
                default:
                    Debug.Assert(kind == ObjectKind.Array);
                    return new TomlArray();
            }
        }

        return Activator.CreateInstance(type);
    }
}