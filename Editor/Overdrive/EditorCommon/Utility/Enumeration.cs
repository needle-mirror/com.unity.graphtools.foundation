using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class Enumeration : IComparable, IComparable<Enumeration>, IEquatable<Enumeration>
    {
        public string Name { get; }

        public int Id { get; }

        public string[] ObsoleteNames;

        protected Enumeration(int id, string name, string[] obsoleteNames = null)
        {
            Id = id;
            Name = name;
            ObsoleteNames = obsoleteNames;
        }

        public override string ToString() => Name;

        public static IEnumerable<T> GetDeclared<T>()
            where T : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        public static IEnumerable<TBase> GetAll<T, TBase>()
            where T : TBase
            where TBase : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return fields.Select(f => f.GetValue(null)).Cast<TBase>();
        }

        public bool Equals(Enumeration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Id.Equals(other.Id)) return false;

            return other.GetType().IsInstanceOfType(this) || GetType().IsInstanceOfType(other);
        }

        public override bool Equals(object obj)
        {
            return Equals((Enumeration)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator==(Enumeration left, Enumeration right)
        {
            return Equals(left, right);
        }

        public static bool operator!=(Enumeration left, Enumeration right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(Enumeration other)
        {
            return ReferenceEquals(other, null) ? 1 : Id.CompareTo(other.Id);
        }

        public int CompareTo(object obj)
        {
            if (obj != null && !(obj is Enumeration))
                throw new ArgumentException("Object must be of type Enumeration.");

            return CompareTo((Enumeration)obj);
        }
    }
}
