// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public sealed class StringValueSyntax : BasicValueSyntax
    {
        private SyntaxToken _token;

        public SyntaxToken Token
        {
            get => _token;
            set => ParentToThis(ref _token, value, value != null && value.Kind.IsString(), "string");
        }

        public string Value { get; set; }

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override int ChildrenCount => 1;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Token;
        }
    }
}