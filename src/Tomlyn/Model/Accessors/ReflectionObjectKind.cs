namespace Tomlyn.Model.Accessors;

internal enum ReflectionObjectKind
{
    Primitive,
    NullablePrimitive,
    Struct,
    NullableStruct,
    Collection,
    Dictionary,
    Object
}