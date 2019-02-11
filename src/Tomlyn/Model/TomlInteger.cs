// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace Tomlyn.Model
{
    public sealed class TomlInteger : TomlValue<long>
    {
        public TomlInteger(long value) : base(ObjectKind.Integer, value)
        {
        }
    }
}