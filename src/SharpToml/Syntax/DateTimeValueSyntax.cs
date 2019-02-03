// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;

namespace SharpToml.Syntax
{
    public sealed class DateTimeValueSyntax : ValueSyntax
    {
        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        public SyntaxToken Token { get; set; }

        public DateTime Value { get; set; }

        public override int ChildrenCount => 1;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Token;
        }
    }
}