using System.Reflection;

namespace UnityEditor.EditorCommon.Extensions
{
    public static class FieldInfoExtensions
    {
        public static bool IsConstantOrStatic(this FieldInfo fieldInfo)
        {
            return fieldInfo.IsLiteral && !fieldInfo.IsInitOnly
                || fieldInfo.IsStatic;
        }
    }
}
