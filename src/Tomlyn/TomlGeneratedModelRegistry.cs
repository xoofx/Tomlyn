using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace Tomlyn;

/// <summary>
/// Generated model adapter interface used by the source generator.
/// </summary>
public interface ITomlGeneratedModel
{
    /// <summary>
    /// Gets the runtime type handled by this generated mapper.
    /// </summary>
    Type TargetType { get; }

    /// <summary>
    /// Tries to read a <see cref="TomlTable"/> into a model instance.
    /// </summary>
    /// <param name="table">The source TOML table.</param>
    /// <param name="model">The mapped model instance when successful.</param>
    /// <param name="diagnostics">Diagnostics produced during the mapping.</param>
    /// <param name="options">Optional mapping options.</param>
    /// <returns><c>true</c> when the table maps successfully; otherwise <c>false</c>.</returns>
    bool TryRead(TomlTable table, [NotNullWhen(true)] out object? model, out DiagnosticsBag diagnostics, TomlModelOptions? options = null);
}

/// <summary>
/// Generic generated model adapter interface used by the source generator.
/// </summary>
public interface ITomlGeneratedModel<T> : ITomlGeneratedModel where T : class, new()
{
    /// <summary>
    /// Tries to read a <see cref="TomlTable"/> into a typed model instance.
    /// </summary>
    /// <param name="table">The source TOML table.</param>
    /// <param name="model">The mapped model instance when successful.</param>
    /// <param name="diagnostics">Diagnostics produced during the mapping.</param>
    /// <param name="options">Optional mapping options.</param>
    /// <returns><c>true</c> when the table maps successfully; otherwise <c>false</c>.</returns>
    bool TryRead(TomlTable table, [NotNullWhen(true)] out T? model, out DiagnosticsBag diagnostics, TomlModelOptions? options = null);
}

/// <summary>
/// Registry for generated TOML mappers.
/// </summary>
public static class TomlGeneratedModelRegistry
{
    private static readonly ConcurrentDictionary<Type, ITomlGeneratedModel> Models = new();

    /// <summary>
    /// Registers a generated model mapper for later lookup.
    /// </summary>
    /// <param name="model">The generated model mapper to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is <c>null</c>.</exception>
    public static void Register(ITomlGeneratedModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        Models[model.TargetType] = model;
    }

    internal static bool TryGet(Type type, [NotNullWhen(true)] out ITomlGeneratedModel? model)
    {
        return Models.TryGetValue(type, out model);
    }
}
