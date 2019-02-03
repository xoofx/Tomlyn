// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public static class TokenKindExtensions
    {
        public static string ToText(this TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.Comma:
                    return ",";
                case TokenKind.Dot:
                    return ".";
                case TokenKind.Equal:
                    return "=";
                case TokenKind.OpenBracket:
                    return "[";
                case TokenKind.OpenBracketDouble:
                    return "[[";
                case TokenKind.CloseBracket:
                    return "]";
                case TokenKind.CloseBracketDouble:
                    return "]]";
                case TokenKind.OpenBrace:
                    return "{";
                case TokenKind.CloseBrace:
                    return "}";
                case TokenKind.True:
                    return "true";
                case TokenKind.False:
                    return "false";
                case TokenKind.Infinite:
                    return "inf";
                case TokenKind.PositiveInfinite:
                    return "+inf";
                case TokenKind.NegativeInfinite:
                    return "-inf";
                case TokenKind.Nan:
                    return "nan";
                case TokenKind.PositiveNan:
                    return "+nan";
                case TokenKind.NegativeNan:
                    return "-nan";
            }

            return null;
        }

        public static bool IsFloat(this TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.Float:
                case TokenKind.Infinite:
                case TokenKind.PositiveInfinite:
                case TokenKind.NegativeInfinite:
                case TokenKind.Nan:
                case TokenKind.PositiveNan:
                case TokenKind.NegativeNan:
                    return true;
            }

            return false;
        }

        public static bool IsInteger(this TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.Integer:
                case TokenKind.IntegerBinary:
                case TokenKind.IntegerHexa:
                case TokenKind.IntegerOctal:
                    return true;
            }

            return false;
        }

        public static bool IsString(this TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.String:
                case TokenKind.StringLiteral:
                case TokenKind.StringMulti:
                case TokenKind.StringLiteralMulti:
                    return true;
            }

            return false;
        }

    }
}