namespace SharpToml.Syntax
{
    public sealed class TableArraySyntax : TableSyntaxBase
    {
        public TableArraySyntax() : base(SyntaxKind.TableArray)
        {
        }

        internal override TokenKind OpenTokenKind => TokenKind.OpenBracketDouble;

        internal override TokenKind CloseTokenKind => TokenKind.CloseBracketDouble;
    }
}