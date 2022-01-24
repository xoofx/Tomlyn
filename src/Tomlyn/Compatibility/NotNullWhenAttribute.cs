// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class NotNullWhenAttribute : Attribute
    {
        // ReSharper disable once UnusedParameter.Local
        [ExcludeFromCodeCoverage]
        public NotNullWhenAttribute(bool returnValue)
        {
        }
    }
}
#endif