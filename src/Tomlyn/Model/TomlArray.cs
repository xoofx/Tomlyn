// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Tomlyn.Model
{
    /// <summary>
    /// Runtime representation of a TOML array
    /// </summary>
    public sealed class TomlArray : TomlObject, IList<object?>
    {
        private readonly List<object?> _list;

        public TomlArray() : base(ObjectKind.Array)
        {
            _list = new List<object?>();
        }

        public TomlArray(int capacity) : base(ObjectKind.Array)
        {
            _list = new List<object?>(capacity);
        }

        public List<object?>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator<object?>  IEnumerable<object?>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(object? item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object? item)
        {
            return item != null && _list.Contains(item);
        }

        public void CopyTo(object?[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(object? item)
        {
            return item != null && _list.Remove(item);
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public int IndexOf(object? item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, object? item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public object? this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
    }
}