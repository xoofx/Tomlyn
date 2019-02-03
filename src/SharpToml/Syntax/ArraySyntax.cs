// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class ArraySyntax : ValueSyntax
    {
        private SyntaxToken _openBracket;
        private SyntaxToken _closeBracket;

        public ArraySyntax()
        {
            Items = new SyntaxList<ArrayItemSyntax>() { Parent = this };
        }

        public SyntaxToken OpenBracket
        {
            get => _openBracket;
            set => ParentToThis(ref _openBracket, value, TokenKind.OpenBracket);
        }

        public SyntaxList<ArrayItemSyntax> Items { get; }

        public SyntaxToken CloseBracket
        {
            get => _closeBracket;
            set => ParentToThis(ref _closeBracket, value, TokenKind.CloseBracket);
        }

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override int ChildrenCount => 3;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            switch (index)
            {
                case 0:
                    return OpenBracket;
                case 1:
                    return Items;
                default:
                    return CloseBracket;
            }
        }
    }
}