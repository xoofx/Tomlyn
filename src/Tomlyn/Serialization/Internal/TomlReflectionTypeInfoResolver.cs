using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Tomlyn.Helpers;
using Tomlyn.Serialization;

namespace Tomlyn.Serialization.Internal;

internal static class TomlReflectionTypeInfoResolver
{
    public static TomlTypeInfo? TryCreateTypeInfo(Type type, TomlSerializerOptions options)
    {
        ArgumentGuard.ThrowIfNull(type, nameof(type));
        ArgumentGuard.ThrowIfNull(options, nameof(options));

        if (!IsSupportedPocoType(type))
        {
            return null;
        }

        var members = CollectMembers(type, options);
        if (members.Count == 0)
        {
            return null;
        }

        var constructor = SelectConstructor(type);
        if (type.IsClass && constructor is null)
        {
            return null;
        }

        return new ReflectionObjectTomlTypeInfo(type, options, members, constructor);
    }

    private static ConstructorInfo? SelectConstructor(Type type)
    {
        if (type.IsValueType)
        {
            return null;
        }

        var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? annotated = null;

        for (var i = 0; i < ctors.Length; i++)
        {
            var ctor = ctors[i];
            if (!ctor.IsDefined(typeof(TomlConstructorAttribute), inherit: true) &&
                !ctor.IsDefined(typeof(JsonConstructorAttribute), inherit: true))
            {
                continue;
            }

            if (annotated is not null)
            {
                return null;
            }

            annotated = ctor;
        }

        if (annotated is not null)
        {
            return annotated;
        }

        var parameterless = type.GetConstructor(Type.EmptyTypes);
        if (parameterless is not null)
        {
            return parameterless;
        }

        var publicCtors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        if (publicCtors.Length == 1)
        {
            return publicCtors[0];
        }

        return null;
    }

    private static bool IsSupportedPocoType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum)
        {
            return false;
        }

        if (type == typeof(string) || type == typeof(object))
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return false;
        }

        if (type.IsArray)
        {
            return false;
        }

        if (type.IsGenericTypeDefinition)
        {
            return false;
        }

        // Note: Type.IsByRefLike is not available on netstandard2.0.
#if NETSTANDARD2_0
        return type.IsClass || type.IsValueType;
#else
        return type.IsClass || (type.IsValueType && !type.IsByRefLike);
