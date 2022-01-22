using System;

namespace Tomlyn.Model;

internal abstract class AccessorBase
{
    public AccessorBase(Type targetType)
    {
        TargetType = targetType;
    }
    
    public Type TargetType { get; }

}