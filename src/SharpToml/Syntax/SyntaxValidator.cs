using System;

namespace SharpToml.Syntax
{
    public class SyntaxValidator : SyntaxVisitor
    {
        private readonly DiagnosticsBag _diagnostics;

        public SyntaxValidator(DiagnosticsBag diagnostics)
        {
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public override void Visit(ArraySyntax array)
        {
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
        }
    }
}