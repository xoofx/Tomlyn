// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Tomlyn.Model;

/// <summary>
/// Allow to attach metadata to properties
/// </summary>
public interface ITomlMetadataProvider
{
    /// <summary>
    /// Gets or sets the attached metadata for properties
    /// </summary>
    TomlPropertiesMetadata? PropertiesMetadata { get; set; }
}