// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public class SyntaxToken : SyntaxNode
    {
        public TokenKind Kind { get; set; }

        public string Text { get; set; }

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);            
        }
        
        public override int ChildrenCount => 0;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return null;
        }
    }
}