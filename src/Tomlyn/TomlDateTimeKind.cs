// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Tomlyn;

/// <summary>
/// Offsets used for a <see cref="TomlDateTime"/>
/// </summary>
public enum TomlDateTimeKind
{
    OffsetDateTimeByZ,
    OffsetDateTimeByNumber,
    LocalDateTime,
    LocalDate,
    LocalTime,
}