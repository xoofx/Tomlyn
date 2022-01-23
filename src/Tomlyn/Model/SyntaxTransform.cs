// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Tomlyn.Model.Accessors;
using Tomlyn.Syntax;

namespace Tomlyn.Model;

/// <summary>
/// Transform syntax to a model.
/// </summary>
internal class SyntaxTransform : SyntaxVisitor
{
    private readonly DynamicModelContext _context;
    private object? _currentObject;
    private DynamicAccessor? _currentObjectAccessor;
    private Type? _currentTargetType;
    private object? _currentValue;
    private readonly Stack<ObjectPath> _objectStack;
    private readonly HashSet<object> _tableArrays;

    public SyntaxTransform(DynamicModelContext context, object rootObject)
    {
        _context = context;
        _objectStack = new Stack<ObjectPath>();
        _tableArrays = new HashSet<object>(ReferenceEqualityComparer.Instance);
        PushObject(rootObject);
    }

    private void PushObject(object obj)
    {
        var accessor = _context.GetAccessor(obj.GetType());
        _objectStack.Push(new ObjectPath(obj, accessor));
        _currentObject = obj;
        _currentObjectAccessor = accessor;
    }

    private void PopStack(int targetCount)
    {
        while (_objectStack.Count > targetCount)
        {
            PopObject();
        }
    }

    private object PopObject()
    {
        _ = _objectStack.Pop();
        var currentObjectPath= _objectStack.Peek();
        _currentObject = currentObjectPath.Value;
        _currentObjectAccessor = currentObjectPath.DynamicAccessor;
        return _currentObject;
    }

    public override void Visit(KeyValueSyntax keyValue)
    {
        var stackCount = _objectStack.Count;
        try
        {
            var currentValue = _currentValue;
            var currentTargetType = _currentTargetType;
            if (!TryFollowKeyPath(keyValue.Key!, keyValue.Kind, out var name))
            {
                return;
            }

            var objectAccessor = ((ObjectDynamicAccessor)_currentObjectAccessor!);
            if (objectAccessor.TryGetPropertyType(keyValue.Span, name, out _currentTargetType))
            {
                keyValue.Value!.Accept(this);
                objectAccessor = ((ObjectDynamicAccessor)_currentObjectAccessor!);
                objectAccessor.TrySetPropertyValue(keyValue.Span, _currentObject!, name, _currentValue);
            }

            _currentValue = currentValue;
            _currentTargetType = currentTargetType;
        }
        finally
        {
            PopStack(stackCount);
        }
    }

    public override void Visit(TableSyntax table)
    {
        var stackCount = _objectStack.Count;
        try
        {
            if (!TryFollowKeyPath(table.Name!, table.Kind, out _))
            {
                return;
            }

            base.Visit(table);
        }
        finally
        {
            PopStack(stackCount);
        }
    }

    public override void Visit(TableArraySyntax table)
    {
        var stackCount = _objectStack.Count;
        try
        {
            if (!TryFollowKeyPath(table.Name!, table.Kind, out _))
            {
                return;
            }

            base.Visit(table);
        }
        finally
        {

            PopStack(stackCount);
        }
    }

    private bool TryFollowKeyPath(KeySyntax key, SyntaxKind kind, out string name)
    {
        name = GetStringFromBasic(key.Key!) ?? string.Empty;
        var items = key.DotKeys;
        SyntaxNode item = key;
        for (int i = 0; i < items.ChildrenCount; i++)
        {
            if (!GetOrCreateSubObject(item.Span, name, ObjectKind.Table))
            {
                return false;
            }
            var nextItem = items.GetChildren(i);
            item = nextItem!;
            name = GetStringFromBasic(nextItem!.Key!) ?? string.Empty;
        }

        if (kind == SyntaxKind.Table || kind == SyntaxKind.TableArray)
        {
            if (!GetOrCreateSubObject(key.Span, name, GetKindFromSyntaxKind(kind)))
            {
                return false;
            }
        }
        return true;
    }

