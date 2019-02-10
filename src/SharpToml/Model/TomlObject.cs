using System;
using System.Collections;
using System.Collections.Generic;
using SharpToml.Syntax;

namespace SharpToml.Model
{
    public abstract class TomlObject
    {
        protected TomlObject(ObjectKind kind)
        {
            Kind = kind;
        }

        public ObjectKind Kind { get; }

        public SyntaxNode Node { get; internal set; }

        internal static bool IsContainer(TomlObject tomlObj)
        {
            return tomlObj.Kind == ObjectKind.Array || tomlObj.Kind == ObjectKind.Table || tomlObj.Kind == ObjectKind.TableArray;
        }

        internal static object ToObject(TomlObject tomlObj)
        {
            return tomlObj is TomlValue value ? value.ValueAsObject : tomlObj;
        }

        internal static TomlObject ToTomlObject(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is TomlObject tomlObj) return tomlObj;
            var type = value.GetType();
            if (type == typeof(string)) return new TomlString((string)value);
            if (type == typeof(long)) return new TomlInteger((long) value);
            if (type == typeof(bool)) return new TomlBoolean((bool)value);
            if (type == typeof(double)) return new TomlFloat((double)value);
            if (type == typeof(DateTime)) return new TomlDateTime((DateTime) value);

            throw new InvalidOperationException($"The type `{type}` of the object is invalid. Only long, bool, double, DateTime and TomlObject are supported");
        }

        internal static TomlObject UpdateObject(TomlObject toUpdate, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is TomlObject tomlObj) return tomlObj;
            var type = value.GetType();
            if (type == typeof(string))
            {
                if (toUpdate is TomlString tomlStr)
                {
                    tomlStr.Value = (string)value;
                    return tomlStr;
                }
                // TODO: propagate trivias from previous value
                return new TomlString((string)value);
            }

            if (type == typeof(long))
            {
                if (toUpdate is TomlInteger tomlInteger)
                {
                    tomlInteger.Value = (long)value;
                    return tomlInteger;
                }
                // TODO: propagate trivias from previous value
                return new TomlInteger((long)value);
            }

            if (type == typeof(bool))
            {
                if (toUpdate is TomlBoolean tomlBoolean)
                {
                    tomlBoolean.Value = (bool)value;
                    return tomlBoolean;
                }
                // TODO: propagate trivias from previous value
                return new TomlBoolean((bool)value);
            }

            if (type == typeof(double))
            {
                if (toUpdate is TomlFloat tomlFloat)
                {
                    tomlFloat.Value = (double)value;
                    return tomlFloat;
                }
                // TODO: propagate trivias from previous value
                return new TomlFloat((double)value);
            }

            if (type == typeof(DateTime))
            {
                if (toUpdate is TomlDateTime tomlDateTime)
                {
                    tomlDateTime.Value = (DateTime)value;
                    return tomlDateTime;
                }
                // TODO: propagate trivias from previous value
                return new TomlDateTime((DateTime)value);
            }

