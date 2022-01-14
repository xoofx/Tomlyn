// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Tomlyn;

/// <summary>
/// Offsets used for a <see cref="DateTimeValue"/>
/// </summary>
public enum DateTimeValueOffsetKind
{
    /// <summary>
    /// No offset.
    /// </summary>
    None,
    /// <summary>
    /// Zero offset.
    /// </summary>
    Zero,
    /// <summary>
    /// Number offset.
    /// </summary>
    Number
}