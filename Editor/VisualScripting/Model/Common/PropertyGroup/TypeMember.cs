using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public struct TypeMember
    {
        public List<string> Path;
        public TypeHandle Type;

        public TypeMember(TypeHandle type, List<string> path)
        {
            Type = type;
            Path = path;
        }

        public override string ToString()
        {
            return string.Join(".", Path);
        }

        public string GetId()
        {
            return ToString();
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            return Path == null ? 0 : Path.Aggregate(0, CombineHashCodes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TypeMember && obj.GetHashCode() == GetHashCode();
        }

        static int CombineHashCodes(int parentsHashCode, string memberName)
        {
            unchecked
            {
                return (parentsHashCode * 397) ^ memberName.GetHashCode();
            }
        }
    }
}
