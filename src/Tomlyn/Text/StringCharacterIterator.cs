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
        public bool TryGetNext(ref int position, out char32 element)
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
                            element = char.ConvertToUtf32(c1, c2);
                            return true;
                        }

                        // Invalid pair; keep consuming and return U+FFFD.
                        element = 0xFFFD;
                        return true;
                    }

                    // Unexpected EOF after a high surrogate.
                    position = text.Length;
                    element = 0xFFFD;
                    return true;
                }

                // Lone low surrogate is invalid.
                if (((uint)c1 & 0xFC00u) == 0xDC00u)
                {
                    element = 0xFFFD;
                    return true;
                }

                element = c1;
                return true;
            }

            position = text.Length;
            element = default;
            return false;
        }
    }
}
