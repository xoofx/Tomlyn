// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class DottedKeyItemSyntax : ValueSyntax
    {
        private SyntaxToken _dot;
        private BasicValueSyntax _value;

        public SyntaxToken Dot
        {
            get => _dot;
            set => ParentToThis(ref _dot, value, TokenKind.Dot);
        }

        public BasicValueSyntax Value
        {
            get => _value;
            set => ParentToThis(ref _value, value);
        }

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override int ChildrenCount => 2;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return index == 0 ? (SyntaxNode)Dot : Value;
        }
    }
}