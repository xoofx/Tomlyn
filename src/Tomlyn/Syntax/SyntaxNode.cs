// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn.Helpers;

namespace Tomlyn.Syntax
{    
    public abstract class SyntaxNode : SyntaxNodeBase
    {
        protected SyntaxNode(SyntaxKind kind)
        {
            Kind = kind;
        }

        public SyntaxKind Kind { get; }

        public List<SyntaxTrivia> LeadingTrivia { get; set; }

        public List<SyntaxTrivia> TrailingTrivia { get; set; }

        public abstract int ChildrenCount { get; }

        public SyntaxNode GetChildren(int index)
        {
            if (index < 0) throw ThrowHelper.GetIndexNegativeArgumentOutOfRangeException();
            if (index > ChildrenCount) throw ThrowHelper.GetIndexArgumentOutOfRangeException(ChildrenCount);
            return GetChildrenImpl(index);
        }

        protected abstract SyntaxNode GetChildrenImpl(int index);

        public override string ToString()
        {
            var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }

        public void WriteTo(TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var stack = new Stack<SyntaxNode>();
            stack.Push(this);
            WriteTo(stack, writer);
        }

        private static void WriteTo(Stack<SyntaxNode> stack, TextWriter writer)
        {
            while (stack.Count > 0)
            {
                var node = stack.Pop();

                WriteTriviaTo(node.LeadingTrivia, writer);
                if (node is SyntaxToken token)
                {
                    writer.Write(token.TokenKind.ToText() ?? token.Text);
                }
                else
                {
                    int count = node.ChildrenCount;
                    for (int i = count - 1; i >= 0; i--)
                    {
                        var child = node.GetChildren(i);
                        if (child == null) continue;
                        stack.Push(child);
                    }
                }
                WriteTriviaTo(node.TrailingTrivia, writer);
            }
        }

        private static void WriteTriviaTo(List<SyntaxTrivia> trivias, TextWriter writer)
        {
            if (trivias == null) return;
            foreach (var trivia in trivias)
            {
                if (trivia == null) continue;
                writer.Write(trivia.Text);
            }
        }

        protected void ParentToThis<TSyntaxNode>(ref TSyntaxNode set, TSyntaxNode node) where TSyntaxNode : SyntaxNode
        {
            if (node?.Parent != null) throw ThrowHelper.GetExpectingNoParentException();
            if (set != null)
            {
                set.Parent = null;
            }
            if (node != null)
            {
                node.Parent = this;
            }
            set = node;
        }

        protected void ParentToThis<TSyntaxNode>(ref TSyntaxNode set, TSyntaxNode node, TokenKind expectedKind) where TSyntaxNode : SyntaxToken
        {
            ParentToThis(ref set, node, node.TokenKind == expectedKind, expectedKind);
        }

        protected void ParentToThis<TSyntaxNode, TExpected>(ref TSyntaxNode set, TSyntaxNode node, bool expectedKindSuccess, TExpected expectedMessage) where TSyntaxNode : SyntaxToken
        {
            if (node != null && !expectedKindSuccess) throw new InvalidOperationException($"Unexpected node kind `{node.TokenKind}` while expecting `{expectedMessage}`");
            ParentToThis(ref set, node);
        }

        protected void ParentToThis<TSyntaxNode>(ref TSyntaxNode set, TSyntaxNode node, TokenKind expectedKind1, TokenKind expectedKind2) where TSyntaxNode : SyntaxToken
        {
            ParentToThis(ref set, node, node.TokenKind == expectedKind1 || node.TokenKind == expectedKind2, new ExpectedTuple2<TokenKind, TokenKind>(expectedKind1, expectedKind2));
        }

        private readonly struct ExpectedTuple2<T1, T2>
        {
            public ExpectedTuple2(T1 value1, T2 value2)
            {
                Value1 = value1;
                Value2 = value2;
            }

            public readonly T1 Value1;

            public readonly T2 Value2;

            public override string ToString()
            {
                return $"`{Value1}` or `{Value2}`";
            }
        }
    }
}