    private static ObjectKind GetKindFromSyntaxKind(SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.Array:
                return ObjectKind.Array;
            case SyntaxKind.InlineTable:
                return ObjectKind.InlineTable;
            case SyntaxKind.Table:
                return ObjectKind.Table;
            case SyntaxKind.TableArray:
                return ObjectKind.TableArray;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, $"Invalid {kind}");
        }
    }

    private bool GetOrCreateSubObject(SourceSpan span, string key, ObjectKind kind)
    {
        var accessor = _currentObjectAccessor as ObjectDynamicAccessor;
        if (accessor is null)
        {
            _context.Diagnostics.Error(span, $"Unable to set a key on an object accessor {_currentObjectAccessor!.TargetType.FullName}");
            return false;
        }

        if (!accessor.TryGetPropertyValue(span, _currentObject!, key, out var currentObject))
        {
            if (!accessor.TryCreateAndSetDefaultPropertyValue(span, _currentObject!, key, kind, out currentObject))
            {
                return false;
            }
        }

        if (currentObject is not null)
        {
            if (kind == ObjectKind.TableArray)
            {
                _tableArrays.Add(currentObject);
            }

            // Special handling of table arrays
            if (_tableArrays.Contains(currentObject))
            {
                var listAccessor = (ListDynamicAccessor)_context.GetAccessor(currentObject.GetType());
                if (kind == ObjectKind.TableArray)
                {
                    var instance = _context.CreateInstance(listAccessor.ElementType, ObjectKind.Table);
                    listAccessor.AddElement(currentObject, instance);
                    currentObject = instance;
                }
                else
                {
                    currentObject = listAccessor.GetLastElement(currentObject);
                }
            }
        }

        if (currentObject is not null)
        {
            PushObject(currentObject);
            return true;
        }

        return false;
    }

    private string? GetStringFromBasic(BareKeyOrStringValueSyntax value)
    {
        if (value is BareKeySyntax basicKey)
        {
            return basicKey.Key?.Text;
        }
        return ((StringValueSyntax) value).Value;
    }

    public override void Visit(BooleanValueSyntax boolValue)
    {
        _currentValue = boolValue.Value;
    }

    public override void Visit(StringValueSyntax stringValue)
    {
        _currentValue = stringValue.Value;
    }

    public override void Visit(DateTimeValueSyntax dateTimeValueSyntax)
    {
        switch (dateTimeValueSyntax.Kind)
        {
            case SyntaxKind.OffsetDateTime:
                _currentValue = new TomlDateTime(ObjectKind.OffsetDateTime, dateTimeValueSyntax.Value);
                break;
            case SyntaxKind.LocalDateTime:
                _currentValue = new TomlDateTime(ObjectKind.LocalDateTime, dateTimeValueSyntax.Value);
                break;
            case SyntaxKind.LocalDate:
                _currentValue = new TomlDateTime(ObjectKind.LocalDate, dateTimeValueSyntax.Value);
                break;
            case SyntaxKind.LocalTime:
                _currentValue = new TomlDateTime(ObjectKind.LocalTime, dateTimeValueSyntax.Value);
                break;
        }
    }

    public override void Visit(FloatValueSyntax floatValueSyntax)
    {
        _currentValue = floatValueSyntax.Value;
    }

    public override void Visit(IntegerValueSyntax integerValueSyntax)
    {
        _currentValue = integerValueSyntax.Value;
    }

    public override void Visit(ArraySyntax array)
    {
        var stackCount = _objectStack.Count;
        try
        {
            var list = _context.CreateInstance(_currentTargetType!, ObjectKind.Array);
            var fastList = list as IList;
            var accessor = _context.GetAccessor(list.GetType());
            var listAccessor = accessor as ListDynamicAccessor;

            // Fail if we don't have a list accessor
            if (listAccessor is null)
            {
                _context.Diagnostics.Error(array.Span, $"Invalid list type {list.GetType().FullName}. Getting a {accessor} instead.");
                return;
            }

            var items = array.Items;
            for (int i = 0; i < items.ChildrenCount; i++)
            {
                var item = items.GetChildren(i)!;
                item.Accept(this);

                // Make sure that we can convert the item to the destination value
                if (!_context.TryConvertValue(item.Span, _currentValue, listAccessor.ElementType, out var itemValue))
                {
                    continue;
                }

                if (fastList is not null)
                {
                    fastList.Add(itemValue);
                }
                else
                {
                    listAccessor.AddElement(list, itemValue);
                }
            }
            _currentValue = list;
        }
        finally
        {
            PopStack(stackCount);
        }
    }

    public override void Visit(InlineTableSyntax inlineTable)
    {
        var stackCount = _objectStack.Count;
        var currentObject = _context.CreateInstance(_currentTargetType!, ObjectKind.InlineTable);
        PushObject(currentObject);
        base.Visit(inlineTable);
        PopStack(stackCount);
        _currentValue = currentObject;
    }

    private struct ObjectPath
    {
        public ObjectPath(object value, DynamicAccessor dynamicAccessor)
        {
            Value = value;
            DynamicAccessor = dynamicAccessor;
        }

        public readonly object Value;

        public readonly DynamicAccessor DynamicAccessor;
    }

    private class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        private ReferenceEqualityComparer()
        {
        }

#pragma warning disable CS0108, CS0114
        public bool Equals(object x, object y)
#pragma warning restore CS0108, CS0114
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}