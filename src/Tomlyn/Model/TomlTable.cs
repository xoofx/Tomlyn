// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using Tomlyn.Syntax;

namespace Tomlyn.Model
{
    /// <summary>
    /// Runtime representation of a TOML table
    /// </summary>
    /// <remarks>
    /// This object keep the order of the inserted key=values
    /// </remarks>
    public sealed class TomlTable : TomlObject, IDictionary<string, object>, ITomlMetadataProvider
    {
        private readonly List<KeyValuePair<string, ValueHolder>> _order;
        private readonly Dictionary<string, ValueHolder> _map;

        /// <summary>
        /// Creates an instance of a <see cref="TomlTable"/>
        /// </summary>
        public TomlTable() : this(false)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="TomlTable"/>.
        /// </summary>
        /// <param name="inline"></param>
        public TomlTable(bool inline) : base(inline ? ObjectKind.InlineTable : ObjectKind.Table)
        {
            _order = new List<KeyValuePair<string, ValueHolder>>();
            _map = new Dictionary<string, ValueHolder>();
        }

        /// <inheritdoc/>
        public TomlPropertiesMetadata? PropertiesMetadata { get; set; }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            
            foreach (var keyPair in _order)
            {
                yield return new KeyValuePair<string, object>(keyPair.Key, keyPair.Value.Target);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _map.Clear();
            _order.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            foreach (var pair in _order)
            {
                if (pair.Key == item.Key)
                {
                    return pair.Value.Target == item.Value;
                }
            }

            return false;
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            if (arrayIndex + _order.Count > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            for (var i = 0; i < _order.Count; i++)
            {
                var item = _order[i];
                array[i + arrayIndex] = new KeyValuePair<string, object>(item.Key, item.Value.Target);
            }
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            foreach (var keyPair in _order)
            {
                if (keyPair.Key == item.Key)
                {
                    if (keyPair.Value == item.Value)
                    {
                        Remove(item.Key);
                        return true;
                    }
                }
            }

            return false;
        }

        public int Count => _map.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var valueHolder = new ValueHolder(value);
            _map.Add(key, valueHolder);
            _order.Add(new KeyValuePair<string, ValueHolder>(key, valueHolder));
        }

        public bool ContainsKey(string key)
        {
            return _map.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            if (_map.Remove(key))
            {
                for (int i = _order.Count - 1; i >= 0; i--)
                {
                    if (_order[i].Key == key)
                    {
                        _order.RemoveAt(i);
                        break;
                    }
                }
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            value = null!;
            if (_map.TryGetValue(key, out var valueHolder))
            {
                value = valueHolder.Target;
                return true;
            }

            return false;
        }

        public object this[string key]
        {
            get => _map[key].Target;
            set
            {
                // The the holder exists, update it.
                if (_map.TryGetValue(key, out var node))
                {
                    node.Target = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                var list = new List<string>();
                foreach (var valuePair in _order)
                {
                    list.Add(valuePair.Key);
                }
                return list;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                var list = new List<object>();
                foreach (var valuePair in _order)
                {
                    list.Add(valuePair.Value.Target);
                }
                return list;
            }
        }
        
        public static TomlTable From(DocumentSyntax documentSyntax)
        {
            if (documentSyntax == null) throw new ArgumentNullException(nameof(documentSyntax));
            if (documentSyntax.HasErrors) throw new InvalidOperationException($"The document has errors: {documentSyntax.Diagnostics}");
            return documentSyntax.ToModel<TomlTable>();
        }

        private class ValueHolder
        {
            public ValueHolder(object target)
            {
                Target = target;
            }

            public object Target { get; set; }
        }
    }
}