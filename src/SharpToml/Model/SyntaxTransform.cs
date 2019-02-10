// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using System;
using SharpToml.Syntax;

namespace SharpToml.Model
{
    internal class SyntaxTransform : SyntaxVisitor
    {
        private readonly TomlTable _rootTable;
        private TomlTable _currentTable;
        private object _currentValue; 

        public SyntaxTransform(TomlTable rootTable)
        {
            _rootTable = rootTable ?? throw new ArgumentNullException(nameof(rootTable));
            _currentTable = _rootTable;
        }

        public override void Visit(KeyValueSyntax keyValue)
        {
            keyValue.Value.Accept(this);
            SetKeyValue(keyValue.Key, _currentValue, false);
        }

        public override void Visit(TableSyntax table)
        {
            _currentTable = _rootTable;
            var newTable = new TomlTable();
            SetKeyValue(table.Name, newTable, false);
            _currentTable = newTable;
            base.Visit(table);
        }

        public override void Visit(TableArraySyntax table)
        {
            _currentTable = _rootTable;
            _currentTable = SetKeyValue(table.Name, null, true);
            base.Visit(table);
        }

        private TomlTable SetKeyValue(KeySyntax key, object value, bool isTableArray)
        {
            var currentTable = _currentTable;
            var name = GetStringFromBasic(key.Base);
            var items = key.DotKeyItems;
            for (int i = 0; i < items.ChildrenCount; i++)
            {
                currentTable = GetTable(currentTable, name, false, false);
                name = GetStringFromBasic(items.GetChildren(i).Value);
            }

            if (isTableArray)
            {
                currentTable = GetTable(currentTable, name, true, true);
            }
            else
            {
                currentTable[name] = value;
            }

            return currentTable;
        }

        private TomlTable GetTable(TomlTable table, string key, bool isTableArray, bool createTableForArray)
        {
            if (table.TryGetValue(key, out var subTableObject))
            {
                if (subTableObject is TomlTableArray tomlArray)
                {
                    if (createTableForArray)
                    {
                        var newTableForArray = new TomlTable();
                        tomlArray.Add(newTableForArray);
                        subTableObject = newTableForArray;
                    }
                    else
                    {
                        subTableObject = tomlArray[tomlArray.Count - 1];
                    }
                }

                if (!(subTableObject is TomlTable))
                {
                    throw new InvalidOperationException($"Cannot transform the key `{key}` to a table while the existing underlying object is a `{subTableObject.GetType()}");
                }
                return (TomlTable) subTableObject;
            }

            var newTable = new TomlTable();
            table[key] = isTableArray ? (TomlObject)new TomlTableArray(1) { newTable } : newTable;
            return newTable;
        }

        private string GetStringFromBasic(BasicValueSyntax value)
        {
            if (value is BasicKeySyntax basicKey)
            {
                return basicKey.Key.Text;
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
            _currentValue = dateTimeValueSyntax.Value;
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
            var tomlArray = new TomlArray(array.ChildrenCount);
            var items = array.Items;
            for(int i = 0; i < items.ChildrenCount; i++)
            {
                var item = items.GetChildren(i);
                item.Accept(this);
                tomlArray.Add(_currentValue);
            }
            _currentValue = tomlArray;
        }

        public override void Visit(InlineTableSyntax inlineTable)
        {
            var parentTable = _currentTable;
            _currentTable = new TomlTable();
            base.Visit(inlineTable);
            _currentValue = _currentTable;
            _currentTable = parentTable;
        }
    }
}