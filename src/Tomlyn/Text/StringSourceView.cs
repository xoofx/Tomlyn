// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;

namespace Tomlyn.Text
{
    internal struct StringSourceView : ISourceView<StringCharacterIterator>
    {
        private readonly string _text;

        public StringSourceView(string text, string sourcePath)
        {
            this._text = text;
            SourcePath = sourcePath;
        }

        public string SourcePath { get; }

        public int Length => _text.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetSpan(int offset, int length)
        {
            var text = _text;
            return offset + length <= text.Length ? text.AsSpan(offset, length) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? GetString(int offset, int length)
        {
            var text = _text;
            return offset + length <= text.Length ? text.Substring(offset, length) : null;
        }

        public StringCharacterIterator GetIterator()
        {
            return new StringCharacterIterator(_text);   
        }

    }
}
