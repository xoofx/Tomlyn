// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace Tomlyn.Syntax
{
    public sealed class KeySyntax : ValueSyntax
    {
        private BasicValueSyntax _base;

        public KeySyntax() : base(SyntaxKind.Key)
        {
            DotKeyItems = new SyntaxList<DottedKeyItemSyntax>() { Parent = this };
        }

        public BasicValueSyntax Base
        {
            get => _base;
            set => ParentToThis(ref _base, value); // TODO: add validation for type of key (basic key or string)
        }

        public SyntaxList<DottedKeyItemSyntax> DotKeyItems { get; }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override int ChildrenCount => 2;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            if (index == 0) return Base;
            return DotKeyItems;
        }
    }
}