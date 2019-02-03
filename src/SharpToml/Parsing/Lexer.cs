// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using SharpToml.Syntax;
using SharpToml.Text;

// tests https://github.com/toml-lang/toml/issues/585#issuecomment-455408303

namespace SharpToml.Parsing
{
    /// <summary>
    /// Lexer enumerator that generates <see cref="SyntaxTokenValue"/>, to be used from a foreach.
    /// </summary>
    internal class Lexer<TSourceView, TCharReader> : ITokenProvider<TSourceView> where TSourceView : struct, ISourceView<TCharReader> where TCharReader : struct, CharacterIterator
    {
        private SyntaxTokenValue _token;
        private TextPosition _position;
        private TextPosition _nextPosition;
        private char32 _pc1; // previous previous character - 1
        private char32 _pc; // previous character
        private char32 _c;
        private List<SyntaxMessage> _errors;
        private TCharReader _reader;
        private const int Eof = -1;
        private TSourceView _sourceView;
        private readonly StringBuilder _textBuilder;
        private readonly List<char32> _currentIdentifierChars;

        /// <summary>
        /// Initialize a new instance of this <see cref="Lexer{TSourceView,TCharReader}" />.
        /// </summary>
        /// <param name="textReader">The text to analyze</param>
        /// <param name="sourcePath">The file path used for error reporting only.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentNullException">If text is null</exception>
        public Lexer(TSourceView sourceView, string sourcePath = null)
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
        public IEnumerable<SyntaxMessage> Errors => _errors ?? Enumerable.Empty<SyntaxMessage>();

