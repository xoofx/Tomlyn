using System;
using System.Text;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Parsing;

internal static class TomlStringDecoder
{
    public static string Decode(string rawTokenText, TokenKind tokenKind)
    {
        if (rawTokenText is null)
        {
            throw new ArgumentNullException(nameof(rawTokenText));
        }

        return tokenKind switch
        {
            TokenKind.String => DecodeBasic(rawTokenText, isMultiline: false),
            TokenKind.StringMulti => DecodeBasic(rawTokenText, isMultiline: true),
            TokenKind.StringLiteral => DecodeLiteral(rawTokenText, isMultiline: false),
            TokenKind.StringLiteralMulti => DecodeLiteral(rawTokenText, isMultiline: true),
            _ => throw new ArgumentOutOfRangeException(nameof(tokenKind), tokenKind, "Unsupported string token kind."),
        };
    }

    private static string DecodeLiteral(string rawTokenText, bool isMultiline)
    {
        var span = rawTokenText.AsSpan();
        var delimiterLength = isMultiline ? 3 : 1;
        if (span.Length < delimiterLength * 2)
        {
            return string.Empty;
        }

        var content = span.Slice(delimiterLength, span.Length - (delimiterLength * 2));
        if (isMultiline)
        {
            content = SkipImmediateNewLine(content);
        }

        return content.ToString();
    }

    private static string DecodeBasic(string rawTokenText, bool isMultiline)
    {
        var span = rawTokenText.AsSpan();
        var delimiterLength = isMultiline ? 3 : 1;
        if (span.Length < delimiterLength * 2)
        {
            return string.Empty;
        }

        var content = span.Slice(delimiterLength, span.Length - (delimiterLength * 2));
        if (isMultiline)
        {
            content = SkipImmediateNewLine(content);
        }

        var builder = new StringBuilder(content.Length);
        for (var index = 0; index < content.Length; index++)
        {
            var c = content[index];
            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            if (index + 1 >= content.Length)
            {
                throw new FormatException("Invalid escape sequence at end of string.");
            }

            var escape = content[++index];
            switch (escape)
            {
                case 'b':
                    builder.Append('\b');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case '"':
                    builder.Append('"');
                    break;
                case '\\':
                    builder.Append('\\');
                    break;
                case 'e':
                    builder.Append('\u001B');
                    break;
                case 'x':
                    builder.Append((char)ReadHexScalar(content, ref index, digits: 2));
                    break;
                case 'u':
                    AppendUnicodeScalar(builder, ReadHexScalar(content, ref index, digits: 4));
                    break;
                case 'U':
                    AppendUnicodeScalar(builder, ReadHexScalar(content, ref index, digits: 8));
                    break;
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    SkipEscapedWhitespaceAndNewlines(content, ref index);
                    break;
                default:
                    throw new FormatException($"Unexpected escape character `{escape}` in string.");
            }
        }

        return builder.ToString();
    }

    private static ReadOnlySpan<char> SkipImmediateNewLine(ReadOnlySpan<char> content)
    {
        if (content.Length == 0)
        {
            return content;
        }

        if (content[0] == '\r')
        {
            if (content.Length > 1 && content[1] == '\n')
            {
                return content.Slice(2);
            }

            return content.Slice(1);
        }

        return content[0] == '\n' ? content.Slice(1) : content;
    }

    private static int ReadHexScalar(ReadOnlySpan<char> content, ref int index, int digits)
    {
        if (index + digits >= content.Length)
        {
            throw new FormatException("Invalid hexadecimal escape sequence.");
        }

        var value = 0;
        for (var i = 0; i < digits; i++)
        {
            var c = content[index + 1 + i];
            if (!CharHelper.IsHexFunc(c))
            {
                throw new FormatException("Invalid hexadecimal escape sequence.");
            }

            value = (value << 4) + CharHelper.HexToDecimal(c);
        }

        index += digits;
        return value;
    }

    private static void AppendUnicodeScalar(StringBuilder builder, int value)
    {
        if (!CharHelper.IsValidUnicodeScalarValue(value))
        {
            throw new FormatException($"Invalid Unicode scalar value [{value:X}].");
        }

        builder.AppendUtf32((char32)value);
    }

    private static void SkipEscapedWhitespaceAndNewlines(ReadOnlySpan<char> content, ref int index)
    {
        var position = index;
        var skippedNewLine = false;
        while (position < content.Length)
        {
            var c = content[position];
            if (c == ' ' || c == '\t')
            {
                position++;
                continue;
            }

            if (c == '\n')
            {
                skippedNewLine = true;
                position++;
                continue;
            }

            if (c == '\r')
            {
                skippedNewLine = true;
                position++;
                if (position < content.Length && content[position] == '\n')
                {
                    position++;
                }

                continue;
            }

            break;
        }

        if (!skippedNewLine)
        {
            throw new FormatException("Invalid escape `\\`. It must skip at least one line.");
        }

        index = Math.Max(index, position - 1);
    }
}
