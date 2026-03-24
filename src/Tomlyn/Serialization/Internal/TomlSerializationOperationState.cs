// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tomlyn.Helpers;

namespace Tomlyn.Serialization.Internal;

internal sealed class TomlSerializationOperationState
{
    private readonly Dictionary<Type, TomlTypeInfo> _typeInfoCache = new();

    public TomlSerializationOperationState(TomlSerializerOptions options)
    {
        ArgumentGuard.ThrowIfNull(options, nameof(options));

        Options = options;
        SingleOrArrayCollections = new TomlSingleOrArrayCollectionHelper();
    }

    public TomlSerializerOptions Options { get; }

    public TomlSingleOrArrayCollectionHelper SingleOrArrayCollections { get; }

    [RequiresUnreferencedCode(TomlTypeInfoResolverPipeline.ReflectionBasedSerializationMessage)]
    [RequiresDynamicCode(TomlTypeInfoResolverPipeline.ReflectionBasedSerializationMessage)]
    public TomlTypeInfo ResolveTypeInfo(Type type)
    {
        ArgumentGuard.ThrowIfNull(type, nameof(type));

        return TomlTypeInfoResolverPipeline.Resolve(this, type);
    }

    public bool TryGetCachedTypeInfo(Type type, [NotNullWhen(true)] out TomlTypeInfo? typeInfo)
    {
        ArgumentGuard.ThrowIfNull(type, nameof(type));
        return _typeInfoCache.TryGetValue(type, out typeInfo);
    }

    public void CacheTypeInfo(Type type, TomlTypeInfo typeInfo)
    {
        ArgumentGuard.ThrowIfNull(type, nameof(type));
        ArgumentGuard.ThrowIfNull(typeInfo, nameof(typeInfo));
        _typeInfoCache[type] = typeInfo;
    }
}
