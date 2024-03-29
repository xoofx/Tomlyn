// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using Tomlyn.Helpers;
using Tomlyn.Model;

namespace Tomlyn.Syntax
{
    public sealed class SyntaxTrivia : SyntaxNodeBase
    {
        public SyntaxTrivia()
        {
        }

        public SyntaxTrivia(TokenKind kind, string text)
        {
            if (!kind.IsTrivia()) throw new ArgumentOutOfRangeException(nameof(kind), $"The kind `{kind}` is not a trivia");
            Kind = kind;
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }
        
        public TokenKind Kind { get; set; }

        public string? Text { get; set; }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected override string ToDebuggerDisplay()
        {
            return $"{base.ToDebuggerDisplay()} {(Text is not null ? TomlFormatHelper.ToString(Text, TomlPropertyDisplayKind.Default) : string.Empty)}";
        }
    }
}