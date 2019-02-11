// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace Tomlyn.Syntax
{
    public sealed class DocumentSyntax : SyntaxNode
    {
        public DocumentSyntax() : base(SyntaxKind.Document)
        {
            KeyValues = new SyntaxList<KeyValueSyntax>() {Parent = this};
            Tables = new SyntaxList<TableSyntaxBase>() { Parent = this };
            Diagnostics = new DiagnosticsBag();
        }
        public DiagnosticsBag Diagnostics { get; }

        public bool HasErrors => Diagnostics.HasErrors;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        public SyntaxList<KeyValueSyntax> KeyValues { get; }


        public SyntaxList<TableSyntaxBase> Tables { get; }

        public override int ChildrenCount => 2;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return index == 0 ? (SyntaxNode)KeyValues : Tables;
        }
    }
}