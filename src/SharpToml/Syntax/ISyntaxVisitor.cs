// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
namespace SharpToml.Syntax
{
    public interface ISyntaxVisitor
    {
        void Accept(SyntaxList table);

        void Accept(TableSyntax table);

        void Accept(DocumentSyntax document);

        void Accept(KeyValueSyntax keyValue);

        void Accept(StringValueSyntax stringValue);

        void Accept(IntegerValueSyntax integerValue);

        void Accept(BooleanValueSyntax boolValue);

        void Accept(FloatValueSyntax boolValue);

        void Accept(TableEntrySyntax tableEntry);

        void Accept(SyntaxToken token);

        void Accept(SyntaxTrivia trivia);

        void Accept(BasicKeySyntax identifier);

        void Accept(KeySyntax keySyntax);

        void Accept(DateTimeValueSyntax dateTime);

        void Accept(ArraySyntax array);

        void Accept(InlineTableItemSyntax inlineTableItem);

        void Accept(ArrayItemSyntax arrayItem);

        void Accept(DottedKeyItemSyntax dottedKeyItem);

        void Accept(InlineTableSyntax inlineTable);
    }
}