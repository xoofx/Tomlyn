// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class KeyValueSyntax : TableEntrySyntax
    {
        private KeySyntax _key;
        private SyntaxToken _equalToken;
        private ValueSyntax _value;
        private SyntaxToken _endOfLineToken;

        public KeySyntax Key
        {
            get => _key;
            set => ParentToThis(ref _key, value);
        }

        public SyntaxToken EqualToken
        {
            get => _equalToken;
            set => ParentToThis(ref _equalToken, value, TokenKind.Equal);
        }

        public ValueSyntax Value
        {
            get => _value;
            set => ParentToThis(ref _value, value);
        }

        public SyntaxToken EndOfLineToken
        {
            get => _endOfLineToken;
            set => ParentToThis(ref _endOfLineToken, value, TokenKind.NewLine);
        }

        public override int ChildrenCount => 4;

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            switch (index)
            {
                case 0:
                    return Key;
                case 1:
                    return EqualToken;
                case 2:
                    return Value;
                default:
                    return EndOfLineToken;
            }
        }
    }
}