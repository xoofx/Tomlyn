// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace Tomlyn.Text
{
    internal interface ISourceView : IStringView
    {
        string SourcePath { get; }
    }

    internal interface ISourceView<out TCharIterator> : ISourceView, IStringView<TCharIterator> where TCharIterator : struct, CharacterIterator
    {
    }
}