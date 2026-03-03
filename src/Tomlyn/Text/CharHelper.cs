// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tomlyn.Text
{
    internal static partial class CharHelper
    {
        public static readonly Func<char32, bool> IsHexFunc = IsHex;
        public static readonly Func<char32, bool> IsOctalFunc = IsOctal;
        public static readonly Func<char32, bool> IsBinaryFunc = IsBinary;

        public static readonly Func<char32, int> HexToDecFunc = HexToDecimal;
        public static readonly Func<char32, int> OctalToDecFunc = OctalToDecimal;
        public static readonly Func<char32, int> BinaryToDecFunc = BinaryToDecimal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsControlCharacter(char32 c)
        {
            return c <= 0x1F || c == 0x7F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyStart(char32 c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '-' || c >= '0' && c <= '9';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyContinue(char32 c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '-' || c >= '0' && c <= '9';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIdentifierStart(char32 c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIdentifierContinue(char32 c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidUnicodeScalarValue(char32 c)
        {
            return c >= 0 && c <= 0xD7FF || c >= 0xE000 && c <= 0x10FFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(char32 c)
        {
            return (c >= '0' && c <= '9');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDateTime(char32 c)
        {
            return IsDigit(c) || c == ':' || c == '-' || c == 'Z' || c == 'T' || c == 'z' || c == 't' || c == '+' || c == '.';
        }

        /// <summary>
        /// Escape a C# string to a TOML string
        /// </summary>
        public static string EscapeForToml(this string text, bool allowNewLinesAndSpace = false)
        {
            StringBuilder? builder = null;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];

                string? str = null;

                // Handle special characters for when multiline/spaces are allowed
                if (allowNewLinesAndSpace)
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        str = "\r\n";
                        i++;
                        c = '\n';
                    }
                    else if (c == '\n')
                    {
                        str = "\n";
                    }
                    else if (c == '\t')
                    {
                        str = "\t";
                    }
                }

                str ??= EscapeChar(text[i]);

                if (str is not null)
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(text.Length * 2);
                        builder.Append(text.Substring(0, i));
                    }
                    builder.Append(str);
                }
                else
                {
                    builder?.Append(c);
                }
            }
            return builder?.ToString() ?? text;
        }

        private static string? EscapeChar(char c)
        {
            if (c < ' ' || c == '"' || c == '\\' || char.IsControl(c))
            {
                switch (c)
                {
                    case '\b':
                        return @"\b";
                    case '\t':
                        return @"\t";
                    case '\n':
                        return @"\n";
                    case '\f':
                        return @"\f";
                    case '\r':
                        return @"\r";
                    case '"':
                        return @"\""";
                    case '\\':
                        return @"\\";
                    default:
                        return $"\\u{(ushort)c:X4}";
                }
            }
            return null;
        }

        /// <summary>
        /// Converts a string that may have control characters to a printable string
        /// </summary>
        public static string? ToPrintableString(this string? text)
        {
            if (text is null) return null;
            StringBuilder? builder = null;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var str = text[i].ToPrintableString();
                if (str is not null)
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(text.Length * 2);
                        builder.Append(text.Substring(0, i));
                    }
                    builder.Append(str);
                }
                else
                {
                    builder?.Append(c);
                }
            }
            return builder?.ToString() ?? text;
        }

        public static string? ToPrintableString(this char c)
        {
            if (c < ' ' || IsWhiteSpace(c))
            {
                switch (c)
                {
                    case ' ':
                        return @" ";
                    case '\b':
                        return @"\b";
                    case '\r':
                        return @"␍";
                    case '\n':
                        return @"␤";
                    case '\t':
                        return @"\t";
                    case '\a':
                        return @"\a";
                    case '\v':
                        return @"\v";
                    case '\f':
                        return @"\f";
                    default:
                        return $"\\u{(int)c:X};";
                }
            }
            return null;
        }

        public static bool IsWhiteSpace(char32 c)
        {
            return c == ' ' || // space
                   c == '\t'; // horizontal tab
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpaceOrNewLine(char32 c)
        {
            return c == ' ' || // space
                   c == '\t' || // horizontal tab
                   c == '\r' || // \r
                   c == '\n'; // \n
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNewLine(char32 c)
        {
            return c == '\r' || // \r
                   c == '\n'; // \n
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToUtf8(byte[] buffer, ref int position, out char32 element)
        {
            if ((uint)position >= (uint)buffer.Length)
            {
                position = buffer.Length;
                element = default;
                return false;
            }

            // bytes   bits    UTF-8 representation
            // -----   ----    -----------------------------------
            // 1        7      0vvvvvvv
            // 2       11      110vvvvv 10vvvvvv
            // 3       16      1110vvvv 10vvvvvv 10vvvvvv
            // 4       21      11110vvv 10vvvvvv 10vvvvvv 10vvvvvv
            // -----   ----    -----------------------------------

            var startOffset = position;
            var b1 = buffer[position++];
            if (b1 < 0x80)
            {
                element = b1;
                return true;
            }

            // Reject invalid leading bytes:
            // - 0x80..0xBF are continuation bytes
            // - 0xC0..0xC1 would introduce overlong sequences
            // - 0xF5..0xFF are outside Unicode scalar range
            if (b1 < 0xC2 || b1 > 0xF4)
            {
                throw new CharReaderException($"Invalid UTF-8 start byte 0x{b1:X2} at byte offset {startOffset}.");
            }

            if (b1 < 0xE0)
            {
                if (position >= buffer.Length)
                {
                    throw new CharReaderException($"Unexpected end of data in UTF-8 sequence at byte offset {startOffset}.");
                }

                var b2 = buffer[position++];
                if ((b2 & 0xC0) != 0x80)
                {
                    throw new CharReaderException($"Invalid UTF-8 continuation byte 0x{b2:X2} at byte offset {position - 1}.");
                }

                element = ((b1 & 0x1F) << 6) | (b2 & 0x3F);
                return true;
            }

            if (b1 < 0xF0)
            {
                if (position + 1 >= buffer.Length)
                {
                    throw new CharReaderException($"Unexpected end of data in UTF-8 sequence at byte offset {startOffset}.");
                }

                var b2 = buffer[position++];
                var b3 = buffer[position++];
                if ((b2 & 0xC0) != 0x80)
                {
                    throw new CharReaderException($"Invalid UTF-8 continuation byte 0x{b2:X2} at byte offset {position - 2}.");
                }
                if ((b3 & 0xC0) != 0x80)
                {
                    throw new CharReaderException($"Invalid UTF-8 continuation byte 0x{b3:X2} at byte offset {position - 1}.");
                }

                // Overlong sequences (U+0000..U+07FF) encoded in 3 bytes are invalid.
                if (b1 == 0xE0 && b2 < 0xA0)
                {
                    throw new CharReaderException($"Overlong UTF-8 sequence at byte offset {startOffset}.");
                }

                // UTF-16 surrogate halves are not valid Unicode scalar values.
                if (b1 == 0xED && b2 >= 0xA0)
                {
                    throw new CharReaderException($"Invalid UTF-8 surrogate code point at byte offset {startOffset}.");
                }

                element = ((b1 & 0x0F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F);
                return true;
            }

            // 4-byte sequence (b1 is 0xF0..0xF4)
            if (position + 2 >= buffer.Length)
            {
                throw new CharReaderException($"Unexpected end of data in UTF-8 sequence at byte offset {startOffset}.");
            }

            var b2_4 = buffer[position++];
            var b3_4 = buffer[position++];
            var b4_4 = buffer[position++];
            if ((b2_4 & 0xC0) != 0x80)
            {
                throw new CharReaderException($"Invalid UTF-8 continuation byte 0x{b2_4:X2} at byte offset {position - 3}.");
            }
            if ((b3_4 & 0xC0) != 0x80)
            {
                throw new CharReaderException($"Invalid UTF-8 continuation byte 0x{b3_4:X2} at byte offset {position - 2}.");
            }
            if ((b4_4 & 0xC0) != 0x80)
            {
                throw new CharReaderException($"Invalid UTF-8 continuation byte 0x{b4_4:X2} at byte offset {position - 1}.");
            }

            // Overlong sequences (U+0000..U+FFFF) encoded in 4 bytes are invalid.
            if (b1 == 0xF0 && b2_4 < 0x90)
            {
                throw new CharReaderException($"Overlong UTF-8 sequence at byte offset {startOffset}.");
            }

            // Outside max scalar value U+10FFFF.
            if (b1 == 0xF4 && b2_4 > 0x8F)
            {
                throw new CharReaderException($"Invalid UTF-8 code point above U+10FFFF at byte offset {startOffset}.");
            }

            var codePoint = ((b1 & 0x07) << 18) | ((b2_4 & 0x3F) << 12) | ((b3_4 & 0x3F) << 6) | (b4_4 & 0x3F);
            if (codePoint > 0x10FFFF)
            {
                throw new CharReaderException($"Invalid UTF-8 code point above U+10FFFF at byte offset {startOffset}.");
            }

            element = codePoint;
            return true;
        }

        public static int HexToDecimal(char32 c)
        {
            Debug.Assert(IsHex(c));
            return (c >= '0' && c <= '9') ? c - '0' : (c >= 'a' && c <= 'f') ? (c - 'a') + 10 : (c - 'A') + 10;
        }

        public static int OctalToDecimal(char32 c)
        {
            Debug.Assert(IsOctal(c));
            return c - '0';
        }

        public static int BinaryToDecimal(char32 c)
        {
            Debug.Assert(IsBinary(c));
            return c - '0';
        }

        private static bool IsHex(char32 c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
        private static bool IsOctal(char32 c)
        {
            return (c >= '0' && c <= '7');
        }
        private static bool IsBinary(char32 c)
        {
            return (c == '0' || c == '1');
        }

        public static void AppendUtf32(this StringBuilder builder, char32 utf32)
        {
            if (utf32 < 65536)
            {
                builder.Append((char) utf32);
                return;
            }
            utf32 -= 65536;
            builder.Append((char) (utf32 / 1024 + 55296));
            builder.Append((char) (utf32 % 1024 + 56320));
        }
    }
}
