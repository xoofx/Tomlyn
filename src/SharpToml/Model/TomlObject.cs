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
            return tomlObj is TomlValue value ? value.Value : tomlObj;
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
    }

    public abstract class TomlValue : TomlObject
    {
        internal TomlValue(ObjectKind kind) : base(kind)
        {
        }

        public abstract object Value { get; }
    }

    public abstract class TomlValue<T> : TomlValue, IEquatable<TomlValue<T>> where T : IEquatable<T>
    {
        private readonly T _value;

        internal TomlValue(ObjectKind kind, T value) : base(kind)
        {
            _value = value;
        }

        public override object Value => _value;

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
        private readonly Dictionary<string, TomlObject> _map;

        public TomlTable() : base(ObjectKind.Table)
        {
            _map = new Dictionary<string, TomlObject>();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var keyPair in _map)
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
            _map.Add(key, ToTomlObject(value));
        }

        public bool ContainsKey(string key)
        {
            return _map.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _map.Remove(key);
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
                _map[key] = ToTomlObject(value);
            }
        }

        public ICollection<string> Keys => _map.Keys;

        public ICollection<object> Values
        {
            get
            {
                var list = new List<object>();
                foreach (var value in _map.Values)
                {
                    list.Add(ToObject(value));
                }
                return list;
            }
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