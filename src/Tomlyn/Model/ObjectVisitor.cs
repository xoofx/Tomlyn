// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Tomlyn.Syntax;

namespace Tomlyn.Model;

/// <summary>
/// 
/// </summary>
internal class ObjectVisitor : SyntaxVisitor
{
    private readonly TomlModelContext _context;
    private object? _currentObject;
    private AccessorBase? _currentObjectAccessor;
    private Type? _currentTargetType;
    private object? _currentValue;
    private readonly AccessorRegistry _typeRegistry;
    private readonly Stack<ObjectPath> _objectStack;

    public ObjectVisitor(object rootObject, TomlModelContext context)
    {
        _context = context;
        _objectStack = new Stack<ObjectPath>();
        _typeRegistry = new AccessorRegistry(context);
        PushObject(rootObject);
    }

    private void PushObject(object obj)
    {
        var accessor = _typeRegistry.GetAccessor(obj.GetType());
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
        var objPath = _objectStack.Pop();
        var currentObjectPath= _objectStack.Peek();
        _currentObject = currentObjectPath.Value;
        _currentObjectAccessor = currentObjectPath.Accessor;
        return _currentObject;
    }

    public override void Visit(KeyValueSyntax keyValue)
    {
        var currentValue = _currentValue;
        var currentTargetType = _currentTargetType;
        if (!TryFollowKeyPath(keyValue.Key!, keyValue.Kind, out var name))
        {
            return;
        }
        var objectAccessor = ((ObjectAccessor)_currentObjectAccessor!);
        if (objectAccessor.TryGetPropertyType(keyValue.Span, name, out _currentTargetType))
        {
            keyValue.Value!.Accept(this);
            objectAccessor = ((ObjectAccessor)_currentObjectAccessor!);
            objectAccessor.TrySetPropertyValue(keyValue.Span, _currentObject!, name, _currentValue);
        }
        _currentValue = currentValue;
        _currentTargetType = currentTargetType;
    }

    public override void Visit(TableSyntax table)
    {
        var stackCount = _objectStack.Count;
        if (!TryFollowKeyPath(table.Name!, table.Kind, out _))
        {
            return;
        }
        base.Visit(table);
        PopStack(stackCount);
    }

    public override void Visit(TableArraySyntax table)
    {
        var stackCount = _objectStack.Count;
        if (!TryFollowKeyPath(table.Name!, table.Kind, out _))
        {
            return;
        }
        var listObject = _currentObject!;
        var listAccessor = ((ListAccessor)_currentObjectAccessor!);
        var itemObject = _context.CreateInstance(listAccessor.ElementType);
        listAccessor.AddElement(listObject, itemObject);
        PushObject(itemObject);
        base.Visit(table);
        PopStack(stackCount);
    }

    private bool TryFollowKeyPath(KeySyntax key, SyntaxKind kind, out string name)
    {
        name = GetStringFromBasic(key.Key!) ?? string.Empty;
        var items = key.DotKeys;
        SyntaxNodeBase item = key;
        for (int i = 0; i < items.ChildrenCount; i++)
        {
            if (!GetOrCreateSubObject(item.Span, name, false))
            {
                return false;
            }
            var nextItem = items.GetChildren(i);
            item = nextItem!;
            name = GetStringFromBasic(nextItem!.Key!) ?? string.Empty;
        }

        var isTableArray = kind == SyntaxKind.TableArray;
        if (kind == SyntaxKind.Table || isTableArray)
        {
            if (!GetOrCreateSubObject(key.Span, name, isTableArray))
            {
                return false;
            }
        }
        return true;
    }

    private bool GetOrCreateSubObject(SourceSpan span, string key, bool createTableArrayItem)
    {
        var accessor = _currentObjectAccessor as ObjectAccessor;
        if (accessor is null)
        {
            _context.Diagnostics.Error(span, $"Unable to set a key on a list accessor {_currentObjectAccessor!.TargetType.FullName}");
            return false;
        }

        if (!accessor.TryGetPropertyValue(_currentObject!, key, out var currentObject))
        {
            currentObject = accessor.CreateAndSetDefaultPropertyValue(span, _currentObject!, key);
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
                _currentValue = dateTimeValueSyntax.Value;
                break;
            case SyntaxKind.LocalDateTime:
                _currentValue = dateTimeValueSyntax.Value;
                break;
            case SyntaxKind.LocalDate:
                _currentValue = dateTimeValueSyntax.Value;
                break;
            case SyntaxKind.LocalTime:
                _currentValue = dateTimeValueSyntax.Value;
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
        var list = _context.CreateInstance(_currentTargetType!);
        var fastList = list as IList;
        var listAccessor = (ListAccessor)_typeRegistry.GetAccessor(_currentTargetType!);

        PushObject(list);
        var items = array.Items;
        for(int i = 0; i < items.ChildrenCount; i++)
        {
            PushObject(list);
            var item = items.GetChildren(i)!;
            item.Accept(this);

            var typedValue = _typeRegistry.ConvertValue(_currentValue, listAccessor.ElementType);

            if (fastList is not null)
            {
                fastList.Add(typedValue);
            }
            else
            {
                listAccessor.AddElement(list, typedValue);
            }
        }
        PopStack(stackCount);
        _currentValue = list;
    }

    public override void Visit(InlineTableSyntax inlineTable)
    {
        var stackCount = _objectStack.Count;
        var currentObject = _context.CreateInstance(_currentTargetType!);
        PushObject(currentObject);
        base.Visit(inlineTable);
        PopStack(stackCount);
    }

    private struct ObjectPath
    {
        public ObjectPath(object value, AccessorBase accessor)
        {
            Value = value;
            Accessor = accessor;
        }

        public readonly object Value;

        public readonly AccessorBase Accessor;
    }
}