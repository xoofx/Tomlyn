// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;

namespace SharpToml.Model
{
    public sealed class TomlDateTime : TomlValue<DateTime>
    {
        public TomlDateTime(DateTime value) : base(ObjectKind.DateTime, value)
        {
        }
    }
}