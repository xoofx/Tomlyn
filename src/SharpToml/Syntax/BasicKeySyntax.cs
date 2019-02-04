// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class BasicKeySyntax : BasicValueSyntax
    {
        private SyntaxToken _key;

        public BasicKeySyntax() : base(SyntaxKind.BasicKey)
        {
        }

        public SyntaxToken Key
        {
            get => _key;
            set => ParentToThis(ref _key, value, TokenKind.BasicKey);
        }

        public override int ChildrenCount => 1;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Key;
        }
    }
}