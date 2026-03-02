// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Tomlyn.Helpers;
using Tomlyn.Model;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Parsing
{
    /// <summary>
    /// Lexer enumerator that generates <see cref="SyntaxTokenValue"/>, to be used from a foreach.
    /// </summary>
    internal class Lexer<TSourceView, TCharReader> : ITokenProvider<TSourceView> where TSourceView : struct, ISourceView<TCharReader> where TCharReader : struct, CharacterIterator
    {
        private SyntaxTokenValue _token;
        private List<DiagnosticMessage>? _errors;
        private TCharReader _reader;
        private const int Eof = -1;
        private TSourceView _sourceView;
        private readonly StringBuilder _textBuilder;
        private readonly List<char32> _currentIdentifierChars;
        private LexerInternalState? _preview1 = null;
        private LexerInternalState _current;

        /// <summary>
        /// Initialize a new instance of this <see cref="Lexer{TSourceView,TCharReader}" />.
        /// </summary>
        /// <param name="sourceView">The text to analyze</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentNullException">If text is null</exception>
        public Lexer(TSourceView sourceView)
        {
            _sourceView = sourceView;
            _reader = sourceView.GetIterator();
            _currentIdentifierChars = new List<char32>();
            _textBuilder = new StringBuilder();
            Reset();
        }

        public TSourceView Source => _sourceView;

        /// <summary>
        /// Gets a boolean indicating whether this lexer has errors.
        /// </summary>
        public bool HasErrors => _errors != null && _errors.Count > 0;

        /// <summary>
        /// Gets error messages.
        /// </summary>
        public IEnumerable<DiagnosticMessage> Errors => _errors ?? Enumerable.Empty<DiagnosticMessage>();

        /// <summary>
        /// Gets or sets a value indicating whether the lexer should decode scalar values (for example, string escape sequences).
        /// </summary>
        public bool DecodeScalars { get; set; } = true;

        public bool MoveNext()
        {
            // If we have errors or we are already at the end of the file, we don't continue
            if (_token.Kind == TokenKind.Eof)
            {
                return false;
            }

            if (State == LexerState.Key)
            {
                NextTokenForKey();
            }
            else
            {
                NextTokenForValue();
            }
            return true;
        }

        public SyntaxTokenValue Token => _token;

        public LexerState State { get; set; }

        private TextPosition _position => _current.Position;

        private char32 _c => _current.CurrentChar;

        private void NextTokenForKey()
        {
            var start = _position;
            switch (_c)
            {
                case '\n':
                    _token = new SyntaxTokenValue(TokenKind.NewLine, start, start);
                    NextChar();
                    break;
                case '\r':
                    NextChar();
                    // case of: \r\n
                    if (_c == '\n')
                    {
                        _token = new SyntaxTokenValue(TokenKind.NewLine, start, _position);
                        NextChar();
                        break;
                    }
                    AddError($"Invalid \\r not followed by \\n", start, start);
                    // case of \r
                    _token = new SyntaxTokenValue(TokenKind.NewLine, start, start);
                    break;
                case '#':
                    NextChar();
                    ReadComment(start);
                    break;
                case '.':
                    NextChar();
                    _token = new SyntaxTokenValue(TokenKind.Dot, start, start);
                    break;
                case '=': // in the context of a key, we need to parse up to the =
                    NextChar();
                    _token = new SyntaxTokenValue(TokenKind.Equal, start, start);
                    break;
                case ',':
                    _token = new SyntaxTokenValue(TokenKind.Comma, start, start);
                    NextChar();
                    break;
                case '{':
                    _token = new SyntaxTokenValue(TokenKind.OpenBrace, _position, _position);
                    NextChar();
                    break;
                case '}':
                    _token = new SyntaxTokenValue(TokenKind.CloseBrace, _position, _position);
                    NextChar();
                    break;
                case '[':
                    NextChar();
                    // case of: ]]
                    if (_c == '[')
                    {
                        _token = new SyntaxTokenValue(TokenKind.OpenBracketDouble, start, _position);
                        NextChar();
                        break;
                    }
                    _token = new SyntaxTokenValue(TokenKind.OpenBracket, start, start);
                    break;
                case ']':
                    NextChar();
                    // case of: ]]
                    if (_c == ']')
                    {
                        _token = new SyntaxTokenValue(TokenKind.CloseBracketDouble, start, _position);
                        NextChar();
                        break;
                    }
                    _token = new SyntaxTokenValue(TokenKind.CloseBracket, start, start);
                    break;
                case '"':
                    ReadString(start, false);
                    break;
                case '\'':
                    ReadStringLiteral(start, false);
                    break;
                case Eof:
                    _token = new SyntaxTokenValue(TokenKind.Eof, _position, _position);
                    break;
                default:
                    // Eat any whitespace
                    if (ConsumeWhitespace())
                    {
                        break;
                    }

                    if (CharHelper.IsKeyStart(_c))
                    {
                        ReadKey();
                        break;
                    }

                    // invalid char
                    _token = new SyntaxTokenValue(TokenKind.Invalid, _position, _position);
                    NextChar();
                    break;
            }
        }

        private void NextTokenForValue()
        {
            var start = _position;
            switch (_c)
            {
                case '\n':
                    _token = new SyntaxTokenValue(TokenKind.NewLine, start, _position);
                    NextChar();
                    break;
                case '\r':
                    NextChar();
                    // case of: \r\n
                    if (_c == '\n')
                    {
                        _token = new SyntaxTokenValue(TokenKind.NewLine, start, _position);
                        NextChar();
                        break;
                    }

                    AddError($"Invalid \\r not followed by \\n", start, start);
                    // case of \r
                    _token = new SyntaxTokenValue(TokenKind.NewLine, start, start);
                    break;
                case '#':
                    NextChar();
                    ReadComment(start);
                    break;
                case ',':
                    _token = new SyntaxTokenValue(TokenKind.Comma, start, start);
                    NextChar();
                    break;
                case '[':
                    NextChar();
                    _token = new SyntaxTokenValue(TokenKind.OpenBracket, start, start);
                    break;
                case ']':
                    NextChar();
                    _token = new SyntaxTokenValue(TokenKind.CloseBracket, start, start);
                    break;
                case '{':
                    _token = new SyntaxTokenValue(TokenKind.OpenBrace, _position, _position);
                    NextChar();
                    break;
                case '}':
                    _token = new SyntaxTokenValue(TokenKind.CloseBrace, _position, _position);
                    NextChar();
                    break;
                case '"':
                    ReadString(start, true);
                    break;
                case '\'':
                    ReadStringLiteral(start, true);
                    break;
                case Eof:
                    _token = new SyntaxTokenValue(TokenKind.Eof, _position, _position);
                    break;
                default:
                    // Eat any whitespace
                    if (ConsumeWhitespace())
                    {
                        break;
                    }

                    // Handle inf, +inf, -inf, true, false
                    if (_c == '+' || _c == '-' || CharHelper.IsIdentifierStart(_c))
                    {
                        ReadSpecialToken();
                        break;
                    }

                    if (CharHelper.IsDigit(_c))
                    {
                        ReadNumberOrDate();
                        break;
                    }

                    // invalid char
                    _token = new SyntaxTokenValue(TokenKind.Invalid, _position, _position);
                    NextChar();
                    break;
            }
        }

        private bool ConsumeWhitespace()
        {
            var start = _position;
            var end = _position;
            while (CharHelper.IsWhiteSpace(_c))
            {
                end = _position;
                NextChar();
            }

            if (start != _position)
            {
                _token = new SyntaxTokenValue(TokenKind.Whitespaces, start, end);
                return true;
            }

            return false;
        }

        private void ReadKey()
        {
            var start = _position;
            var end = _position;
            while (CharHelper.IsKeyContinue(_c))
            {
                end = _position;
                NextChar();
            }

            _token = new SyntaxTokenValue(TokenKind.BasicKey, start, end);
        }

        private void ReadSpecialToken()
        {
            var start = _position;
            var end = _position;
            _currentIdentifierChars.Clear();

            // We track an identifier to check if it is a keyword (inf, true, false)
            var firstChar = _c;
            _currentIdentifierChars.Add(_c);

            NextChar();

            // IF we have a digit, this is a -1 or +2
            if ((firstChar == '+' || firstChar == '-') && CharHelper.IsDigit(_c))
            {
                _currentIdentifierChars.Clear();
                ReadNumberOrDate(firstChar, start);
                return;
            }

            while (CharHelper.IsIdentifierContinue(_c))
            {
                // We track an identifier to check if it is a keyword (inf, true, false)
                _currentIdentifierChars.Add(_c);

                end = _position;
                NextChar();
            }

            if (MatchCurrentIdentifier("true"))
            {
                _token = new SyntaxTokenValue(TokenKind.True, start, end, stringValue: null, data: 1);
            }
            else if (MatchCurrentIdentifier("false"))
            {
                _token = new SyntaxTokenValue(TokenKind.False, start, end, stringValue: null, data: 0);
            }
            else if (MatchCurrentIdentifier("inf"))
            {
                _token = new SyntaxTokenValue(TokenKind.Infinite, start, end, stringValue: null, data: unchecked((ulong)BitConverter.DoubleToInt64Bits(double.PositiveInfinity)));
            }
            else if (MatchCurrentIdentifier("+inf"))
            {
                _token = new SyntaxTokenValue(TokenKind.PositiveInfinite, start, end, stringValue: null, data: unchecked((ulong)BitConverter.DoubleToInt64Bits(double.PositiveInfinity)));
            }
            else if (MatchCurrentIdentifier("-inf"))
            {
                _token = new SyntaxTokenValue(TokenKind.NegativeInfinite, start, end, stringValue: null, data: unchecked((ulong)BitConverter.DoubleToInt64Bits(double.NegativeInfinity)));
            }
            else if (MatchCurrentIdentifier("nan"))
            {
                _token = new SyntaxTokenValue(TokenKind.Nan, start, end, stringValue: null, data: unchecked((ulong)BitConverter.DoubleToInt64Bits(double.NaN)));
            }
            else if (MatchCurrentIdentifier("+nan"))
            {
                _token = new SyntaxTokenValue(TokenKind.PositiveNan, start, end, stringValue: null, data: unchecked((ulong)BitConverter.DoubleToInt64Bits(double.NaN)));
            }
            else if (MatchCurrentIdentifier("-nan"))
            {
                _token = new SyntaxTokenValue(TokenKind.NegativeNan, start, end, stringValue: null, data: unchecked((ulong)BitConverter.DoubleToInt64Bits(double.NaN)));
            }
            else
            {
                _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
            }
            _currentIdentifierChars.Clear();
        }

        private bool MatchCurrentIdentifier(string text)
        {
            // TODO: we expect strings to be ASCII only 
            if (_currentIdentifierChars.Count != text.Length) return false;
            for (int i = 0; i < text.Length; i++)
            {
                if (_currentIdentifierChars[i] != text[i]) return false;
            }
            return true;
        }

        private void ReadNumberOrDate(char32? signPrefix = null, TextPosition? signPrefixPos = null)
        {
            var start = signPrefixPos ?? _position;
            var end = _position;
            var isFloat = false;

            var positionFirstDigit = _position;

            //var firstChar = numberPrefix ?? _c;
            var hasLeadingSign = signPrefix != null;
            var hasLeadingZero = _c == '0';

            // Reset parsing of integer
            _textBuilder.Length = 0;
            if (hasLeadingSign) _textBuilder.AppendUtf32(signPrefix!.Value);

            // If we start with 0, it might be an hexa, octal or binary literal
            if (hasLeadingZero)
            {
                NextChar(); // Skip first digit character
                if (!hasLeadingSign && (_c == 'x' || _c == 'X' || _c == 'o' || _c == 'O' || _c == 'b' || _c == 'B'))
                {
                    string name;
                    Func<char32, bool> match;
                    Func<char32, int> convert;
                    string range;
                    string prefix;
                    int shift;
                    TokenKind tokenKind;
                    if (_c == 'x' || _c == 'X')
                    {
                        name = "hexadecimal";
                        range = "[0-9a-zA-Z]";
                        prefix = "0x";
                        match = CharHelper.IsHexFunc;
                        convert = CharHelper.HexToDecFunc;
                        shift = 4;
                        tokenKind = TokenKind.IntegerHexa;
                        if (_c == 'X')
                        {
                            AddError($"Invalid capital X for hexadecimal. Use `x` instead.", _position, _position);
                        }
                    }
                    else if (_c == 'o' || _c == 'O')
                    {
                        name = "octal";
                        range = "[0-7]";
                        prefix = "0o";
                        match = CharHelper.IsOctalFunc;
                        convert = CharHelper.OctalToDecFunc;
                        shift = 3;
                        tokenKind = TokenKind.IntegerOctal;
                        if (_c == 'O')
                        {
                            AddError($"Invalid capital O for octal. Use `o` instead.", _position, _position);
                        }
                    }
                    else
                    {
                        name = "binary";
                        range = "0 or 1";
                        prefix = "0b";
                        match = CharHelper.IsBinaryFunc;
                        convert = CharHelper.BinaryToDecFunc;
                        shift = 1;
                        tokenKind = TokenKind.IntegerBinary;
                        if (_c == 'B')
                        {
                            AddError($"Invalid capital B for binary. Use `b` instead.", _position, _position);
                        }
                    }

                    end = _position;
                    NextChar(); // skip x,X,o,O,b,B

                    int originalMaxShift = 64 / shift;
                    int maxShift = originalMaxShift;
                    bool hasCharInRange = false;
                    bool lastWasDigit = false;
                    ulong value = 0;
                    while (true)
                    {
                        bool hasLocalCharInRange = false;
                        if (_c == '_' || (hasLocalCharInRange = match(_c)))
                        {
                            var nextIsDigit = _c != '_';
                            if (!lastWasDigit && !nextIsDigit)
                            {
                                // toml-specs: each underscore must be surrounded by at least one digit on each side.
                                AddError($"An underscore must be surrounded by at least one {name} digit on each side", start, start);
                            }
                            else if (nextIsDigit)
                            {
                                value = (value << shift) + (ulong)convert(_c);
                                maxShift--;
                                // Log only once the error that the value is beyond
                                if (maxShift == -1)
                                {
                                    AddError($"Invalid size of {name} integer. Expecting less than or equal {originalMaxShift} {name} digits", start, start);
                                }
                            }

                            lastWasDigit = nextIsDigit;

                            if (hasLocalCharInRange)
                            {
                                hasCharInRange = true;
                            }
                            end = _position;
                            NextChar();
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!hasCharInRange)
                    {
                        AddError($"Invalid {name} integer. Expecting at least one {range} after {prefix}", start, start);
                        _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
                    }
                    else if (!lastWasDigit)
                    {
                        AddError($"Invalid {name} integer. Expecting a {range} after the last character", start, start);
                        _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
                    }
                    else
                    {
                        // toml-specs: 64 bit (signed long) range expected (−9,223,372,036,854,775,808 to 9,223,372,036,854,775,807).
                        var signedValue = unchecked((long)value);
                        _token = new SyntaxTokenValue(tokenKind, start, end, stringValue: null, data: unchecked((ulong)signedValue));
                    }
                    return;
                }
                else
                {
                    // Append the leading 0
                    _textBuilder.Append('0');
                }
            }

            // Parse leading digits
            var beforeFollowingZero = _position;
            bool hasMultipleLeadingZero = false;

            // Skip leading zeros
            var previousCharIsDigit = false;
            if (hasLeadingZero)
            {
                int zeroDigit = 0;
                previousCharIsDigit = true;
                while (_c == '0' || _c == '_')
                {
                    previousCharIsDigit = _c == '0';
                    if (previousCharIsDigit)
                    {
                        _textBuilder.Append((char)_c);
                        zeroDigit++;
                    }
                    end = _position;
                    NextChar();
                }

                hasMultipleLeadingZero = zeroDigit > 0;
            }

            ReadDigits(ref end, previousCharIsDigit);

            // We are in the case of a date
            if (_c == '-' || _c == ':')
            {
                // Offset Date-Time
                // odt1 = 1979-05-27T07:32:00Z
                // odt2 = 1979-05-27T00:32:00-07:00
                // odt3 = 1979-05-27T00:32:00.999999-07:00
                //
                // For the sake of readability, you may replace the T delimiter between date and time with a space (as permitted by RFC 3339 section 5.6).
                //  NOTE: ISO 8601 defines date and time separated by "T".
                //      Applications using this syntax may choose, for the sake of
                //      readability, to specify a full-date and full-time separated by
                //      (say) a space character.
                // odt4 = 1979-05-27 07:32:00Z
                //
                // Local Date-Time
                //
                // ldt1 = 1979-05-27T07:32:00
                //
                // Local Date
                //
                // ld1 = 1979-05-27
                //
                // Local Time
                //
                // lt1 = 07:32:00
                // lt2 = 00:32:00.999999

                // Parse the date/time
                while (CharHelper.IsDateTime(_c))
                {
                    _textBuilder.AppendUtf32(_c);
                    end = _position;
                    NextChar();
                }

                // If we have a space, followed by a digit, try to parse the following
                if (CharHelper.IsWhiteSpace(_c) && CharHelper.IsDateTime(PeekChar()))
                {
                    _textBuilder.AppendUtf32(_c); // Append the space
                    NextChar(); // skip the space
                    while (CharHelper.IsDateTime(_c))
                    {
                        _textBuilder.AppendUtf32(_c);
                        end = _position;
                        NextChar();
                    }
                }
                
                var dateTimeAsString = _textBuilder.ToString();

                if (hasLeadingSign)
                {
                    AddError($"Invalid prefix `{signPrefix!.Value}` for the following offset/local date/time `{dateTimeAsString}`", start, end);
                    // Still try to recover
                    dateTimeAsString = dateTimeAsString.Substring(1);
                }

                TomlDateTime datetime;
                if (DateTimeRFC3339.TryParseOffsetDateTime(dateTimeAsString, out datetime))
                {
                    var tokenKind = datetime.Kind == TomlDateTimeKind.OffsetDateTimeByZ
                        ? TokenKind.OffsetDateTimeByZ
                        : TokenKind.OffsetDateTimeByNumber;
                    _token = new SyntaxTokenValue(tokenKind, start, end, stringValue: dateTimeAsString);
                }
                else if (DateTimeRFC3339.TryParseLocalDateTime(dateTimeAsString, out datetime))
                {
                    _token = new SyntaxTokenValue(TokenKind.LocalDateTime, start, end, stringValue: dateTimeAsString);
                }
                else if (DateTimeRFC3339.TryParseLocalDate(dateTimeAsString, out datetime))
                {
                    _token = new SyntaxTokenValue(TokenKind.LocalDate, start, end, stringValue: dateTimeAsString);
                }
                else if (DateTimeRFC3339.TryParseLocalTime(dateTimeAsString, out datetime))
                {
                    _token = new SyntaxTokenValue(TokenKind.LocalTime, start, end, stringValue: dateTimeAsString);
                }
                else
                {
                    // Try to recover the date using the standard C# (not necessarily RFC3339)
                    if (DateTime.TryParse(dateTimeAsString, CultureInfo.InvariantCulture, DateTimeStyles.AllowInnerWhite, out var rawTime))
                    {
                        datetime = new TomlDateTime(rawTime, 0, TomlDateTimeKind.LocalDateTime);
                        _token = new SyntaxTokenValue(TokenKind.LocalDateTime, start, end, stringValue: dateTimeAsString);

                        // But we produce an error anyway
                        AddError($"Invalid format of date time/offset `{dateTimeAsString}` not following RFC3339", start, end);
                    }
                    else
                    {
                        datetime = new TomlDateTime(DateTimeOffset.MinValue, 0, TomlDateTimeKind.LocalDateTime);
                        _token = new SyntaxTokenValue(TokenKind.LocalDateTime, start, end, stringValue: dateTimeAsString);
                        // But we produce an error anyway
                        AddError($"Unable to parse the date time/offset `{dateTimeAsString}`", start, end);
                    }
                }

                return;
            }

            if (hasMultipleLeadingZero)
            {
                AddError("Multiple leading 0 are not allowed", beforeFollowingZero, beforeFollowingZero);
            }

            // Read any number following
            if (_c == '.')
            {
                _textBuilder.Append('.');
                end = _position;
                NextChar(); // Skip the dot .

                // We expect at least a digit after .
                if (!CharHelper.IsDigit(_c))
                {
                    AddError("Expecting at least one digit after the float dot .", _position, _position);
                    _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
                    return;
                }

                isFloat = true;
                ReadDigits(ref end, false);
            }

            // Parse only the exponent if we don't have a range
            if (_c == 'e' || _c == 'E')
            {
                isFloat = true;

                _textBuilder.AppendUtf32(_c);
                end = _position;
                NextChar();
                if (_c == '+' || _c == '-')
                {
                    _textBuilder.AppendUtf32(_c);
                    end = _position;
                    NextChar();
                }

                if (!CharHelper.IsDigit(_c))
                {
                    AddError("Expecting at least one digit after the exponent", _position, _position);
                    _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
                    return;
                }
                ReadDigits(ref end, false);
            }

            if (isFloat)
            {
                if (!TryParseDecimalDouble(out var doubleValue))
                {
                    var numberAsText = _textBuilder.ToString();
                    AddError($"Unable to parse floating point `{numberAsText}`", start, end);
                    doubleValue = 0.0;
                }

                if (hasLeadingZero && HasMultipleDigitsInIntegerPart())
                {
                    var numberAsText = _textBuilder.ToString();
                    AddError($"Unexpected leading zero (`0`) for float `{numberAsText}`", positionFirstDigit, positionFirstDigit);
                }

                var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(doubleValue));
                _token = new SyntaxTokenValue(TokenKind.Float, start, end, stringValue: null, data: bits);
            }
            else
            {
                if (!TryParseDecimalInt64(out var longValue))
                {
                    var numberAsText = _textBuilder.ToString();
                    AddError($"Unable to parse integer `{numberAsText}`", start, end);
                    longValue = 0;
                }

                if (hasLeadingZero && longValue != 0)
                {
                    var numberAsText = _textBuilder.ToString();
                    AddError($"Unexpected leading zero (`0`) for integer `{numberAsText}`", positionFirstDigit, positionFirstDigit);
                }

                _token = new SyntaxTokenValue(TokenKind.Integer, start, end, stringValue: null, data: unchecked((ulong)longValue));
            }
        }

        private bool TryParseDecimalInt64(out long value)
        {
            value = 0;
            var length = _textBuilder.Length;
            if (length == 0)
            {
                return false;
            }

            var index = 0;
            var negative = false;
            var first = _textBuilder[0];
            if (first == '+' || first == '-')
            {
                negative = first == '-';
                index = 1;
                if (index >= length)
                {
                    return false;
                }
            }

            ulong accumulator = 0;
            for (; index < length; index++)
            {
                var digitChar = _textBuilder[index];
                var digit = digitChar - '0';
                if ((uint)digit > 9)
                {
                    return false;
                }

                var digitValue = (ulong)digit;
                if (accumulator > (ulong.MaxValue - digitValue) / 10)
                {
                    return false;
                }

                accumulator = (accumulator * 10) + digitValue;
            }

            if (negative)
            {
                if (accumulator == 0x8000_0000_0000_0000UL)
                {
                    value = long.MinValue;
                    return true;
                }

                if (accumulator > 0x8000_0000_0000_0000UL)
                {
                    return false;
                }

                value = unchecked(-(long)accumulator);
                return true;
            }

            value = unchecked((long)accumulator);
            return true;
        }

        private bool TryParseDecimalDouble(out double value)
        {
#if NETSTANDARD2_0
            var numberAsText = _textBuilder.ToString();
            return double.TryParse(numberAsText, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
#else
            value = 0.0;
            var length = _textBuilder.Length;
            if (length == 0)
            {
                return false;
            }

            char[]? rented = null;
            try
            {
                Span<char> buffer = length <= 128
                    ? stackalloc char[length]
                    : (rented = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);

                _textBuilder.CopyTo(0, buffer, length);
                return double.TryParse(buffer, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<char>.Shared.Return(rented);
                }
            }
#endif
        }

        private bool HasMultipleDigitsInIntegerPart()
        {
            var length = _textBuilder.Length;
            if (length <= 1)
            {
                return false;
            }

            var index = 0;
            var first = _textBuilder[0];
            if (first == '+' || first == '-')
            {
                index = 1;
                if (index >= length)
                {
                    return false;
                }
            }

            var end = index;
            while (end < length)
            {
                var c = _textBuilder[end];
                if (c == '.' || c == 'e' || c == 'E')
                {
                    break;
                }

                end++;
            }

            return (end - index) > 1;
        }

        private void ReadDigits(ref TextPosition end, bool isPreviousDigit)
        {
            bool isDigit;
            while ((isDigit = CharHelper.IsDigit(_c)) || _c == '_')
            {
                if (isDigit)
                {
                    _textBuilder.AppendUtf32(_c);
                    isPreviousDigit = true;
                }
                else if (!isPreviousDigit)
                {
                    AddError("An underscore `_` must follow a digit and not another `_`", _position, _position);
                }
                else
                {
                    isPreviousDigit = false;
                }
                end = _position;
                NextChar();
            }

            if (!isPreviousDigit)
            {
                AddError("Missing a digit after a trailing underscore `_`", _position, _position);
            }
        }

        private void ReadString(TextPosition start, bool allowMultiline)
        {
            var end = _position;
            bool isMultiLine = false;

            NextChar(); // Skip "
            if (allowMultiline && _c == '"')
            {
                end = _position;
                NextChar();

                if (_c == '"')
                {
                    end = _position;
                    NextChar();
                    // we have an opening ''' -> this a multi-line string
                    isMultiLine = true;
                    SkipImmediateNextLine();
                }
                else
                {
                    // Else this is an empty string
                    _token = new SyntaxTokenValue(TokenKind.String, start, end, DecodeScalars ? string.Empty : null);
                    return;
                }
            }

            // Reset the current string buffer
            _textBuilder.Length = 0;

            continue_parsing_string:
            while (_c != '\"' && _c != Eof)
            {
                if (_c == '\r' && PeekChar() != '\n')
                {
                    AddError($"Invalid \\r not followed by \\n", _position, _position);
                }

                if (!TryReadEscapeChar(ref end))
                {
                    if (!isMultiLine && CharHelper.IsNewLine(_c))
                    {
                        AddError("Invalid newline in a string", _position, _position);
                    }
                    else if (CharHelper.IsControlCharacter(_c) && (!isMultiLine || !CharHelper.IsWhiteSpaceOrNewLine(_c)))
                    {
                        AddError($"Invalid control character found {((char)_c).ToPrintableString()}", start, start);
                    }

                    if (DecodeScalars)
                    {
                        _textBuilder.AppendUtf32(_c);
                    }
                    end = _position;
                    NextChar();
                }
            }

            if (isMultiLine)
            {
                if (_c == '"')
                {
                    int count = 0;
                    while (_c == '"')
                    {
                        if (count >= 5) break;
                        count++;
                        end = _position;
                        NextChar();
                    }

                    if (count >= 3)
                    {
                        for (int i = 0; i < count - 3; i++)
                        {
                            if (DecodeScalars)
                            {
                                _textBuilder.Append('"');
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (DecodeScalars)
                            {
                                _textBuilder.Append('"');
                            }
                        }
                        goto continue_parsing_string;
                    }
                }
                else
                {
                    AddError("Invalid End-Of-File found for multi-line string", end, end);
                }
                _token = new SyntaxTokenValue(TokenKind.StringMulti, start, end, DecodeScalars ? _textBuilder.ToString() : null);
            }
            else
            {
                if (_c == '"')
                {
                    end = _position;
                    NextChar();
                }
                else
                {
                    AddError("Invalid End-Of-File found on string literal", end, end);
                }
                _token = new SyntaxTokenValue(TokenKind.String, start, end, DecodeScalars ? _textBuilder.ToString() : null);
            }
        }

        private void SkipImmediateNextLine()
        {
            // Skip any white spaces until the next line
            if (_c == '\r')
            {
                var start = _position;
                NextChar();
                if (_c == '\n')
                {
                    NextChar();
                }
                else
                {
                    AddError($"Invalid \\r not followed by \\n", start, start);
                }
            }
            else if (_c == '\n')
            {
                NextChar();
            }
        }

        private bool TryReadEscapeChar(ref TextPosition end)
        {
            if (_c == '\\')
            {
                end = _position;
                NextChar();
                // 0 \ ' " a b f n r t v u0000-uFFFF x00-xFF
                switch (_c)
                {
                    case 'b':
                        if (DecodeScalars) _textBuilder.Append('\b');
                        end = _position;
                        NextChar();
                        return true;
                    case 't':
                        if (DecodeScalars) _textBuilder.Append('\t');
                        end = _position;
                        NextChar();
                        return true;
                    case 'n':
                        if (DecodeScalars) _textBuilder.Append('\n');
                        end = _position;
                        NextChar();
                        return true;
                    case 'f':
                        if (DecodeScalars) _textBuilder.Append('\f');
                        end = _position;
                        NextChar();
                        return true;
                    case 'r':
                        if (DecodeScalars) _textBuilder.Append('\r');
                        end = _position;
                        NextChar();
                        return true;
                    case '"':
                        if (DecodeScalars) _textBuilder.Append('"');
                        end = _position;
                        NextChar();
                        return true;
                    case '\\':
                        if (DecodeScalars) _textBuilder.Append('\\');
                        end = _position;
                        NextChar();
                        return true;
                    case 'e':
                        if (DecodeScalars) _textBuilder.Append('\u001B');
                        end = _position;
                        NextChar();
                        return true;
                    case 'x':
                    {
                        var start = _position;
                        end = _position;
                        NextChar();

                        if (!CharHelper.IsHexFunc(_c))
                        {
                            AddError("Invalid escape `\\x`. Expected 2 hexadecimal digits.", start, start);
                            return false;
                        }

                        var value = CharHelper.HexToDecimal(_c);
                        end = _position;
                        NextChar();

                        if (!CharHelper.IsHexFunc(_c))
                        {
                            AddError("Invalid escape `\\x`. Expected 2 hexadecimal digits.", start, start);
                            return false;
                        }

                        value = (value << 4) + CharHelper.HexToDecimal(_c);
                        end = _position;
                        NextChar();

                        if (DecodeScalars) _textBuilder.Append((char)value);
                        return true;
                    }

                    // toml-specs:  When the last non-whitespace character on a line is a \,
                    // it will be trimmed along with all whitespace (including newlines)
                    // up to the next non-whitespace character or closing delimiter. 
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        var startWithSpace = _c == ' ';
                        var startPosition = _position;
                        while (CharHelper.IsWhiteSpaceOrNewLine(_c))
                        {
                            end = _position;
                            NextChar();
                        }

                        if (startWithSpace && end.Line == startPosition.Line)
                        {
                            AddError("Invalid escape `\\`. It must skip at least one line.", startPosition, startPosition);
                        }

                        return true;

                    case 'u':
                    case 'U':
                    {
                        var start = _position;
                        end = _position;
                        var maxCount = _c == 'u' ? 4 : 8;
                        NextChar();

                        // Must be followed 0 to 8 hex numbers (0-FFFFFFFF)
                        int i = 0;
                        int value = 0;
                        for (; CharHelper.IsHexFunc(_c) && i < maxCount; i++)
                        {
                            value = (value << 4) + CharHelper.HexToDecimal(_c);
                            end = _position;
                            NextChar();
                        }

                        if (i == maxCount)
                        {
                            if (!CharHelper.IsValidUnicodeScalarValue(value))
                            {
                                AddError($"Invalid Unicode scalar value [{value:X}]",start, start);
                            }
                            if (DecodeScalars) _textBuilder.AppendUtf32((char32)value);
                            return true;
                        }
                    }
                        break;
                }

                AddError($"Unexpected escape character [{_c}] in string. Only b t n f r e \\ \" xHH u0000-uFFFF U00000000-UFFFFFFFF are allowed", _position, _position);
                return false;
            }
            return false;
        }

        private void ReadStringLiteral(TextPosition start, bool allowMultiline)
        {
            var end = _position;

            bool isMultiLine = false;

            NextChar(); // Skip '
            if (allowMultiline && _c == '\'')
            {
                end = _position;
                NextChar();

                if (_c == '\'')
                {
                    end = _position;
                    NextChar();
                    // we have an opening ''' -> this a multi-line literal string
                    isMultiLine = true;

                    SkipImmediateNextLine();
                }
                else
                {
                    // Else this is an empty literal string
                    _token = new SyntaxTokenValue(TokenKind.StringLiteral, start, end, DecodeScalars ? string.Empty : null);
                    return;
                }
            }

            _textBuilder.Length = 0;
            continue_parsing_string:
            while (_c != '\'' && _c != Eof)
            {
                if (_c == '\r' && PeekChar() != '\n')
                {
                    AddError($"Invalid \\r not followed by \\n", _position, _position);
                }

                if (!isMultiLine && CharHelper.IsNewLine(_c))
                {
                    AddError("Invalid newline in a string", _position, _position);
                }
                else if (CharHelper.IsControlCharacter(_c) && _c != '\t' && (!isMultiLine || !CharHelper.IsNewLine(_c)))
                {
                    AddError($"Invalid control character found {((char)_c).ToPrintableString()}", start, start);
                }
                if (DecodeScalars)
                {
                    _textBuilder.AppendUtf32(_c);
                }
                end = _position;
                NextChar();
            }

            if (isMultiLine)
            {
                if (_c == '\'')
                {
                    int count = 0;
                    while (_c == '\'')
                    {
                        if (count >= 5) break;
                        count++;
                        end = _position;
                        NextChar();
                    }

                    if (count >= 3)
                    {
                        for (int i = 0; i < count - 3; i++)
                        {
                            if (DecodeScalars)
                            {
                                _textBuilder.Append('\'');
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (DecodeScalars)
                            {
                                _textBuilder.Append('\'');
                            }
                        }
                        goto continue_parsing_string;
                    }

                }
                else
                {
                    AddError("Invalid End-Of-File found for multi-line literal string", end, end);
                }
                _token = new SyntaxTokenValue(TokenKind.StringLiteralMulti, start, end, DecodeScalars ? _textBuilder.ToString() : null);
            }
            else
            {
                if (_c == '\'')
                {
                    end = _position;
                    NextChar();
                }
                else
                {
                    AddError("Invalid End-Of-File found on string literal", end, end);
                }
                _token = new SyntaxTokenValue(TokenKind.StringLiteral, start, end, DecodeScalars ? _textBuilder.ToString() : null);
            }
        }
        

        private void ReadComment(TextPosition start)
        {
            var end = start;
            // Read until the end of the line/file
            while (_c != Eof && _c != '\r' && _c != '\n')
            {
                // Invalid characters for comment
                // U+0000 to U+0008, U+000A to U+001F, U+007F
                if (_c >= 0 && _c <= 8 || _c >= 0xa && _c <= 0x1f || _c == 0x7f)
                {
                    AddError($"Invalid control character U+{_c.Code:X4} in comment", _position, _position);
                }
                end = _position;
                NextChar();
            }

            if (_c == '\r' && PeekChar() != '\n')
            {
                AddError($"Invalid control character U+{_c.Code:X4} in comment", _position, _position);
            }

            _token = new SyntaxTokenValue(TokenKind.Comment, start, end);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NextChar()
        {
            // If we have a character in preview
            if (_preview1 != null)
            {
                _current = _preview1.Value;
                _preview1 = null;
                return;
            }

            // Else move to the next position
            _current.Position = _current.NextPosition;
            _current.PreviousChar = _current.CurrentChar; // save the previous character
            _current.CurrentChar = NextCharFromReader();
        }

        // Peek one char ahead
        private char32 PeekChar()
        {
            if (_preview1 == null)
            {
                var saved = _current;
                NextChar();
                _preview1 = _current;
                _current = saved;
            }

            return _preview1.Value.CurrentChar;
        }

        private char32 NextCharFromReader()
        {
            try
            {
                int position = _position.Offset;
                var nextChar = _reader.TryGetNext(ref position);
                _current.NextPosition.Offset = position;

                if (nextChar.HasValue)
                {
                    var nextc = nextChar.Value;
                    if (nextc == '\n')
                    {
                        _current.NextPosition.Column = 0;
                        _current.NextPosition.Line += 1;
                    }
                    else
                    {
                        _current.NextPosition.Column++;
                    }
                    CheckCharacter(nextc);
                    return nextc;
                }

            }
            catch (CharReaderException ex)
            {
                AddError(ex.Message, _position, _position);
            }

            return Eof;
        }

        private void CheckCharacter(char32 c)
        {
            // The character 0xFFFD is the replacement character and we assume that something went wrong when reading the input
            if (!CharHelper.IsValidUnicodeScalarValue(c) || c == 0xFFFD)
            {
                AddError($"The character `{c}` is an invalid UTF8 character", _current.Position, _current.Position);
            }
        }

        private void AddError(string message, TextPosition start, TextPosition end)
        {
            if (_errors == null)
            {
                _errors = new List<DiagnosticMessage>();
            }
            _errors.Add(new DiagnosticMessage(DiagnosticMessageKind.Error, new SourceSpan(_sourceView.SourcePath, start, end), message));
        }

        private void Reset()
        {
            // Initialize the position at -1 when starting
            _preview1 = null;
            _current = new LexerInternalState {Position = new TextPosition(_reader.Start, 0, 0)};
            // It is important to initialize this separately from the previous line
            _current.CurrentChar = NextCharFromReader();
            _token = new SyntaxTokenValue();
            _errors = null;
        }
    }
    
    [DebuggerDisplay("{Position} {Character}")]
    internal struct LexerInternalState
    {
        public LexerInternalState(TextPosition nextPosition, TextPosition position, char32 previousChar, char32 c)
        {
            NextPosition = nextPosition;
            Position = position;
            PreviousChar = previousChar;
            CurrentChar = c;
        }

        public TextPosition NextPosition;

        public TextPosition Position;

        public char32 PreviousChar;

        public char32 CurrentChar;
    }

}

