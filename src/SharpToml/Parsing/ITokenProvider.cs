// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System.Collections.Generic;
using SharpToml.Syntax;
using SharpToml.Text;

namespace SharpToml.Parsing
{
    internal interface ITokenProvider<out TSourceView>  where TSourceView : ISourceView 
    {
        /// <summary>
        /// Gets a boolean indicating whether this lexer has errors.
        /// </summary>
        bool HasErrors { get; }

        TSourceView Source { get; }

        LexerSate State { get; set; }

        bool MoveNext();

        SyntaxTokenValue Token { get; }

        /// <summary>
        /// Gets error messages.
        /// </summary>
        IEnumerable<SyntaxMessage> Errors { get; }
    }
}