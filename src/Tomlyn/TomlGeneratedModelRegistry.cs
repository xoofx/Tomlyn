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
    Type TargetType { get; }

    bool TryRead(TomlTable table, [NotNullWhen(true)] out object? model, out DiagnosticsBag diagnostics, TomlModelOptions? options = null);
}

/// <summary>
/// Generic generated model adapter interface used by the source generator.
/// </summary>
public interface ITomlGeneratedModel<T> : ITomlGeneratedModel where T : class, new()
{
    bool TryRead(TomlTable table, [NotNullWhen(true)] out T? model, out DiagnosticsBag diagnostics, TomlModelOptions? options = null);
}

/// <summary>
/// Registry for generated TOML mappers.
/// </summary>
public static class TomlGeneratedModelRegistry
{
    private static readonly ConcurrentDictionary<Type, ITomlGeneratedModel> Models = new();

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
