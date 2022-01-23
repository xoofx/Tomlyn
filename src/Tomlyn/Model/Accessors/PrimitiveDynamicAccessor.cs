using System;

namespace Tomlyn.Model.Accessors;

internal sealed class PrimitiveDynamicAccessor : DynamicAccessor
{
    public PrimitiveDynamicAccessor(DynamicModelContext context, Type targetType, bool isNullable) : base(context, targetType)
    {
        IsNullable = isNullable;
    }

    public bool IsNullable { get; set; }
}