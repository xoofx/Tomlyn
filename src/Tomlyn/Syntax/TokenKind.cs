// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace Tomlyn.Syntax
{
    /// <summary>
    /// An enumeration to categorize tokens.
    /// </summary>
    public enum TokenKind
    {
        Invalid,

        Eof,

        Whitespaces,

        NewLine,

        Comment,

        OffsetDateTimeByZ,
        OffsetDateTimeByNumber,
        LocalDateTime,
        LocalDate,
        LocalTime,

        Integer,

        IntegerHexa,

        IntegerOctal,

        IntegerBinary,

        Float,
        
        String,

        StringMulti,

        StringLiteral,

        StringLiteralMulti,

        Comma,

        Dot,

        Equal,

        OpenBracket,
        OpenBracketDouble,
        CloseBracket,
        CloseBracketDouble,

        OpenBrace,
        CloseBrace,

        True,
        False,

        Infinite,
        PositiveInfinite,
        NegativeInfinite,

        Nan,
        PositiveNan,
        NegativeNan,

        BasicKey,
    }
}