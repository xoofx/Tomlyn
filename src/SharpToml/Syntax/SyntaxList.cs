// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using SharpToml.Helpers;

namespace SharpToml.Syntax
{
    public abstract class SyntaxList : SyntaxNode
    {
        protected readonly List<SyntaxNode> Children;

        internal SyntaxList() : base(SyntaxKind.List)
        {
            Children = new List<SyntaxNode>();
        }

        public sealed override int ChildrenCount => Children.Count;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Children[index];
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class SyntaxList<TSyntaxNode> : SyntaxList, IEnumerable<TSyntaxNode> where TSyntaxNode : SyntaxNode
    {
        public SyntaxList()
        {
        }

        public void AddChildren(TSyntaxNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node.Parent != null) throw ThrowHelper.GetExpectingNoParentException();
            Children.Add(node);
            node.Parent = this;
        }

        public new TSyntaxNode GetChildren(int index)
        {
            return (TSyntaxNode)base.GetChildren(index);
        }

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Children[index];
        }

        public void RemoveChildrenAt(int index)
        {
            var node = Children[index];
            Children.RemoveAt(index);
            node.Parent = null;
        }

        public void RemoveChildren(TSyntaxNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node.Parent != this) throw new InvalidOperationException("The node is not part of this list");
            Children.Remove(node);
            node.Parent = null;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(Children);
        }

        IEnumerator<TSyntaxNode> IEnumerable<TSyntaxNode>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<TSyntaxNode>
        {
            private readonly List<SyntaxNode> _nodes;
            private int _index;

            public Enumerator(List<SyntaxNode> nodes)
            {
                _nodes = nodes;
                _index = -1;
            }

            public bool MoveNext()
            {
                if (_index + 1 == _nodes.Count) return false;
                _index++;
                return true;
            }

            public void Reset()
            {
                _index = -1;
            }

            public TSyntaxNode Current
            {
                get
                {
                    if (_index < 0) throw new InvalidOperationException("MoveNext must be called before accessing Current");
                    return (TSyntaxNode)_nodes[_index];
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}