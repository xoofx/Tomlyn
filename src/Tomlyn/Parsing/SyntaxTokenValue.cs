// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using System;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Parsing
{
    /// <summary>
    /// A lightweight token struct to avoid GC allocations.
    /// </summary>
    public readonly struct SyntaxTokenValue : IEquatable<SyntaxTokenValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxTokenValue"/> struct.
        /// </summary>
        /// <param name="kind">The type.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="value">Optional parse value of the token</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public SyntaxTokenValue(TokenKind kind, TextPosition start, TextPosition end, object? value = null)
        {
            if (start.Offset > end.Offset) throw new ArgumentOutOfRangeException(nameof(start), $"[{nameof(start)}] index must be <= to [{nameof(end)}]");
            Kind = kind;
            Start = start;
            End = end;
            Value = value;
        }

        /// <summary>
        /// The type of token.
        /// </summary>
        public readonly TokenKind Kind;

        /// <summary>
        /// The start position of this token.
        /// </summary>
        public readonly TextPosition Start;

        /// <summary>
        /// The end position of this token.
        /// </summary>
        public readonly TextPosition End;

        /// <summary>
        /// The parsed value
        /// </summary>
        public readonly object? Value;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Kind}({Start}:{End})";
        }

        /// <summary>
        /// Gets the text represented by this token from a source string.
        /// </summary>
        /// <param name="text">The full source text.</param>
        /// <returns>The token text, or <c>null</c> if out of range.</returns>
        public string? GetText(string text)
        {
            if (Kind == TokenKind.Eof)
            {
                return "<eof>";
            }
            return End.Offset < text.Length ? text.Substring(Start.Offset, End.Offset - Start.Offset + 1) : null;
        }

        internal string? GetText<TTextView>(TTextView text) where TTextView : IStringView
        {
            if (Kind == TokenKind.Eof)
            {
                return "<eof>";
            }
            return text.GetString(Start.Offset, End.Offset - Start.Offset + 1);
        }

        /// <inheritdoc />
        public bool Equals(SyntaxTokenValue other)
        {
            return Kind == other.Kind && Start.Equals(other.Start) && End.Equals(other.End);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SyntaxTokenValue && Equals((SyntaxTokenValue) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Kind;
                hashCode = (hashCode*397) ^ Start.GetHashCode();
                hashCode = (hashCode*397) ^ End.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Compares two <see cref="SyntaxTokenValue"/> instances for equality.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><c>true</c> if the values are equal; otherwise <c>false</c>.</returns>
        public static bool operator ==(SyntaxTokenValue left, SyntaxTokenValue right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="SyntaxTokenValue"/> instances for inequality.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><c>true</c> if the values are not equal; otherwise <c>false</c>.</returns>
        public static bool operator !=(SyntaxTokenValue left, SyntaxTokenValue right)
        {
            return !left.Equals(right);
        }
    }
}