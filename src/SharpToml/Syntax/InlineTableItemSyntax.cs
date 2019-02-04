// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class InlineTableItemSyntax : SyntaxNode
    {
        private KeyValueSyntax _keyValue;
        private SyntaxToken _comma;

        public InlineTableItemSyntax() : base(SyntaxKind.InlineTable)
        {
        }

        public KeyValueSyntax KeyValue
        {
            get => _keyValue;
            set => ParentToThis(ref _keyValue, value);
        }

        public SyntaxToken Comma
        {
            get => _comma;
            set => ParentToThis(ref _comma, value, TokenKind.Comma);
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override int ChildrenCount => 2;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return index == 0 ? (SyntaxNode)KeyValue : Comma;
        }
    }
}