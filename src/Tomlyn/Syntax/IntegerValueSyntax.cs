// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace Tomlyn.Syntax
{
    public sealed class IntegerValueSyntax : ValueSyntax
    {
        private SyntaxToken _token;

        public IntegerValueSyntax() : base(SyntaxKind.Integer)
        {
        }

        public SyntaxToken Token
        {
            get => _token;
            set => ParentToThis(ref _token, value, value != null && value.TokenKind.IsInteger(), "decimal/hex/binary/octal integer");
        }

        public long Value { get; set; }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override int ChildrenCount => 1;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Token;
        }
    }
}