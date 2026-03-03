// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System.Runtime.CompilerServices;

namespace Tomlyn.Text
{
    internal struct StringCharacterUtf8Iterator : CharacterIterator
    {
        private readonly byte[] _text;
        private readonly int _start;

        public StringCharacterUtf8Iterator(byte[] text)
        {
            this._text = text;
            // Check if we have a BOM, if we have it, move right after
            // 0xEF,0xBB,0xBF
            _start = (text.Length >= 3 && text[0] == (byte)0xEF && text[1] == (byte)0xBB && text[2] == (byte)0xBF) ? 3 : 0;
        }

        public int Start => _start;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNext(ref int position, out char32 element)
        {
            var text = _text;
            return CharHelper.ToUtf8(text, ref position, out element);
        }
    }
}
