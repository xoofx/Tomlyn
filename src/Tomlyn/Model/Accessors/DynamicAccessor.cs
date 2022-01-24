using System;

namespace Tomlyn.Model.Accessors;

internal abstract class DynamicAccessor
{
    protected DynamicAccessor(DynamicModelReadContext context, Type targetType)
    {
        Context = context;
        TargetType = targetType;
    }

    public DynamicModelReadContext Context { get; }
    
    public Type TargetType { get; }

    public override string ToString()
    {
        return $"{GetType().Name} Type: {TargetType.FullName}";
    }
}