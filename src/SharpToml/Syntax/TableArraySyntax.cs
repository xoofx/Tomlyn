namespace SharpToml.Syntax
{
    public sealed class TableArraySyntax : TableSyntaxBase
    {
        public TableArraySyntax() : base(SyntaxKind.TableArray)
        {
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override TokenKind OpenTokenKind => TokenKind.OpenBracketDouble;

        internal override TokenKind CloseTokenKind => TokenKind.CloseBracketDouble;
    }
}