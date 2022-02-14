// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Text;

namespace Tomlyn.Helpers;

/// <summary>
/// Naming helpers used by Tomlyn.
/// </summary>
public class TomlNamingHelper
{
    [ThreadStatic]
    private static StringBuilder? _builder;

    /// <summary>
    /// Converts a string from pascal case (e.g `ThisIsFine`) to snake case (e.g `this_is_fine`).
    /// </summary>
    /// <param name="name">A PascalCase string to convert to snake case.</param>
    /// <returns>The snake case version of the input string.</returns>
    public static string PascalToSnakeCase(string name)
    {
        var builder = Builder;
        try
        {
            var pc = (char)0;
            foreach (var c in name)
            {
                if (char.IsUpper(c) && !char.IsUpper(pc) && pc != 0 && pc != '_')
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
                pc = c;
            }

            return builder.ToString();
        }
        finally
        {
            builder.Length = 0;
        }
    }

    private static StringBuilder Builder => _builder ??= new StringBuilder();
}