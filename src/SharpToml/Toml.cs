using System;
using SharpToml.Parsing;
using SharpToml.Syntax;
using SharpToml.Text;

namespace SharpToml
{
    public static class Toml
    {
        public static DocumentSyntax Parse(string text, string sourcePath = null)
        {
            var textView = new StringSourceView(text, sourcePath ?? string.Empty);
            var lexer = new Lexer<StringSourceView, StringCharacterIterator>(textView, textView.SourcePath);
            var parser = new Parser<StringSourceView>(lexer);
            return parser.Run();
        }
    }
}
