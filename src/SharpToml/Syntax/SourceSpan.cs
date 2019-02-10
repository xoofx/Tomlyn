// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public struct SourceSpan
    {
        public SourceSpan(string fileName, TextPosition start, TextPosition end)
        {
            FileName = fileName;
            Start = start;
            End = end;
        }

        public string FileName;

        public int Offset => Start.Offset;

        public int Length => End.Offset - Start.Offset + 1;


        public TextPosition Start;

        public TextPosition End;

        public override string ToString()
        {
            return $"{FileName}{Start}-{End}";
        }

        public string ToStringSimple()
        {
            return $"{FileName}{Start}";
        }
    }
}