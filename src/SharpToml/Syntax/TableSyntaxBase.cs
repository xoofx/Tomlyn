namespace SharpToml.Syntax
{
    public abstract class TableSyntaxBase : TableEntrySyntax
    {
        private SyntaxToken _openBracket;
        private KeySyntax _name;
        private SyntaxToken _closeBracket;
        private SyntaxToken _endOfLineToken;

        internal TableSyntaxBase(SyntaxKind kind) : base(kind)
        {
            Items = new SyntaxList<KeyValueSyntax>() { Parent = this };
        }

        public SyntaxToken OpenBracket
        {
            get => _openBracket;
            set => ParentToThis(ref _openBracket, value, OpenTokenKind);
        }

        public KeySyntax Name
        {
            get => _name;
            set => ParentToThis(ref _name, value);
        }

        public SyntaxToken CloseBracket
        {
            get => _closeBracket;
            set => ParentToThis(ref _closeBracket, value, CloseTokenKind);
        }

        public SyntaxToken EndOfLineToken
        {
            get => _endOfLineToken;
            set => ParentToThis(ref _endOfLineToken, value, TokenKind.NewLine, TokenKind.Eof);
        }

        public SyntaxList<KeyValueSyntax> Items { get; }
        public override int ChildrenCount => 5;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal abstract TokenKind OpenTokenKind { get; }

        internal abstract TokenKind CloseTokenKind { get; }

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            switch (index)
            {
                case 0: return OpenBracket;
                case 1: return Name;
                case 2: return CloseBracket;
                case 3: return EndOfLineToken;
                default:
                    return Items;
            }
        }
    }
}