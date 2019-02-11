using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SharpToml.Model;
using SharpToml.Text;

namespace SharpToml.Syntax
{
    internal class SyntaxValidator : SyntaxVisitor
    {
        private readonly DiagnosticsBag _diagnostics;
        private ObjectPath _currentPath;
        private readonly Dictionary<ObjectPath, ObjectPathValue> _maps;
        private int _currentArrayIndex;


        public SyntaxValidator(DiagnosticsBag diagnostics)
        {
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _currentPath = new ObjectPath();
            _maps = new Dictionary<ObjectPath, ObjectPathValue>();
        }

        public override void Visit(KeyValueSyntax keyValue)
        {
            var savedPath = _currentPath.Clone();
            KeyNameToObjectPath(keyValue.Key, ObjectKind.Table);

            ObjectKind kind;
            switch (keyValue.Value)
            {
                case ArraySyntax _:
                    kind = ObjectKind.Array;
                    break;
                case BooleanValueSyntax _:
                    kind = ObjectKind.Boolean;
                    break;
                case DateTimeValueSyntax time:
                    switch (time.Kind)
                    {
                        case SyntaxKind.OffsetDateTime:
                            kind = ObjectKind.OffsetDateTime;
                            break;
                        case SyntaxKind.LocalDateTime:
                            kind = ObjectKind.LocalDateTime;
                            break;
                        case SyntaxKind.LocalDate:
                            kind = ObjectKind.LocalDate;
                            break;
                        case SyntaxKind.LocalTime:
                            kind = ObjectKind.LocalTime;
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported datetime kind `{time.Kind}` for the key-value `{keyValue}`");
                    }
                    break;
                case FloatValueSyntax _:
                    kind = ObjectKind.Float;
                    break;
                case InlineTableSyntax _:
                    kind = ObjectKind.Table;
                    break;
                case IntegerValueSyntax _:
                    kind = ObjectKind.Integer;
                    break;
                case StringValueSyntax _:
                    kind = ObjectKind.String;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported value type `{keyValue.Value}` for the key-value `{keyValue}`");
            }
            AddObjectPath(keyValue, kind, false);

            base.Visit(keyValue);
            _currentPath = savedPath;
        }

        public override void Visit(TableSyntax table)
        {
            var savedPath = _currentPath.Clone();
            KeyNameToObjectPath(table.Name, ObjectKind.Table);
            AddObjectPath(table, ObjectKind.Table, false);

            base.Visit(table);
            _currentPath = savedPath;
        }

        public override void Visit(TableArraySyntax table)
        {
            var savedPath = _currentPath.Clone();
            KeyNameToObjectPath(table.Name, ObjectKind.Table);
            var currentArrayTable = AddObjectPath(table, ObjectKind.TableArray, true);

            var savedIndex = _currentArrayIndex;
            _currentArrayIndex = currentArrayTable.ArrayIndex;

            base.Visit(table);

            currentArrayTable.ArrayIndex++;
            _currentArrayIndex = savedIndex;
            _currentPath = savedPath;
        }

        private void KeyNameToObjectPath(KeySyntax key, ObjectKind kind)
        {
            var name = GetStringFromBasic(key.Base);
            _currentPath.Add(name);

            var items = key.DotKeyItems;
            for (int i = 0; i < items.ChildrenCount; i++)
            {
                AddObjectPath(key, kind, true);
                var dotItem = GetStringFromBasic(items.GetChildren(i).Value);
                _currentPath.Add(dotItem);
            }
        }

