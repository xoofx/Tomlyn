using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Tomlyn.Helpers;
using Tomlyn.Serialization;
using Tomlyn.Serialization.Internal;

namespace Tomlyn;

/// <summary>
/// Serializes and deserializes TOML payloads, following a <c>System.Text.Json</c>-style API shape.
/// </summary>
public static class TomlSerializer
{
    private const int CachedStringBuilderCapacity = 1024;

    [ThreadStatic]
    private static StringBuilder? t_cachedStringBuilder;

    private const string ReflectionBasedSerializationMessage =
        "Reflection-based TOML serialization is not compatible with trimming/NativeAOT. " +
        "Use a source-generated TomlSerializerContext or pass a TomlTypeInfo instance.";

    private static readonly bool ReflectionEnabledByDefault = TomlSerializerFeatureSwitches.IsReflectionEnabledByDefaultCalculated;
    private static readonly Encoding DefaultStreamEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Gets a value indicating whether reflection-based serialization is enabled by default.
    /// </summary>
    public static bool IsReflectionEnabledByDefault => ReflectionEnabledByDefault;

    /// <summary>
    /// Serializes a value into TOML text.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static string Serialize<T>(T value, TomlSerializerOptions? options = null)
        => Serialize((object?)value, typeof(T), options);

    /// <summary>
    /// Serializes a value into TOML text using generated metadata from a serializer context.
    /// </summary>
    public static string Serialize<T>(T value, TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(context, nameof(context));
        return Serialize(value, typeof(T), context);
    }

