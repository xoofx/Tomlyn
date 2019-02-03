// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;

namespace SharpToml.Syntax
{
    public sealed class DocumentSyntax : SyntaxNode
    {
        public DocumentSyntax()
        {
            Entries = new SyntaxList<TableEntrySyntax>() { Parent = this };
            Messages = new List<SyntaxMessage>();
        }
        public List<SyntaxMessage> Messages { get; }

        public bool HasErrors { get; set; }

        public void ClearMessages()
        {
            Messages.Clear();
            HasErrors = false;
        }

        public void LogMessage(SyntaxMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Messages.Add(message);
            if (message.Kind == SyntaxMessageKind.Error)
            {
                HasErrors = true;
            }
        }

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