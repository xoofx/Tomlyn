// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class InlineTableSyntax : ValueSyntax
    {
        private SyntaxToken _openBrace;
        private SyntaxToken _closeBrace;

        public InlineTableSyntax() : base(SyntaxKind.InlineTable)
        {
            Items = new SyntaxList<InlineTableItemSyntax>() { Parent = this };
        }

        public SyntaxToken OpenBrace
        {
            get => _openBrace;
            set => ParentToThis(ref _openBrace, value, TokenKind.OpenBrace);
        }

        public SyntaxList<InlineTableItemSyntax> Items { get; }

        public SyntaxToken CloseBrace
        {
            get => _closeBrace;
            set => ParentToThis(ref _closeBrace, value, TokenKind.CloseBrace);
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override int ChildrenCount => 3;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            switch (index)
            {
                case 0:
                    return OpenBrace;
                case 1:
                    return Items;
                default:
                    return CloseBrace;
            }
        }
    }
}