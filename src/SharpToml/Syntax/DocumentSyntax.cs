// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class DocumentSyntax : SyntaxNode
    {
        public DocumentSyntax()
        {
            Entries = new SyntaxList<TableEntrySyntax>() { Parent = this };
            Diagnostics = new DiagnosticsBag();
        }
        public DiagnosticsBag Diagnostics { get; }

        public bool HasErrors => Diagnostics.HasErrors;

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        public SyntaxList<TableEntrySyntax> Entries { get; }

        public override int ChildrenCount => 1;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Entries;
        }
    }
}