        public bool MoveNext()
        {
            // If we have errors or we are already at the end of the file, we don't continue
            if (_token.Kind == TokenKind.Eof)
            {
                return false;
            }

            if (State == LexerSate.Key)
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

        public LexerSate State { get; set; }

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
                    _token = SyntaxTokenValue.Eof;
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
                    _token = SyntaxTokenValue.Eof;
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
                        ReadNumber();
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
            _currentIdentifierChars.Add(_c);
            NextChar();

            while (CharHelper.IsIdentifierContinue(_c))
            {
                // We track an identifier to check if it is a keyword (inf, true, false)
                _currentIdentifierChars.Add(_c);

                end = _position;
                NextChar();
            }

            if (MatchCurrentIdentifier("true"))
            {
                _token = new SyntaxTokenValue(TokenKind.True, start, end, BoxedValues.True);
            }
            else if (MatchCurrentIdentifier("false"))
            {
                _token = new SyntaxTokenValue(TokenKind.False, start, end, BoxedValues.False);
            }
            else if (MatchCurrentIdentifier("inf"))
            {
                _token = new SyntaxTokenValue(TokenKind.Infinite, start, end, BoxedValues.FloatPositiveInfinity);
            }
            else if (MatchCurrentIdentifier("+inf"))
            {
                _token = new SyntaxTokenValue(TokenKind.PositiveInfinite, start, end, BoxedValues.FloatPositiveInfinity);
            }
            else if (MatchCurrentIdentifier("-inf"))
            {
                _token = new SyntaxTokenValue(TokenKind.NegativeInfinite, start, end, BoxedValues.FloatNegativeInfinity);
            }
            else if (MatchCurrentIdentifier("nan"))
            {
                _token = new SyntaxTokenValue(TokenKind.Nan, start, end, BoxedValues.FloatNan);
            }
            else if (MatchCurrentIdentifier("+nan"))
            {
                _token = new SyntaxTokenValue(TokenKind.PositiveNan, start, end, BoxedValues.FloatNan);
            }
            else if (MatchCurrentIdentifier("-nan"))
            {
                _token = new SyntaxTokenValue(TokenKind.NegativeNan, start, end, BoxedValues.FloatNan); // TODO: Add negative Nan
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

        private void ReadNumber()
        {
            var start = _position;
            var end = _position;
            var isFloat = false;

            var firstChar = _c;
            var startsWithZero = _c == '0';
            NextChar(); // Skip first digit character

            // If we start with 0, it might be an hexa, octal or binary literal
            if (startsWithZero)
            {
                if (_c == 'x' || _c == 'X' || _c == 'o' || _c == 'O' || _c == 'b' || _c == 'B')
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
                        _token = new SyntaxTokenValue(tokenKind, start, end, (long)value);
                    }
                    return;
                }
            }

            // Reset parsing of integer
            _textBuilder.Length = 0;
            _textBuilder.Append(firstChar);

            // Parse all digits
            bool isDigit;
            while ((isDigit = CharHelper.IsDigit(_c)) || _c == '_') 
            {
                if (isDigit)
                {
                    _textBuilder.Append(_c);
                }
                end = _position;
                NextChar();
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
                while ((isDigit = CharHelper.IsDigit(_c)) || _c == '_')
                {
                    if (isDigit)
                    {
                        _textBuilder.Append(_c);
                    }
                    end = _position;
                    NextChar();
                }
            }

            // Parse only the exponent if we don't have a range
            if (_c == 'e' || _c == 'E')
            {
                isFloat = true;

                _textBuilder.Append(_c);
                end = _position;
                NextChar();
                if (_c == '+' || _c == '-')
                {
                    _textBuilder.Append(_c);
                    end = _position;
                    NextChar();
                }

                if (!CharHelper.IsDigit(_c))
                {
                    AddError("Expecting at least one digit after the exponent", _position, _position);
                    _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
                    return;
                }

                while ((isDigit = CharHelper.IsDigit(_c)) || _c == '_')
                {
                    if (isDigit)
                    {
                        _textBuilder.Append(_c);
                    }
                    end = _position;
                    NextChar();
                }
            }

            var numberAsText = _textBuilder.ToString();
            object resolvedValue;
            if (isFloat)
            {
                if (!double.TryParse(numberAsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    AddError($"Unable to parse floating point `{numberAsText}`", start, end);
                    _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
                    return;
                }
                // If value is 0.0 or 1.0, use box cached otherwise box
                resolvedValue = doubleValue == 0.0 ? BoxedValues.FloatZero : doubleValue == 1.0 ? BoxedValues.FloatOne : doubleValue;
            }
            else
            {
                if (!long.TryParse(numberAsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                {
                    AddError($"Unable to parse integer `{numberAsText}`", start, end);
                    _token = new SyntaxTokenValue(TokenKind.Invalid, start, end);
                    return;
                }
                // If value is 0 or 1, use box cached otherwise box
                resolvedValue = longValue == 0 ? BoxedValues.IntegerZero : longValue == 1 ? BoxedValues.IntegerOne : longValue;
            }

            _token = new SyntaxTokenValue(isFloat ? TokenKind.Float : TokenKind.Integer, start, end, resolvedValue);
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
                }
                else
                {
                    // Else this is an empty string
                    _token = new SyntaxTokenValue(TokenKind.String, start, end, string.Empty);
                    return;
                }
            }

            // Reset the current string buffer
            _textBuilder.Length = 0;

            continue_parsing_string:
            while (_c != '\"' && _c != Eof)
            {
                if (!TryReadEscapeChar(ref end))
                {
                    _textBuilder.Append(_c);
                    end = _position;
                    NextChar();
                }
            }

            if (isMultiLine)
            {
                if (_c == '"')
                {
                    end = _position;
                    NextChar();
                    if (_c == '"')
                    {
                        end = _position;
                        NextChar();
                        if (_c == '"')
                        {
                            end = _position;
                            NextChar();
                        }
                        else
                        {
                            goto continue_parsing_string;
                        }
                    }
                    else
                    {
                        goto continue_parsing_string;
                    }
                }
                else
                {
                    AddError("Invalid End-Of-File found for multi-line string", end, end);
                }
                _token = new SyntaxTokenValue(TokenKind.StringMulti, start, end, _textBuilder.ToString());
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
                _token = new SyntaxTokenValue(TokenKind.String, start, end, _textBuilder.ToString());
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
                        _textBuilder.Append('\b');
                        end = _position;
                        NextChar();
                        return true;
                    case 't':
                        _textBuilder.Append('\t');
                        end = _position;
                        NextChar();
                        return true;
                    case 'n':
                        _textBuilder.Append('\n');
                        end = _position;
                        NextChar();
                        return true;
                    case 'f':
                        _textBuilder.Append('\f');
                        end = _position;
                        NextChar();
                        return true;
                    case 'r':
                        _textBuilder.Append('\r');
                        end = _position;
                        NextChar();
                        return true;
                    case '"':
                        _textBuilder.Append('"');
                        end = _position;
                        NextChar();
                        return true;
                    case '\\':
                        end = _position;
                        NextChar();

                        // toml-specs:  When the last non-whitespace character on a line is a \,
                        // it will be trimmed along with all whitespace (including newlines)
                        // up to the next non-whitespace character or closing delimiter. 
                        if (_c == '\r' || _c == '\n')
                        {
                            while (CharHelper.IsWhiteSpace(_c))
                            {
                                end = _position;
                                NextChar();
                            }
                        }
                        else
                        {
                            _textBuilder.Append('\\');
                        }
                        return true;
                    case 'u':
                    case 'U':
                    {
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
                            CharHelper.AppendFromUtf32(value, _textBuilder);
                            return true;
                        }
                    }
                        break;
                }

                AddError($"Unexpected escape character [{_c}] in string. Only 0 'b t n f r \\ \" u0000-uFFFF U00000000-UFFFFFFFF are allowed", _position, _position);
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
                }
                else
                {
                    // Else this is an empty literal string
                    _token = new SyntaxTokenValue(TokenKind.StringLiteral, start, end, string.Empty);
                    return;
                }
            }

