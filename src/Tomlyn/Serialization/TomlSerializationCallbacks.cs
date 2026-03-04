// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Tomlyn.Serialization;

/// <summary>
/// Defines a callback that is invoked before an instance is serialized to TOML.
/// </summary>
public interface ITomlOnSerializing
{
    /// <summary>
    /// Called before the instance is serialized.
    /// </summary>
    void OnTomlSerializing();
}

/// <summary>
/// Defines a callback that is invoked after an instance has been serialized to TOML.
/// </summary>
public interface ITomlOnSerialized
{
    /// <summary>
    /// Called after the instance has been serialized.
    /// </summary>
    void OnTomlSerialized();
}

/// <summary>
/// Defines a callback that is invoked after an instance is created but before it is populated during deserialization.
/// </summary>
public interface ITomlOnDeserializing
{
    /// <summary>
    /// Called before the instance is populated from TOML.
    /// </summary>
    void OnTomlDeserializing();
}

/// <summary>
/// Defines a callback that is invoked after an instance has been populated during deserialization.
/// </summary>
public interface ITomlOnDeserialized
{
    /// <summary>
    /// Called after the instance has been populated from TOML.
    /// </summary>
    void OnTomlDeserialized();
}
