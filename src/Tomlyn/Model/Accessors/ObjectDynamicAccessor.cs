using System;
using Tomlyn.Syntax;

namespace Tomlyn.Model.Accessors;

internal abstract class ObjectDynamicAccessor : DynamicAccessor
{
    protected ObjectDynamicAccessor(DynamicModelContext context, Type targetType) : base(context, targetType)
    {
    }

    public abstract bool TryGetPropertyValue(SourceSpan span, object obj, string name, out object? value);
    public abstract bool TrySetPropertyValue(SourceSpan span, object obj, string name, object? value);
    public abstract bool TryGetPropertyType(SourceSpan span, string name, out Type? propertyType);
    public abstract bool TryCreateAndSetDefaultPropertyValue(SourceSpan span, object obj, string name, ObjectKind kind, out object? instance);
}