    /// <summary>
    /// Serializes a value into TOML text using an explicit input type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static string Serialize(object? value, Type inputType, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(inputType, nameof(inputType));
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var typeInfo = ResolveTypeInfo(effectiveOptions, inputType);
        return SerializeToString(value, typeInfo);
    }

    /// <summary>
    /// Serializes a value into TOML text using generated metadata from a serializer context.
    /// </summary>
    public static string Serialize(object? value, Type inputType, TomlSerializerContext context)
    {
        ArgumentGuard.ThrowIfNull(inputType, nameof(inputType));
        ArgumentGuard.ThrowIfNull(context, nameof(context));

        var typeInfo = ResolveTypeInfo(context, inputType);
        return SerializeToString(value, typeInfo);
    }

    /// <summary>
    /// Serializes a value into TOML text using explicit metadata.
    /// </summary>
    public static string Serialize<T>(T value, TomlTypeInfo<T> typeInfo)
    {
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        return SerializeToString(value, typeInfo);
    }

    /// <summary>
    /// Serializes a value to a writer using explicit metadata.
    /// </summary>
    public static void Serialize<T>(TextWriter writer, T value, TomlTypeInfo<T> typeInfo)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        Serialize(writer, (object?)value, (TomlTypeInfo)typeInfo);
    }

    /// <summary>
    /// Serializes a value to a stream using explicit metadata.
    /// </summary>
    public static void Serialize<T>(Stream utf8Stream, T value, TomlTypeInfo<T> typeInfo)
    {
        ArgumentGuard.ThrowIfNull(utf8Stream, nameof(utf8Stream));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        using var writer = new StreamWriter(utf8Stream, DefaultStreamEncoding, bufferSize: 1024, leaveOpen: true);
        Serialize(writer, value, typeInfo);
        writer.Flush();
    }

    /// <summary>
    /// Serializes a value into TOML text using explicit metadata.
    /// </summary>
    public static string Serialize(object? value, TomlTypeInfo typeInfo)
    {
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        return SerializeToString(value, typeInfo);
    }

    private static string SerializeToString(object? value, TomlTypeInfo typeInfo)
    {
        var builder = t_cachedStringBuilder;
        if (builder is null)
        {
            builder = new StringBuilder(CachedStringBuilderCapacity);
            t_cachedStringBuilder = builder;
        }
        else
        {
            builder.Clear();
        }

        string result;
        try
        {
            using var writer = new StringWriter(builder, CultureInfo.InvariantCulture);
            Serialize(writer, value, typeInfo);
            result = builder.ToString();
        }
        finally
        {
            builder.Clear();
        }

        return result;
    }

    /// <summary>
    /// Serializes a value to a writer.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static void Serialize<T>(TextWriter writer, T value, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        Serialize(writer, (object?)value, typeof(T), options);
    }

    /// <summary>
    /// Serializes a value to a writer using an explicit input type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static void Serialize(TextWriter writer, object? value, Type inputType, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(inputType, nameof(inputType));
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var typeInfo = ResolveTypeInfo(effectiveOptions, inputType);
        Serialize(writer, value, typeInfo);
    }

    /// <summary>
    /// Serializes a value to a stream using UTF-8 encoding.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static void Serialize<T>(Stream utf8Stream, T value, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(utf8Stream, nameof(utf8Stream));
        Serialize(utf8Stream, (object?)value, typeof(T), options);
    }

    /// <summary>
    /// Serializes a value to a stream using UTF-8 encoding and an explicit input type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static void Serialize(Stream utf8Stream, object? value, Type inputType, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(utf8Stream, nameof(utf8Stream));
        ArgumentGuard.ThrowIfNull(inputType, nameof(inputType));

        using var writer = new StreamWriter(utf8Stream, DefaultStreamEncoding, bufferSize: 1024, leaveOpen: true);
        Serialize(writer, value, inputType, options);
        writer.Flush();
    }

    /// <summary>
    /// Serializes a value to a stream using UTF-8 encoding and explicit metadata.
    /// </summary>
    public static void Serialize(Stream utf8Stream, object? value, TomlTypeInfo typeInfo)
    {
        ArgumentGuard.ThrowIfNull(utf8Stream, nameof(utf8Stream));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));

        using var writer = new StreamWriter(utf8Stream, DefaultStreamEncoding, bufferSize: 1024, leaveOpen: true);
        Serialize(writer, value, typeInfo);
        writer.Flush();
    }

    /// <summary>
    /// Deserializes a TOML payload from text.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static T? Deserialize<T>(string toml, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var typeInfo = ResolveTypeInfo(effectiveOptions, typeof(T));
        return (T?)Deserialize(toml, typeInfo);
    }

    /// <summary>
    /// Deserializes a TOML payload from text using explicit metadata.
    /// </summary>
    public static T? Deserialize<T>(string toml, TomlTypeInfo<T> typeInfo)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        return (T?)Deserialize(toml, (TomlTypeInfo)typeInfo);
    }

    /// <summary>
    /// Deserializes a TOML payload from text into an explicit destination type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static object? Deserialize(string toml, Type returnType, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        ArgumentGuard.ThrowIfNull(returnType, nameof(returnType));

        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var typeInfo = ResolveTypeInfo(effectiveOptions, returnType);
        return Deserialize(toml, typeInfo);
    }

    /// <summary>
    /// Deserializes a TOML payload from UTF-8 bytes.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static T? Deserialize<T>(byte[] utf8Toml, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(utf8Toml, nameof(utf8Toml));
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var typeInfo = ResolveTypeInfo(effectiveOptions, typeof(T));
        return (T?)DeserializeUtf8(utf8Toml, typeInfo);
    }

    /// <summary>
    /// Deserializes a TOML payload from UTF-8 bytes using explicit metadata.
    /// </summary>
    public static T? Deserialize<T>(byte[] utf8Toml, TomlTypeInfo<T> typeInfo)
    {
        ArgumentGuard.ThrowIfNull(utf8Toml, nameof(utf8Toml));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        return (T?)DeserializeUtf8(utf8Toml, (TomlTypeInfo)typeInfo);
    }

    /// <summary>
    /// Deserializes a TOML payload from UTF-8 bytes into an explicit destination type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static object? Deserialize(byte[] utf8Toml, Type returnType, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(utf8Toml, nameof(utf8Toml));
        ArgumentGuard.ThrowIfNull(returnType, nameof(returnType));

        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var typeInfo = ResolveTypeInfo(effectiveOptions, returnType);
        return DeserializeUtf8(utf8Toml, typeInfo);
    }

    /// <summary>
    /// Deserializes a TOML payload from UTF-8 bytes using explicit metadata.
    /// </summary>
    public static object? Deserialize(byte[] utf8Toml, TomlTypeInfo typeInfo)
    {
        ArgumentGuard.ThrowIfNull(utf8Toml, nameof(utf8Toml));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        return DeserializeUtf8(utf8Toml, typeInfo);
    }

    /// <summary>
    /// Attempts to deserialize a TOML payload from text.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static bool TryDeserialize<T>(string toml, out T? value, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        try
        {
            value = Deserialize<T>(toml, options);
            return true;
        }
        catch (TomlException)
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to deserialize a TOML payload from text into an explicit destination type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    public static bool TryDeserialize(string toml, Type returnType, out object? value, TomlSerializerOptions? options = null)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        ArgumentGuard.ThrowIfNull(returnType, nameof(returnType));
        try
        {
            value = Deserialize(toml, returnType, options);
            return true;
        }
        catch (TomlException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Deserializes a TOML payload from text using explicit metadata.
    /// </summary>
    public static object? Deserialize(string toml, TomlTypeInfo typeInfo)
    {
        ArgumentGuard.ThrowIfNull(toml, nameof(toml));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));

        var options = typeInfo.Options;
        var reader = TomlReader.Create(toml, options);
        return DeserializeCore(reader, typeInfo, options);
    }

    private static object? DeserializeUtf8(byte[] utf8Toml, TomlTypeInfo typeInfo)
    {
        ArgumentGuard.ThrowIfNull(utf8Toml, nameof(utf8Toml));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));

        var options = typeInfo.Options;
        var reader = TomlReader.Create(utf8Toml, options);
        return DeserializeCore(reader, typeInfo, options);
    }

    private static object? DeserializeCore(TomlReader reader, TomlTypeInfo typeInfo, TomlSerializerOptions options)
    {
        reader.Read(); // StartDocument
        reader.Read(); // value start

        if (options.RootValueHandling == TomlRootValueHandling.WrapInRootKey)
        {
            if (reader.TokenType != TomlTokenType.StartTable)
            {
                throw reader.CreateException($"Expected a TOML table at the document root but was {reader.TokenType}.");
            }

            reader.Read();
            while (reader.TokenType != TomlTokenType.EndTable)
            {
                if (reader.TokenType != TomlTokenType.PropertyName)
                {
                    throw reader.CreateException($"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.");
                }

                var name = reader.PropertyName!;
                reader.Read();
                if (string.Equals(name, options.RootValueKeyName, StringComparison.Ordinal))
                {
                    return typeInfo.ReadAsObject(reader);
                }

                reader.Skip();
            }

            throw reader.CreateException($"The root value key '{options.RootValueKeyName}' was not found.");
        }

        return typeInfo.ReadAsObject(reader);
    }

    /// <summary>
    /// Serializes a value to a writer using explicit metadata.
    /// </summary>
    public static void Serialize(TextWriter writer, object? value, TomlTypeInfo typeInfo)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));

        var options = typeInfo.Options;
        if (options.RootValueHandling != TomlRootValueHandling.WrapInRootKey &&
            options.MetadataStore is null &&
            typeInfo.Type == typeof(Tomlyn.Model.TomlTable) &&
            value is Tomlyn.Model.TomlTable rootTable)
        {
            Tomlyn.Serialization.Internal.TomlModelTextWriter.WriteDocument(writer, rootTable, options);
            return;
        }

        var tomlWriter = new TomlWriter(writer, options);
        tomlWriter.WriteStartDocument();

        if (options.RootValueHandling == TomlRootValueHandling.WrapInRootKey)
        {
            tomlWriter.WriteStartTable();
            tomlWriter.WritePropertyNameLiteral(options.RootValueKeyName);
            typeInfo.Write(tomlWriter, value);
            tomlWriter.WriteEndTable();
        }
        else
        {
            typeInfo.Write(tomlWriter, value);
        }

        tomlWriter.WriteEndDocument();
    }

    [RequiresUnreferencedCode(ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(ReflectionBasedSerializationMessage)]
    private static TomlTypeInfo ResolveTypeInfo(TomlSerializerOptions options, Type type)
    {
        return TomlTypeInfoResolverPipeline.Resolve(options, type);
    }

    private static TomlTypeInfo ResolveTypeInfo(TomlSerializerContext context, Type type)
    {
        var typeInfo = context.GetTypeInfo(type, context.Options);
        if (typeInfo is null)
        {
            throw new TomlException($"No generated metadata is available for type '{type.FullName}' in the provided context.");
        }

        return typeInfo;
    }
}
