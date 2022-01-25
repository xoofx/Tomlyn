// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Tomlyn.Syntax
{
    /// <summary>
    /// Extensions for <see cref="SyntaxNode"/>.
    /// </summary>
    public static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Get all <see cref="SyntaxToken"/> and <see cref="SyntaxTrivia"/> in Depth-First-Search order of the node.
        /// </summary>
        /// <param name="node">The node to collect all tokens from</param>
        /// <param name="includeCommentsAndWhitespaces"><c>true</c> to include comments and whitespaces.</param>
        /// <returns>All descendants in Depth-First-Search order of the node.</returns>
        public static IEnumerable<SyntaxNodeBase> Tokens(this SyntaxNode node, bool includeCommentsAndWhitespaces = true)
        {
            foreach (var token in Descendants(node, true))
            {
                if (token is SyntaxTrivia)
                {
                    if (!includeCommentsAndWhitespaces) continue;
                }
                else if (token is not SyntaxToken) continue;
                
                yield return token;
            }
        }

        /// <summary>
        /// Get all descendants in Depth-First-Search order of the node. Note that this method returns the node itself (last).
        /// </summary>
        /// <param name="node">The node to collect all descendants</param>
        /// <param name="includeTokensCommentsAndWhitespaces"><c>true</c> to include tokens, comments and whitespaces.</param>
        /// <returns>All descendants in Depth-First-Search order of the node.</returns>
        public static IEnumerable<SyntaxNodeBase> Descendants(this SyntaxNode node, bool includeTokensCommentsAndWhitespaces = false)
        {
            if (!includeTokensCommentsAndWhitespaces && node is SyntaxToken) yield break;

            if (includeTokensCommentsAndWhitespaces && node.LeadingTrivia is not null)
            {
                foreach (var syntaxTrivia in node.LeadingTrivia)
                {
                    yield return syntaxTrivia;
                }
            }

            var childrenCount = node.ChildrenCount;
            for (int i = 0; i < childrenCount; i++)
            {
                var child = node.GetChild(i);

                // Skip syntax list
                if (child is SyntaxList list)
                {
                    var subChildrenCount = list.ChildrenCount;
                    for (int j = 0; j < subChildrenCount; j++)
                    {
                        var subChild = list.GetChild(j);
                        if (subChild is not null)
                        {
                            foreach (var sub in Descendants(subChild, includeTokensCommentsAndWhitespaces))
                            {
                                yield return sub;
                            }
                        }
                    }
                }
                else if (child is not null)
                {
                    foreach (var sub in Descendants(child, includeTokensCommentsAndWhitespaces))
                    {
                        yield return sub;
                    }
                }
            }

            yield return node;

            if (includeTokensCommentsAndWhitespaces && node.TrailingTrivia is not null)
            {
                foreach (var syntaxTrivia in node.TrailingTrivia)
                {
                    yield return syntaxTrivia;
                }
            }
        }

        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, int value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            list.Add(new KeyValueSyntax(name, new IntegerValueSyntax(value)));
        }

        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, long value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            list.Add(new KeyValueSyntax(name, new IntegerValueSyntax(value)));
        }

        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, bool value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            list.Add(new KeyValueSyntax(name, new BooleanValueSyntax(value)));
        }

        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, double value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            list.Add(new KeyValueSyntax(name, new FloatValueSyntax(value)));
        }

        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, string value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            list.Add(new KeyValueSyntax(name, new StringValueSyntax(value)));
        }

        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, int[] values)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            list.Add(new KeyValueSyntax(name, new ArraySyntax(values)));
        }
        
        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, string[] values)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            list.Add(new KeyValueSyntax(name, new ArraySyntax(values)));
        }

        public static void Add(this SyntaxList<KeyValueSyntax> list, string name, DateTimeValueSyntax value)
		{
			if (list == null) throw new ArgumentNullException(nameof(list));
			list.Add(new KeyValueSyntax(name, value));
		}

        public static KeyValueSyntax AddTrailingComment(this KeyValueSyntax keyValue, string comment)
        {
            if (keyValue == null) throw new ArgumentNullException(nameof(keyValue));
            if (keyValue.Value == null) throw new InvalidOperationException("The Value must not be null on the KeyValueSyntax");
            keyValue.Value.AddTrailingWhitespace().AddTrailingComment(comment);
            return keyValue;
        }

        public static T AddLeadingWhitespace<T>(this T node) where T : SyntaxNode
        {
            return AddLeadingTrivia(node, SyntaxFactory.Whitespace());
        }

        public static T AddTrailingWhitespace<T>(this T node) where T : SyntaxNode
        {
            return AddTrailingTrivia(node, SyntaxFactory.Whitespace());
        }

        public static T AddLeadingTrivia<T>(this T node, SyntaxTrivia trivia) where T : SyntaxNode
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var trivias = node.LeadingTrivia;
            if (trivias == null)
            {
                trivias = new List<SyntaxTrivia>();
                node.LeadingTrivia = trivias;
            }
            trivias.Add(trivia);
            return node;
        }

        public static T AddTrailingTrivia<T>(this T node, SyntaxTrivia trivia) where T : SyntaxNode
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var trivias = node.TrailingTrivia;
            if (trivias == null)
            {
                trivias = new List<SyntaxTrivia>();
                node.TrailingTrivia = trivias;
            }
            trivias.Add(trivia);
            return node;
        }

        public static T AddLeadingComment<T>(this T node, string comment) where T : SyntaxNode
        {
            return AddLeadingTrivia(node, SyntaxFactory.Comment(comment));
        }
        
        public static T AddTrailingComment<T>(this T node, string comment) where T : SyntaxNode
        {
            return AddTrailingTrivia(node, SyntaxFactory.Comment(comment));
        }

        public static T AddLeadingTriviaNewLine<T>(this T node) where T : SyntaxNode
        {
            return AddLeadingTrivia(node, SyntaxFactory.NewLineTrivia());
        }

        public static T AddTrailingTriviaNewLine<T>(this T node) where T : SyntaxNode
        {
            return AddTrailingTrivia(node, SyntaxFactory.NewLineTrivia());
        }
    }
}
