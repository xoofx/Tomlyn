// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class ArrayItemSyntax : SyntaxNode
    {
        private ValueSyntax _value;
        private SyntaxToken _comma;

        public ValueSyntax Value
        {
            get => _value;
            set => ParentToThis(ref _value, value);
        }

        public SyntaxToken Comma
        {
            get => _comma;
            set => ParentToThis(ref _comma, value, TokenKind.Comma);
        }

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override int ChildrenCount => 2;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return index == 0 ? (SyntaxNode)Value : Comma;
        }
    }
}