        private ObjectPathValue AddObjectPath(SyntaxNode node, ObjectKind kind, bool isImplicit)
        {
            var currentPath = _currentPath.Clone();
            ObjectPathValue existingValue;
            if (_maps.TryGetValue(currentPath, out existingValue))
            {
                if (!((existingValue.IsImplicit || isImplicit) && (existingValue.Kind == kind || isImplicit && existingValue.Kind == ObjectKind.TableArray && kind == ObjectKind.Table)))
                {
                    _diagnostics.Error(node.Span, $"The element `{node.ToString().TrimEnd('\r','\n').ToPrintableString()}` with the key `{currentPath}` is already defined at {existingValue.Node.Span.Start} with `{existingValue.Node.ToString().TrimEnd('\r', '\n').ToPrintableString()}` and cannot be redefined");
                }
                else if (existingValue.Kind == ObjectKind.TableArray)
                {
                    _currentPath.Add(existingValue.ArrayIndex);
                }
            }
            else
            {
                existingValue = new ObjectPathValue(node, kind, isImplicit);
                _maps.Add(currentPath, existingValue);
            }
            return existingValue;
        }

        private string GetStringFromBasic(BasicValueSyntax value)
        {
            if (value is BasicKeySyntax basicKey)
            {
                return basicKey.Key.Text;
            }
            return ((StringValueSyntax) value).Value;
        }

        public override void Visit(ArraySyntax array)
        {
            var savedIndex = _currentArrayIndex;

            var items = array.Items;
            SyntaxKind firstKind = default;
            for(int i = 0; i < items.ChildrenCount; i++)
            {
                var item = items.GetChildren(i);
                var value = item.Value;
                if (i == 0)
                {
                    firstKind = value.Kind;
                }
                else if (firstKind != value.Kind)
                {
                    _diagnostics.Error(value.Span, $"The array item of type `{value.Kind.ToString().ToLowerInvariant()}` doesn't match the type of the first item: `{firstKind.ToString().ToLowerInvariant()}`");
                }
            }
            base.Visit(array);

            _currentArrayIndex = savedIndex;
        }

        public override void Visit(ArrayItemSyntax arrayItem)
        {
            _currentPath.Add(_currentArrayIndex);
            base.Visit(arrayItem);
            _currentArrayIndex++;
        }

        private class ObjectPath : List<ObjectPathItem>
        {
            private int _hashCode;

            public void Add(string key)
            {
                _hashCode = (_hashCode * 397) ^ key.GetHashCode();
                base.Add(new ObjectPathItem(key));
            }

            public void Add(int index)
            {
                _hashCode = (_hashCode * 397) ^ index;
                base.Add(new ObjectPathItem(index));
            }

            public ObjectPath Clone()
            {
                return (ObjectPath) MemberwiseClone();
            }

            public override bool Equals(object obj)
            {
                var other = (ObjectPath) obj;
                if (other.Count != Count) return false;
                if (other._hashCode != _hashCode) return false;
                for (int i = 0; i < Count; i++)
                {
                    if (this[i] != other[i]) return false;
                }
                return true;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override string ToString()
            {
                var buffer = new StringBuilder();
                for (int i = 0; i < Count; i++)
                {
                    if (i > 0) buffer.Append('.');
                    buffer.Append(this[i]);
                }
                return buffer.ToString();
            }
        }

        [DebuggerDisplay("{Node} - {Kind}")]
        private class ObjectPathValue
        {
            public ObjectPathValue(SyntaxNode node, ObjectKind kind, bool isImplicit)
            {
                Node = node;
                Kind = kind;
                IsImplicit = isImplicit;
            }


            public readonly SyntaxNode Node;

            public readonly ObjectKind Kind;

            public readonly bool IsImplicit;

            public int ArrayIndex;
        }
        
        private readonly struct ObjectPathItem : IEquatable<ObjectPathItem>
        {
            public ObjectPathItem(string key) : this()
            {
                Key = key;
            }

            public ObjectPathItem(int index) : this()
            {
                Index = index;
            }


            public readonly string Key;

            public readonly int Index;

            public bool Equals(ObjectPathItem other)
            {
                return string.Equals(Key, other.Key) && Index == other.Index;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ObjectPathItem other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Key != null ? Key.GetHashCode() : 0) * 397) ^ Index;
                }
            }

            public static bool operator ==(ObjectPathItem left, ObjectPathItem right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ObjectPathItem left, ObjectPathItem right)
            {
                return !left.Equals(right);
            }

            public override string ToString()
            {
                return Key ?? $"[{Index}]";
            }
        }
    }
}