            throw new InvalidOperationException($"The type `{type}` of the object is invalid. Only long, bool, double, DateTime and TomlObject are supported");
        }
    }

    public abstract class TomlValue : TomlObject
    {
        internal TomlValue(ObjectKind kind) : base(kind)
        {
        }

        public abstract object ValueAsObject { get; }
    }

    public abstract class TomlValue<T> : TomlValue, IEquatable<TomlValue<T>> where T : IEquatable<T>
    {
        private T _value;

        internal TomlValue(ObjectKind kind, T value) : base(kind)
        {
            _value = value;
        }

        public override object ValueAsObject => _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }

        public bool Equals(TomlValue<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TomlValue<T>) obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }

    public sealed class TomlInteger : TomlValue<long>
    {
        public TomlInteger(long value) : base(ObjectKind.Integer, value)
        {
        }
    }

    public sealed class TomlBoolean : TomlValue<bool>
    {
        public TomlBoolean(bool value) : base(ObjectKind.Boolean, value)
        {
        }
    }

    public sealed class TomlFloat : TomlValue<double>
    {
        public TomlFloat(double value) : base(ObjectKind.Float, value)
        {
        }
    }

    public sealed class TomlDateTime : TomlValue<DateTime>
    {
        public TomlDateTime(DateTime value) : base(ObjectKind.DateTime, value)
        {
        }
    }

    public sealed class TomlString : TomlValue<string>
    {
        public TomlString(string value) : base(ObjectKind.String, value)
        {
        }
    }

    public class TomlTable : TomlObject, IDictionary<string, object>
    {
        // TODO: optimize the internal by avoiding two structures
        private readonly List<KeyValuePair<string, TomlObject>> _order;
        private readonly Dictionary<string, TomlObject> _map;

        public TomlTable() : base(ObjectKind.Table)
        {
            _order = new List<KeyValuePair<string, TomlObject>>();
            _map = new Dictionary<string, TomlObject>();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var keyPair in _order)
            {
                yield return new KeyValuePair<string, object>(keyPair.Key, ToObject(keyPair.Value));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _map.Clear();
            _order.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotSupportedException();
        }

        public int Count => _map.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var toml = ToTomlObject(value);
            _map.Add(key, toml);
            _order.Add(new KeyValuePair<string, TomlObject>(key, toml));
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
            TomlObject node;
            if (_map.TryGetValue(key, out node))
            {
                value = ToObject(node);
                return true;
            }

            value = null;
            return false;
        }

        public object this[string key]
        {
            get => ToObject(_map[key]);
            set
            {
                // If the value exist already, try to create it
                if (_map.TryGetValue(key, out var node))
                {
                    node = UpdateObject(node, value);
                    _map[key] = node;
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
                    list.Add(ToObject(valuePair.Value));
                }
                return list;
            }
        }
        
        public static TomlTable From(DocumentSyntax documentSyntax)
        {
            if (documentSyntax == null) throw new ArgumentNullException(nameof(documentSyntax));
            if (documentSyntax.HasErrors) throw new InvalidOperationException($"The document has errors: {documentSyntax.Diagnostics}");
            var root = new TomlTable();
            var syntaxTransform = new SyntaxTransform(root);
            syntaxTransform.Visit(documentSyntax);
            return root;
        }
    }

    public class TomlArray : TomlObject, IList<object>
    {
        private readonly List<TomlObject> _items;

        public TomlArray() : base(ObjectKind.Array)
        {
            _items = new List<TomlObject>();
        }

        public TomlArray(int capacity) : base(ObjectKind.Array)
        {
            _items = new List<TomlObject>(capacity);
        }

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var item in _items)
            {
                yield return ToObject(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(object item)
        {
            _items.Add(ToTomlObject(item));
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(object item)
        {
            var toml = ToTomlObject(item);
            return _items.Contains(toml);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(object item)
        {
            var toml = ToTomlObject(item);
            return _items.Remove(toml);
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public int IndexOf(object item)
        {
            var toml = ToTomlObject(item);
            return _items.IndexOf(toml);
        }

        public void Insert(int index, object item)
        {
            var toml = ToTomlObject(item);
            _items.Insert(index, toml);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        public object this[int index]
        {
            get => ToObject(_items[index]);
            set => _items[index] = ToTomlObject(value);
        }
    }


    public class TomlTableArray : TomlObject, IList<TomlTable>
    {
        private readonly List<TomlTable> _items;

        public TomlTableArray() : base(ObjectKind.TableArray)
        {
            _items = new List<TomlTable>();
        }

        internal TomlTableArray(int capacity) : base(ObjectKind.TableArray)
        {
            _items = new List<TomlTable>(capacity);
        }


        public List<TomlTable>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator<TomlTable> IEnumerable<TomlTable>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TomlTable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(TomlTable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return _items.Contains(item);
        }

        public void CopyTo(TomlTable[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public bool Remove(TomlTable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return _items.Remove(item);
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public int IndexOf(TomlTable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return _items.IndexOf(item);
        }

        public void Insert(int index, TomlTable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        public TomlTable this[int index]
        {
            get => _items[index];
            set => _items[index] = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    public enum ObjectKind
    {
        Table,

        TableArray,

        Array,

        Boolean,

        String,

        Integer,

        Float,

        DateTime
    }
}