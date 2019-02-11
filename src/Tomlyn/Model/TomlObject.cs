using System;
using Tomlyn.Syntax;

namespace Tomlyn.Model
{
    public abstract class TomlObject
    {
        protected TomlObject(ObjectKind kind)
        {
            Kind = kind;
        }

        public ObjectKind Kind { get; }

        public SyntaxNode Node { get; internal set; }

        internal static bool IsContainer(TomlObject tomlObj)
        {
            return tomlObj.Kind == ObjectKind.Array || tomlObj.Kind == ObjectKind.Table || tomlObj.Kind == ObjectKind.TableArray;
        }

        internal static object ToObject(TomlObject tomlObj)
        {
            return tomlObj is TomlValue value ? value.ValueAsObject : tomlObj;
        }

        internal static TomlObject ToTomlObject(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is TomlObject tomlObj) return tomlObj;
            var type = value.GetType();
            if (type == typeof(string)) return new TomlString((string)value);
            if (type == typeof(long)) return new TomlInteger((long) value);
            if (type == typeof(bool)) return new TomlBoolean((bool)value);
            if (type == typeof(double)) return new TomlFloat((double)value);
            if (type == typeof(DateTime)) return new TomlDateTime(ObjectKind.LocalDateTime, (DateTime) value);

            throw new InvalidOperationException($"The type `{type}` of the object is invalid. Only long, bool, double, DateTime and TomlObject are supported");
        }

        internal static TomlObject UpdateObject(TomlObject toUpdate, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is TomlObject tomlObj) return tomlObj;
            var type = value.GetType();
            if (type == typeof(string))
            {
                if (toUpdate is TomlString tomlStr)
                {
                    tomlStr.Value = (string)value;
                    return tomlStr;
                }
                // TODO: propagate trivias from previous value
                return new TomlString((string)value);
            }

            if (type == typeof(long))
            {
                if (toUpdate is TomlInteger tomlInteger)
                {
                    tomlInteger.Value = (long)value;
                    return tomlInteger;
                }
                // TODO: propagate trivias from previous value
                return new TomlInteger((long)value);
            }

            if (type == typeof(bool))
            {
                if (toUpdate is TomlBoolean tomlBoolean)
                {
                    tomlBoolean.Value = (bool)value;
                    return tomlBoolean;
                }
                // TODO: propagate trivias from previous value
                return new TomlBoolean((bool)value);
            }

            if (type == typeof(double))
            {
                if (toUpdate is TomlFloat tomlFloat)
                {
                    tomlFloat.Value = (double)value;
                    return tomlFloat;
                }
                // TODO: propagate trivias from previous value
                return new TomlFloat((double)value);
            }

            if (type == typeof(DateTime))
            {
                if (toUpdate is TomlDateTime tomlDateTime)
                {
                    tomlDateTime.Value = (DateTime)value;
                    return tomlDateTime;
                }
                // TODO: propagate trivias from previous value
                return new TomlDateTime(ObjectKind.LocalDateTime, (DateTime)value);
            }

            throw new InvalidOperationException($"The type `{type}` of the object is invalid. Only long, bool, double, DateTime and TomlObject are supported");
        }
    }
}