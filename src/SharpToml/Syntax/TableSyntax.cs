// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class TableSyntax : TableEntrySyntax
    {
        private SyntaxToken _openBracket;
        private KeySyntax _name;
        private SyntaxToken _closeBracket;
        private SyntaxToken _endOfLineToken;

        public TableSyntax()
        {
            KeyValues = new SyntaxList<KeyValueSyntax>() { Parent = this };
        }

        public SyntaxToken OpenBracket
        {
            get => _openBracket;
            set => ParentToThis(ref _openBracket, value, TokenKind.OpenBracket, TokenKind.OpenBracketDouble);
        }

        public KeySyntax Name
        {
            get => _name;
            set => ParentToThis(ref _name, value);
        }

        public SyntaxToken CloseBracket
        {
            get => _closeBracket;
            set => ParentToThis(ref _closeBracket, value, TokenKind.CloseBracket, TokenKind.CloseBracketDouble);
        }

        public SyntaxToken EndOfLineToken
        {
            get => _endOfLineToken;
            set => ParentToThis(ref _endOfLineToken, value, TokenKind.NewLine, TokenKind.Eof);
        }

        public SyntaxList<KeyValueSyntax> KeyValues { get; }

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override int ChildrenCount => 5;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            switch (index)
            {
                case 0: return OpenBracket;
                case 1: return Name;
                case 2: return CloseBracket;
                case 3: return EndOfLineToken;
                default:
                    return KeyValues;
            }
        }
    }
}