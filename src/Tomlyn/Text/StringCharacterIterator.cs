// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System.Runtime.CompilerServices;

namespace Tomlyn.Text
{
    internal struct StringCharacterIterator : CharacterIterator
    {
        private readonly string _text;

        public StringCharacterIterator(string text)
        {
            this._text = text;
        }

        public int Start => 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char32? TryGetNext(ref int position)
        {
            var text = _text;
            if (position < text.Length)
            {
                var c1 = text[position++];

                // Fast surrogate checks (avoid char.IsHighSurrogate/IsLowSurrogate)
                if (((uint)c1 & 0xFC00u) == 0xD800u)
                {
                    // High surrogate; requires a following low surrogate.
                    if (position < text.Length)
                    {
                        var c2 = text[position++];
                        if (((uint)c2 & 0xFC00u) == 0xDC00u)
                        {
                            return char.ConvertToUtf32(c1, c2);
                        }

                        // Invalid pair; keep consuming and return U+FFFD.
                        return 0xFFFD;
                    }

                    // Unexpected EOF after a high surrogate.
                    position = text.Length;
                    return 0xFFFD;
                }

                // Lone low surrogate is invalid.
                if (((uint)c1 & 0xFC00u) == 0xDC00u)
                {
                    return 0xFFFD;
                }

                return c1;
            }

            position = text.Length;
            return null;
        }
    }
}
