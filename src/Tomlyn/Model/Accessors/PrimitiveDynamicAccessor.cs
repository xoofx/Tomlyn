using System;

namespace Tomlyn.Model.Accessors;

internal sealed class PrimitiveDynamicAccessor : DynamicAccessor
{
    public PrimitiveDynamicAccessor(DynamicModelReadContext context, Type targetType, bool isNullable) : base(context, targetType, ReflectionObjectKind.Primitive)
    {
        IsNullable = isNullable;
    }

    public bool IsNullable { get; set; }
}