// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using SharpToml.Helpers;

namespace SharpToml.Syntax
{
    public abstract class SyntaxList : SyntaxNode
    {
        protected readonly List<SyntaxNode> Children;

        internal SyntaxList()
        {
            Children = new List<SyntaxNode>();
        }

        public sealed override int ChildrenCount => Children.Count;

        protected override SyntaxNode GetChildrenImpl(int index)
        {
            return Children[index];
        }

        public override void Visit(ISyntaxVisitor visitor)
        {
            visitor.Accept(this);
        }
    }

    public sealed class SyntaxList<TSyntaxNode> : SyntaxList where TSyntaxNode : SyntaxNode
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
    }
}