// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tomlyn.Model.Accessors;
using Tomlyn.Syntax;

namespace Tomlyn.Model;

internal class DynamicModelWriteContext : DynamicModelReadContext
{
    public DynamicModelWriteContext(TomlModelOptions options, TextWriter writer) : base(options)
    {
        Writer = writer;
        ConvertToToml = options.ConvertToToml;
    }

    public TextWriter Writer { get; }

    public Func<object, object?>? ConvertToToml { get; set; }
}