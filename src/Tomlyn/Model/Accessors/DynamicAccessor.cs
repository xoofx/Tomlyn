using System;

namespace Tomlyn.Model.Accessors;

internal abstract class DynamicAccessor
{
    protected DynamicAccessor(DynamicModelContext context, Type targetType)
    {
        Context = context;
        TargetType = targetType;
    }

    public DynamicModelContext Context { get; }
    
    public Type TargetType { get; }

    public override string ToString()
    {
        return $"{GetType().Name} Type: {TargetType.FullName}";
    }
}