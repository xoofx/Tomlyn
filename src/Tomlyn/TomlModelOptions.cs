using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Tomlyn.Helpers;
using Tomlyn.Model;

namespace Tomlyn;

public class TomlModelOptions
{
    /// <summary>
    /// Default member to create an instance.
    /// </summary>
    public static readonly Func<Type, ObjectKind, object> DefaultCreateInstance = DefaultCreateInstanceImpl;

    /// <summary>
    /// Default convert name using snake case via help <see cref="TomlNamingHelper.PascalToSnakeCase"/>.
    /// </summary>
    public static readonly Func<string, string> DefaultConvertPropertyName = TomlNamingHelper.PascalToSnakeCase;

    public TomlModelOptions()
    {
        GetPropertyName = DefaultGetPropertyNameImpl;
        CreateInstance = DefaultCreateInstance;
        ConvertPropertyName = DefaultConvertPropertyName;

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
    /// Gets or sets the delegate to retrieve a name from a property. If this function returns null, the property is ignored.
    /// </summary>
    public Func<PropertyInfo, string?> GetPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the delegate used to convert the name of the property to the name used in TOML. By default, it is using snake case via <see cref="TomlNamingHelper.PascalToSnakeCase"/>.
    /// </summary>
    /// <remarks>
    /// This delegate is used by the default <see cref="GetPropertyName"/> delegate.
    /// </remarks>
    public Func<string, string> ConvertPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the function used when deserializing from TOML to create instance of objects. Default is set to <see cref="DefaultCreateInstance"/>.
    /// The arguments of the function are:
    /// - The type to create an instance for.
    /// - The expected <see cref="ObjectKind"/> from a TOML perspective.
    /// Returns an instance according to the type and the expected <see cref="ObjectKind"/>.
    /// </summary>
    public Func<Type, ObjectKind, object> CreateInstance { get; set; }

    /// <summary>
    /// Gets or sets the convert function called when deserializing a value from TOML to a model (e.g string to Uri).
    /// 
    /// Must return null if cannot convert. The arguments of the function are:
    /// - The input object value to convert.
    /// - The target type to convert to.
    /// 
    /// Returns an instance of target type converted from the input value or null if conversion is not supported.
    /// </summary>
    [Obsolete($"Use {nameof(ConvertToModel)} instead")]
    public Func<object, Type, object?>? ConvertTo
    {
        get => ConvertToModel;
        set => ConvertToModel = value;
    }

    /// <summary>
    /// Gets or sets the convert function called when deserializing a value from TOML to a model (e.g string to Uri).
    /// 
    /// Must return null if cannot convert. The arguments of the function are:
    /// - The input object value to convert.
    /// - The target type to convert to.
    /// 
    /// Returns an instance of target type converted from the input value or null if conversion is not supported.
    /// </summary>
    public Func<object, Type, object?>? ConvertToModel { get; set; }

    /// <summary>
    /// Gets or sets the convert function called when serializing a value from a model to a TOML representation.
    /// This function allows to substitute a value to another type before converting (e.g Uri to string).
    /// </summary>
    public Func<object, object?>? ConvertToToml { get; set; }

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

        return ConvertPropertyName(name ?? prop.Name);
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