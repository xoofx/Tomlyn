// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Tomlyn.Model;
using Tomlyn.Parsing;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn
{
    /// <summary>
    /// Main entry class to parse, validate and transform to a model a TOML document.
    /// </summary>
    public static class Toml
    {
        /// <summary>
        /// Parses a text to a TOML document.
        /// </summary>
        /// <param name="text">A string representing a TOML document</param>
        /// <param name="sourcePath">An optional path/file name to identify errors</param>
        /// <param name="options">Options for parsing. Default is parse and validate.</param>
        /// <returns>A parsed TOML document</returns>
        public static DocumentSyntax Parse(string text, string? sourcePath = null, TomlParserOptions options = TomlParserOptions.ParseAndValidate)
        {
            var textView = new StringSourceView(text, sourcePath ?? string.Empty);
            var lexer = new Lexer<StringSourceView, StringCharacterIterator>(textView);
            var parser = new Parser<StringSourceView>(lexer);
            var doc = parser.Run();
            if (!doc.HasErrors && options == TomlParserOptions.ParseAndValidate)
            {
                Validate(doc);
            }
            return doc;
        }

        /// <summary>
        /// Parses a UTF8 byte array to a TOML document.
        /// </summary>
        /// <param name="utf8Bytes">A UTF8 string representing a TOML document</param>
        /// <param name="sourcePath">An optional path/file name to identify errors</param>
        /// <param name="options">Options for parsing. Default is parse and validate.</param>
        /// <returns>A parsed TOML document</returns>
        public static DocumentSyntax Parse(byte[] utf8Bytes, string? sourcePath = null, TomlParserOptions options = TomlParserOptions.ParseAndValidate)
        {
            var textView = new StringUtf8SourceView(utf8Bytes, sourcePath ?? string.Empty);
            var lexer = new Lexer<StringUtf8SourceView, StringCharacterUtf8Iterator>(textView);
            var parser = new Parser<StringUtf8SourceView>(lexer);
            var doc = parser.Run();
            if (!doc.HasErrors && options == TomlParserOptions.ParseAndValidate)
            {
                Validate(doc);
            }
            return doc;
        }

        /// <summary>
        /// Converts a <see cref="DocumentSyntax"/> to a <see cref="TomlTable"/>
        /// </summary>
        /// <param name="syntax">A TOML document</param>
        /// <returns>A <see cref="TomlTable"/>, a runtime representation of the TOML document</returns>
        public static TomlTable ToModel(this DocumentSyntax syntax)
        {
            if (syntax == null) throw new ArgumentNullException(nameof(syntax));
            return TomlTable.From(syntax);
        }

        public static T ToModel<T>(this DocumentSyntax syntax, TomlModelOptions? options = null) where T : class, new()
        {
            if (!TryToModel<T>(syntax, out var data, out var diagnostics, options))
            {
                // Will be fixed with ReturnWhen false
                throw new TomlException(diagnostics!);
            }
            return data;
        }

        public static bool TryToModel<T>(this DocumentSyntax syntax, out T data, out DiagnosticsBag? diagnostics, TomlModelOptions? options = null) where T : class, new()
        {
            data = new T();
            var context = new DynamicModelContext(options ?? new TomlModelOptions());
            SyntaxTransform visitor = new SyntaxTransform(context, data);
            visitor.Visit(syntax);
            if (context.Diagnostics.HasErrors)
            {
                diagnostics = context.Diagnostics;
                return false;
            }

            diagnostics = null;
            return true;
        }

        /// <summary>
        /// Validates the specified TOML document.
        /// </summary>
        /// <param name="doc">The TOML document to validate</param>
        /// <returns>The same instance as the parameter. Check <see cref="DocumentSyntax.HasErrors"/> and <see cref="DocumentSyntax.Diagnostics"/> for details.</returns>
        public static DocumentSyntax Validate(DocumentSyntax doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (doc.HasErrors) return doc;
            var validator = new SyntaxValidator(doc.Diagnostics);
            validator.Visit(doc);
            return doc;
        }
    }
}