            _textBuilder.Length = 0;
            continue_parsing_string:
            while (_c != '\'' && _c != Eof)
            {
                _textBuilder.Append(_c);
                end = _position;
                NextChar();
            }

            if (isMultiLine)
            {
                if (_c == '\'')
                {
                    end = _position;
                    NextChar();
                    if (_c == '\'')
                    {
                        end = _position;
                        NextChar();
                        if (_c == '\'')
                        {
                            end = _position;
                            NextChar();
                        }
                        else
                        {
                            goto continue_parsing_string;
                        }
                    }
                    else
                    {
                        goto continue_parsing_string;
                    }
                }
                else
                {
                    AddError("Invalid End-Of-File found for multi-line literal string", end, end);
                }
                _token = new SyntaxTokenValue(TokenKind.StringLiteralMulti, start, end, _textBuilder.ToString());
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
                _token = new SyntaxTokenValue(TokenKind.StringLiteral, start, end, _textBuilder.ToString());
            }
        }
        

        private void ReadComment(TextPosition start)
        {
            var end = _position;
            // Read until the end of the line/file
            while (_c != Eof && _c != '\r' && _c != '\n')
            {
                end = _position;
                NextChar();
            }

            _token = new SyntaxTokenValue(TokenKind.Comment, start, end);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NextChar()
        {
            // Else move to the next position
            _position = _nextPosition;
            _pc1 = _pc; // save the previous character -1
            _pc = _c; // save the previous character
            _c = NextCharFromReader();
        }

        private char32 NextCharFromReader()
        {
            try
            {
                int position = _position.Offset;
                var nextChar = _reader.TryGetNext(ref position);
                _nextPosition.Offset = position;

                if (nextChar.HasValue)
                {
                    var nextc = nextChar.Value;
                    if (nextc == '\n')
                    {
                        _nextPosition.Column = 0;
                        _nextPosition.Line += 1;
                    }
                    else
                    {
                        _nextPosition.Column++;
                    }
                    return nextc;
                }

            }
            catch (CharReaderException ex)
            {
                AddError(ex.Message, _position, _position);
            }

            return Eof;
        }

        private void AddError(string message, TextPosition start, TextPosition end)
        {
            if (_errors == null)
            {
                _errors = new List<SyntaxMessage>();
            }
            _errors.Add(new SyntaxMessage(SyntaxMessageKind.Error, new SourceSpan(_sourceView.SourcePath, start, end), message));
        }

        private void Reset()
        {
            // Initialize the position at -1 when starting
            _nextPosition = new TextPosition();
            _position = new TextPosition();
            _pc = 0;
            _c = NextCharFromReader();

            _token = new SyntaxTokenValue();
            _errors = null;
        }


        private class BoxedValues
        {
            public static object True = true;
            public static object False = false;
            public static object IntegerZero = (long) 0;
            public static object IntegerOne = (long)1;
            public static object FloatZero = 0.0;
            public static object FloatOne = 1.0;
            public static object FloatPositiveInfinity = double.PositiveInfinity;
            public static object FloatNegativeInfinity = double.NegativeInfinity;
            public static object FloatNan = double.NaN;
        }
    }
}

