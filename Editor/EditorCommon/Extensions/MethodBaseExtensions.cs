using System;
using System.Reflection;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.EditorCommon.Extensions
{
    struct MethodDetails
    {
        internal string ClassName { get; }
        internal string MethodName { get; }
        internal string Details { get; }

        internal MethodDetails(string className, string methodName, string details)
        {
            ClassName = className;
            MethodName = methodName;
            Details = details;
        }
    }

    static class MethodBaseExtensions
    {
        internal static MethodDetails GetMethodDetails(this MethodBase methodBase)
        {
            string menuName = VseUtility.GetTitle(methodBase);
            string detailsPostFix = " (";
            string postFix = "";
            bool comma = false;

            foreach (ParameterInfo parameterInfo in methodBase.GetParameters())
            {
                if (comma)
                {
                    detailsPostFix += ", ";
                    postFix += ", ";
                }

                detailsPostFix += parameterInfo.ParameterType.FriendlyName() + " " + parameterInfo.Name;
                postFix += parameterInfo.ParameterType.FriendlyName();
                comma = true;
            }

            detailsPostFix += ")";

            MethodInfo methodInfo = methodBase as MethodInfo;
            if (methodInfo != null && methodInfo.ReturnType != typeof(void))
            {
                detailsPostFix += " => " + methodInfo.ReturnType.FriendlyName();
            }

            string className = methodBase.DeclaringType.FriendlyName(false).Nicify();
            string methodName = $"{menuName} ({postFix})";
            string details = methodBase.IsConstructor
                ? $"Create {className}"
                : (methodBase.IsStatic ? "Static " : "") + className + "." + menuName + detailsPostFix;

            return new MethodDetails(className, methodName, details);
        }

        internal static Type GetReturnType(this MethodBase methodBase)
        {
            var methodInfo = methodBase as MethodInfo;

            if (methodInfo != null)
                return methodInfo.ReturnType;

            if (methodBase.IsConstructor)
                return methodBase.DeclaringType;

            throw new InvalidOperationException();
        }
    }
}
