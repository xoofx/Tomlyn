// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System.Globalization;

namespace Tomlyn.Syntax
{
    /// <summary>
    /// A float TOML value syntax node.
    /// </summary>
    public sealed class FloatValueSyntax : ValueSyntax
    {
        private SyntaxToken _token;

        /// <summary>
        /// Creates an instance of <see cref="FloatValueSyntax"/>
        /// </summary>
        public FloatValueSyntax() : base(SyntaxKind.Float)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="FloatValueSyntax"/>
        /// </summary>
        /// <param name="value">The double value</param>
        public FloatValueSyntax(double value) : this()
        {
            if (double.IsNaN(value))
            {
                Token = new SyntaxToken(TokenKind.Nan, TokenKind.Nan.ToText());
            }
            else if (double.IsPositiveInfinity(value))
            {
                Token = new SyntaxToken(TokenKind.PositiveInfinite, TokenKind.PositiveInfinite.ToText());
            }
            else if (double.IsNegativeInfinity(value))
            {
                Token = new SyntaxToken(TokenKind.NegativeInfinite, TokenKind.NegativeInfinite.ToText());
            }
            else
            {
                Token = new SyntaxToken(TokenKind.Float, value.ToString("G16", CultureInfo.InvariantCulture));
            }
            Value = value;
        }

        /// <summary>
        /// The token storing the float value.
        /// </summary>
        public SyntaxToken Token
        {
            get => _token;
            set => ParentToThis(ref _token, value, value != null && value.TokenKind.IsFloat(), TokenKind.Float);
        }

        /// <summary>
        /// The parsed value of the <see cref="Token"/>
        /// </summary>
        public double Value { get; set; }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override int ChildrenCount => 1;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Token;
        }
    }
}