#endif
    }

    private static List<MemberModel> CollectMembers(Type type, TomlSerializerOptions options)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var members = new List<MemberModel>(properties.Length);

        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            if (property.GetMethod is null || property.GetMethod.GetParameters().Length != 0)
            {
                continue;
            }

            if (!property.GetMethod.IsPublic)
            {
                continue;
            }

            var ignore = GetIgnoreBehavior(property);
            if (ignore.IgnoreAlways)
            {
                continue;
            }

            var name = GetSerializedName(property, property.Name, options);
            if (name is null)
            {
                continue;
            }

            Action<object, object?>? setter = null;
            if (property.SetMethod is not null && (property.SetMethod.IsPublic || HasIncludeAttribute(property)))
            {
                setter = (instance, value) => property.SetValue(instance, value);
            }

            members.Add(new MemberModel(
                property,
                name,
                property.PropertyType,
                instance => property.GetValue(instance),
                setter,
                GetOrder(property),
                ignore.WriteIgnoreCondition,
                GetDefaultValue(property.PropertyType),
                IsRequired(property)));
        }

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var field in fields)
        {
            if (!HasIncludeAttribute(field))
            {
                continue;
            }

            var ignore = GetIgnoreBehavior(field);
            if (ignore.IgnoreAlways)
            {
                continue;
            }

            var name = GetSerializedName(field, field.Name, options);
            if (name is null)
            {
                continue;
            }

            Action<object, object?>? setter = null;
            if (!field.IsInitOnly && !field.IsLiteral)
            {
                setter = (instance, value) => field.SetValue(instance, value);
            }

            members.Add(new MemberModel(
                field,
                name,
                field.FieldType,
                instance => field.GetValue(instance),
                setter,
                GetOrder(field),
                ignore.WriteIgnoreCondition,
                GetDefaultValue(field.FieldType),
                IsRequired(field)));
        }

        return OrderMembers(members, options);
    }

    private static bool HasIncludeAttribute(MemberInfo member)
    {
        if (member.IsDefined(typeof(TomlIncludeAttribute), inherit: true))
        {
            return true;
        }

        if (member.IsDefined(typeof(JsonIncludeAttribute), inherit: true))
        {
            return true;
        }

        return false;
    }

    private static bool IsRequired(MemberInfo member)
    {
        if (member.IsDefined(typeof(TomlRequiredAttribute), inherit: true))
        {
            return true;
        }

        if (member.IsDefined(typeof(JsonRequiredAttribute), inherit: true))
        {
            return true;
        }

        return false;
    }

    private readonly record struct IgnoreBehavior(bool IgnoreAlways, TomlIgnoreCondition? WriteIgnoreCondition);

    private static IgnoreBehavior GetIgnoreBehavior(MemberInfo member)
    {
        var tomlIgnore = member.GetCustomAttribute<TomlIgnoreAttribute>(inherit: true);
        if (tomlIgnore is not null)
        {
            // Interpret the presence of [TomlIgnore] with default Condition as "ignore always" (JsonIgnoreCondition.Always parity).
            return tomlIgnore.Condition == TomlIgnoreCondition.Never
                ? new IgnoreBehavior(IgnoreAlways: true, WriteIgnoreCondition: null)
                : new IgnoreBehavior(IgnoreAlways: false, WriteIgnoreCondition: tomlIgnore.Condition);
        }

        var jsonIgnore = member.GetCustomAttribute<JsonIgnoreAttribute>(inherit: true);
        if (jsonIgnore is not null)
        {
            return jsonIgnore.Condition switch
            {
                JsonIgnoreCondition.Never => new IgnoreBehavior(IgnoreAlways: false, WriteIgnoreCondition: null),
                JsonIgnoreCondition.WhenWritingNull => new IgnoreBehavior(IgnoreAlways: false, WriteIgnoreCondition: TomlIgnoreCondition.WhenWritingNull),
                JsonIgnoreCondition.WhenWritingDefault => new IgnoreBehavior(IgnoreAlways: false, WriteIgnoreCondition: TomlIgnoreCondition.WhenWritingDefault),
                _ => new IgnoreBehavior(IgnoreAlways: true, WriteIgnoreCondition: null),
            };
        }

        return new IgnoreBehavior(IgnoreAlways: false, WriteIgnoreCondition: null);
    }

    private static object? GetDefaultValue(Type type)
    {
        if (!type.IsValueType)
        {
            return null;
        }

        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetSerializedName(MemberInfo member, string defaultName, TomlSerializerOptions options)
    {
        var tomlName = member.GetCustomAttribute<TomlPropertyNameAttribute>(inherit: true);
        if (tomlName is not null)
        {
            return tomlName.Name;
        }

        var jsonName = member.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: true);
        if (jsonName is not null)
        {
            return jsonName.Name;
        }

        var namingPolicy = options.PropertyNamingPolicy;
        if (namingPolicy is not null)
        {
            return namingPolicy.ConvertName(defaultName);
        }

        return defaultName;
    }

    private static int GetOrder(MemberInfo member)
    {
        var tomlOrder = member.GetCustomAttribute<TomlPropertyOrderAttribute>(inherit: true);
        if (tomlOrder is not null)
        {
            return tomlOrder.Order;
        }

        var jsonOrder = member.GetCustomAttribute<JsonPropertyOrderAttribute>(inherit: true);
        if (jsonOrder is not null)
        {
            return jsonOrder.Order;
        }

        return 0;
    }

    private static List<MemberModel> OrderMembers(List<MemberModel> members, TomlSerializerOptions options)
    {
        // Best-effort declaration order using MetadataToken.
        static int DeclarationOrder(MemberModel m)
        {
            try
            {
                return m.Member.MetadataToken;
            }
            catch
            {
                return 0;
            }
        }

        var comparer = StringComparer.Ordinal;

        return options.MappingOrder switch
        {
            TomlMappingOrderPolicy.Declaration => members.OrderBy(DeclarationOrder).ToList(),
            TomlMappingOrderPolicy.Alphabetical => members.OrderBy(m => m.SerializedName, comparer).ToList(),
            TomlMappingOrderPolicy.OrderThenDeclaration => members.OrderBy(m => m.Order).ThenBy(DeclarationOrder).ToList(),
            TomlMappingOrderPolicy.OrderThenAlphabetical => members.OrderBy(m => m.Order).ThenBy(m => m.SerializedName, comparer).ToList(),
            _ => members,
        };
    }

    private readonly record struct MemberModel(
        MemberInfo Member,
        string SerializedName,
        Type MemberType,
        Func<object, object?> Getter,
        Action<object, object?>? Setter,
        int Order,
        TomlIgnoreCondition? WriteIgnoreCondition,
        object? DefaultValue,
        bool IsRequired);

    private static bool ShouldIgnoreValue(object? memberValue, TomlIgnoreCondition ignoreCondition, object? defaultValue)
    {
        return ignoreCondition switch
        {
            TomlIgnoreCondition.WhenWritingNull => memberValue is null,
            TomlIgnoreCondition.WhenWritingDefault => memberValue is null || (defaultValue is not null && Equals(memberValue, defaultValue)),
            _ => false,
        };
    }

    private sealed class ReflectionObjectTomlTypeInfo : TomlTypeInfo
    {
        private readonly List<MemberModel> _members;
        private readonly Dictionary<string, int> _indexByName;
        private readonly ConstructorInfo? _constructor;
        private readonly ParameterBinding[] _parameters;
        private readonly Dictionary<string, int>? _parameterIndexByName;
        private readonly bool _hasRequiredMembers;

        public ReflectionObjectTomlTypeInfo(Type type, TomlSerializerOptions options, List<MemberModel> members, ConstructorInfo? constructor)
            : base(type, options)
        {
            _members = members ?? throw new ArgumentNullException(nameof(members));
            _constructor = constructor;
            _indexByName = new Dictionary<string, int>(
                _members.Count,
                options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            for (var i = 0; i < _members.Count; i++)
            {
                var name = _members[i].SerializedName;
                if (!_indexByName.ContainsKey(name))
                {
                    _indexByName.Add(name, i);
                }
            }

            _hasRequiredMembers = false;
            for (var i = 0; i < _members.Count; i++)
            {
                if (_members[i].IsRequired)
                {
                    _hasRequiredMembers = true;
                    break;
                }
            }

            if (_constructor is null)
            {
                _parameters = Array.Empty<ParameterBinding>();
                _parameterIndexByName = null;
                return;
            }

            var ctorParameters = _constructor.GetParameters();
            if (ctorParameters.Length == 0)
            {
                _parameters = Array.Empty<ParameterBinding>();
                _parameterIndexByName = null;
                return;
            }

            _parameters = new ParameterBinding[ctorParameters.Length];
            _parameterIndexByName = new Dictionary<string, int>(
                ctorParameters.Length,
                options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            for (var i = 0; i < ctorParameters.Length; i++)
            {
                var parameter = ctorParameters[i];
                var parameterName = parameter.Name;
                if (string.IsNullOrEmpty(parameterName))
                {
                    var fallback = $"arg{i}";
                    _parameterIndexByName.Add(fallback, i);
                    _parameters[i] = new ParameterBinding(fallback, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue, MemberIndex: null);
                    continue;
                }

                var memberIndex = FindMemberIndexByClrName(parameterName);
                var keyName = memberIndex >= 0
                    ? _members[memberIndex].SerializedName
                    : options.PropertyNamingPolicy?.ConvertName(parameterName) ?? parameterName;

                if (_parameterIndexByName.ContainsKey(keyName))
                {
                    throw new InvalidOperationException($"Constructor parameter name collision for key '{keyName}' on type '{type.FullName}'.");
                }

                _parameterIndexByName.Add(keyName, i);
                _parameters[i] = new ParameterBinding(keyName, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue, memberIndex >= 0 ? memberIndex : null);
            }
        }

        public override void Write(TomlWriter writer, object? value)
        {
            ArgumentGuard.ThrowIfNull(writer, nameof(writer));

            if (value is null)
            {
                throw new TomlException("TOML does not support null values.");
            }

            writer.WriteStartTable();
            for (var i = 0; i < _members.Count; i++)
            {
                var member = _members[i];
                var memberValue = member.Getter(value);

                var ignoreCondition = member.WriteIgnoreCondition ?? Options.DefaultIgnoreCondition;
                if (ShouldIgnoreValue(memberValue, ignoreCondition, member.DefaultValue))
                {
                    continue;
                }

                writer.WritePropertyName(member.SerializedName);
                var typeInfo = TomlTypeInfoResolverPipeline.Resolve(Options, member.MemberType);
                typeInfo.Write(writer, memberValue);
            }
            writer.WriteEndTable();
        }

        public override object? ReadAsObject(TomlReader reader)
        {
            ArgumentGuard.ThrowIfNull(reader, nameof(reader));

            if (reader.TokenType != TomlTokenType.StartTable)
            {
                throw reader.CreateException($"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.");
            }

            if (_parameters.Length > 0)
            {
                return ReadWithConstructor(reader);
            }

            var instance = CreateInstance();
            var needsSeen = Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error || _hasRequiredMembers;
            var seen = needsSeen ? new bool[_members.Count] : null;

            reader.Read(); // first property or end
            while (reader.TokenType != TomlTokenType.EndTable)
            {
                if (reader.TokenType != TomlTokenType.PropertyName)
                {
                    throw reader.CreateException($"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.");
                }

                var name = reader.PropertyName!;
                if (_indexByName.TryGetValue(name, out var memberIndex))
                {
                    if (seen is not null && seen[memberIndex])
                    {
                        throw reader.CreateException($"Duplicate key '{name}' was encountered.");
                    }

                    if (seen is not null)
                    {
                        seen[memberIndex] = true;
                    }

                    var member = _members[memberIndex];
                    reader.Read(); // value

                    if (member.Setter is null)
                    {
                        reader.Skip();
                        continue;
                    }

                    var typeInfo = TomlTypeInfoResolverPipeline.Resolve(Options, member.MemberType);
                    var memberValue = typeInfo.ReadAsObject(reader);
                    member.Setter(instance, memberValue);
                    continue;
                }

                reader.Read(); // value
                reader.Skip();
            }

            reader.Read(); // consume EndTable

            if (seen is not null)
            {
                ValidateRequiredMembers(seen);
            }

            return instance;
        }

        private object CreateInstance()
        {
            try
            {
                if (_constructor is not null && _constructor.GetParameters().Length == 0)
                {
                    return _constructor.Invoke(Array.Empty<object>())!;
                }

                return Activator.CreateInstance(Type)!;
            }
            catch (Exception ex)
            {
                throw new TomlException($"Failed to create an instance of '{Type.FullName}'.", ex);
            }
        }

        private object ReadWithConstructor(TomlReader reader)
        {
            var ctorArgs = new object?[_parameters.Length];
            var ctorSeen = new bool[_parameters.Length];
            var memberValues = new object?[_members.Count];
            var memberSeen = new bool[_members.Count];

            reader.Read(); // first property or end
            while (reader.TokenType != TomlTokenType.EndTable)
            {
                if (reader.TokenType != TomlTokenType.PropertyName)
                {
                    throw reader.CreateException($"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.");
                }

                var name = reader.PropertyName!;

                if (_parameterIndexByName is not null && _parameterIndexByName.TryGetValue(name, out var parameterIndex))
                {
                    if (ctorSeen[parameterIndex] && Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)
                    {
                        throw reader.CreateException($"Duplicate key '{name}' was encountered.");
                    }

                    ctorSeen[parameterIndex] = true;
                    var binding = _parameters[parameterIndex];

                    reader.Read(); // value
                    var typeInfo = TomlTypeInfoResolverPipeline.Resolve(Options, binding.ParameterType);
                    var value = typeInfo.ReadAsObject(reader);
                    ctorArgs[parameterIndex] = value;

                    if (binding.MemberIndex is { } linkedMemberIndex && linkedMemberIndex >= 0 && linkedMemberIndex < _members.Count)
                    {
                        memberSeen[linkedMemberIndex] = true;
                        memberValues[linkedMemberIndex] = value;
                    }

                    continue;
                }

                if (_indexByName.TryGetValue(name, out var memberIndex))
                {
                    if (memberSeen[memberIndex] && Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)
                    {
                        throw reader.CreateException($"Duplicate key '{name}' was encountered.");
                    }

                    memberSeen[memberIndex] = true;
                    var member = _members[memberIndex];

                    reader.Read(); // value
                    if (member.Setter is null)
                    {
                        reader.Skip();
                        continue;
                    }

                    var typeInfo = TomlTypeInfoResolverPipeline.Resolve(Options, member.MemberType);
                    var value = typeInfo.ReadAsObject(reader);
                    memberValues[memberIndex] = value;
                    continue;
                }

                reader.Read(); // value
                reader.Skip();
            }

            reader.Read(); // consume EndTable

            for (var i = 0; i < _parameters.Length; i++)
            {
                if (ctorSeen[i])
                {
                    continue;
                }

                var binding = _parameters[i];
                if (binding.HasDefaultValue)
                {
                    ctorArgs[i] = binding.DefaultValue;
                    continue;
                }

                throw new TomlException($"Missing required constructor parameter '{binding.KeyName}' when deserializing '{Type.FullName}'.");
            }

            ValidateRequiredMembers(memberSeen);

            object instance;
            try
            {
                instance = _constructor!.Invoke(ctorArgs)!;
            }
            catch (Exception ex)
            {
                throw new TomlException($"Failed to create an instance of '{Type.FullName}'.", ex);
            }

            for (var i = 0; i < _members.Count; i++)
            {
                if (!memberSeen[i])
                {
                    continue;
                }

                var member = _members[i];
                if (member.Setter is null)
                {
                    continue;
                }

                member.Setter(instance, memberValues[i]);
            }

            return instance;
        }

        private void ValidateRequiredMembers(bool[] seen)
        {
            for (var i = 0; i < _members.Count; i++)
            {
                var member = _members[i];
                if (!member.IsRequired)
                {
                    continue;
                }

                if (!seen[i])
                {
                    throw new TomlException($"Missing required TOML key '{member.SerializedName}' when deserializing '{Type.FullName}'.");
                }
            }
        }

        private int FindMemberIndexByClrName(string parameterName)
        {
            for (var i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Member.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly record struct ParameterBinding(string KeyName, Type ParameterType, bool HasDefaultValue, object? DefaultValue, int? MemberIndex);
    }
}
