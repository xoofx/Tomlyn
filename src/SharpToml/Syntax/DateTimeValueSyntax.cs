// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;

namespace SharpToml.Syntax
{
    public sealed class DateTimeValueSyntax : ValueSyntax
    {
        public DateTimeValueSyntax(SyntaxKind kind) : base(CheckDateTimeKind(kind))
        {
        }

        public SyntaxToken Token { get; set; }

        public DateTime Value { get; set; }

        public override int ChildrenCount => 1;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Token;
        }

        private static SyntaxKind CheckDateTimeKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.OffsetDateTime:
                case SyntaxKind.LocalDateTime:
                case SyntaxKind.LocalDate:
                case SyntaxKind.LocalTime:
                    return kind;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}