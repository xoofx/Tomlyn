using System;
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
    /// Default member to create an instance.
    /// </summary>
    public static readonly Func<Type, ObjectKind, object> DefaultCreateInstance = DefaultCreateInstanceImpl;

    public TomlModelOptions()
    {
        GetPropertyName = DefaultGetPropertyNameImpl;
        CreateInstance = DefaultCreateInstance;

        AttributeListForIgnore = new List<string>()
        {
            "System.Runtime.Serialization.IgnoreDataMemberAttribute",
            "System.Text.Json.Serialization.JsonIgnoreAttribute"
        };

        AttributeListForGetName = new List<string>()
        {
            "System.Runtime.Serialization.DataMemberAttribute",
            "System.Text.Json.Serialization.JsonPropertyNameAttribute"
        };
    }

    /// <summary>
    /// Gets or sets the delegate to retrieve a name. If this function returns null, the property is ignored.
    /// </summary>
    public Func<PropertyInfo, string?> GetPropertyName { get; set; }

    public Func<Type, ObjectKind, object> CreateInstance { get; set; }

    public Func<object, Type, object>? ConvertTo { get; set; }

    /// <summary>
    /// Gets the list of the attributes used to ignore a property.
    /// </summary>
    /// <remarks>
    /// By default, the list contains:
    /// - System.Runtime.Serialization.IgnoreDataMemberAttribute
    /// - System.Text.Json.Serialization.JsonPropertyNameAttribute
    /// </remarks>
    public List<string> AttributeListForIgnore { get; }

    /// <summary>
    /// Gets the list of the attributes used to fetch the property `Name`.
    /// </summary>
    /// <remarks>
    /// By default, the list contains:
    /// - System.Runtime.Serialization.DataMemberAttribute
    /// - System.Text.Json.Serialization.JsonPropertyNameAttribute
    /// </remarks>
    public List<string> AttributeListForGetName { get; }

    /// <summary>
    /// Default implementation for getting the property name
    /// </summary>
    private string? DefaultGetPropertyNameImpl(PropertyInfo prop)
    {
        string? name = null;
        foreach (var attribute in prop.GetCustomAttributes())
        {
            var fullName = attribute.GetType().FullName;
            // Check if attribute is ignored
            foreach (var fullNameOfIgnoreAttribute in AttributeListForIgnore)
            {
                if (fullName == fullNameOfIgnoreAttribute)
                {
                    return null;
                }
            }

            // Allow to dynamically bind to JsonPropertyNameAttribute even if we are not targeting netstandard2.0
            foreach (var fullNameOfAttributeWithName in AttributeListForGetName)
            {
                if (fullName == fullNameOfAttributeWithName)
                {
                    var nameProperty = attribute.GetType().GetProperty("Name");
                    if (nameProperty != null && nameProperty.PropertyType == typeof(string))
                    {
                        name = nameProperty.GetValue(attribute) as string;
                        if (name is not null) break;
                    }
                }
            }
        }

        name ??= prop.Name;

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

        return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create an instance of type '{type.FullName}'");
    }
}