// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Tomlyn.Text
{
    internal struct StringCharacterUtf8Iterator : CharacterIterator
    {
        private readonly byte[] _text;

        public StringCharacterUtf8Iterator(byte[] text)
        {
            this._text = text;
        }

        public int Start => 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char32? TryGetNext(ref int position)
        {
            return CharHelper.ToUtf8(_text, ref position);
        }
    }
}