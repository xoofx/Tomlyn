using System;
using System.Reflection;
using Tomlyn.Model;

namespace Tomlyn;

public class TomlModelOptions
{
    public TomlModelOptions()
    {
        GetPropertyName = Toml.GetDefaultPropertyName;
        CreateInstance = Activator.CreateInstance;
    }
    
    public Func<PropertyInfo, string> GetPropertyName { get; set; }

    public Func<Type, object> CreateInstance { get; set; }
}