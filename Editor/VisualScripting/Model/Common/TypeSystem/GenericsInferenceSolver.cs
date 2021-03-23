using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Mode
{
    public static class GenericsInferenceSolver
    {
        public static bool SolveTypeArguments(Stencil stencil, MethodBase methodBase, ref Type[] genericTypes, ref TypeHandle[] typeReferences, Type otherConnectedType, int index)
        {
            Assert.IsNotNull(methodBase.DeclaringType);

            Type genericType;
            if (methodBase.IsGenericMethod) // F<T> => T
                genericType = methodBase.GetGenericArguments()[0];
            else // class C<T> { F() } => T
            {
                Assert.IsTrue(methodBase.DeclaringType.ContainsGenericParameters);

                genericType = methodBase.DeclaringType.GetGenericArguments()[0];
            }

            if (index < 0) // connected port: method instance ie. this
            {
                genericTypes = new[] { genericType };
                Type inferredInstanceType = ReflectType(otherConnectedType, methodBase.DeclaringType, genericType);
                typeReferences[0] = inferredInstanceType.GenerateTypeHandle(stencil);
                return true;
            }

            ParameterInfo[] parameterInfos = methodBase.GetParameters();
            ParameterInfo parameter = parameterInfos[index];

            // we just connected the int i of F<T>(T t, int i), ie. not interesting
            if (!parameter.ParameterType.ContainsGenericParameters)
            {
                return false;
            }

            genericTypes = new[] { genericType };

            if (otherConnectedType == null)
                return true;

            Type inferredPortType = ReflectType(otherConnectedType, parameter.ParameterType, genericType);
            typeReferences[0] = inferredPortType.GenerateTypeHandle(stencil);
            return true;
        }

        [CanBeNull]
        public static Type InferReturnType(Stencil stencil, MethodBase methodBase, Type[] genericTypes, TypeHandle[] solvedTypeArguments, Type dataType)
        {
            Type returnType = methodBase.GetReturnType();
            if (returnType == typeof(void) || !returnType.ContainsGenericParameters)
                return null;

            Type genericType = genericTypes.SingleOrDefault();
            TypeHandle solvedType = solvedTypeArguments.SingleOrDefault();
            Type newOutputType = null;

            if (returnType == genericType) // T F<T>() == T
            {
                newOutputType = solvedType.Resolve(stencil);
            }
            else if (returnType.IsGenericType) // List<T> F<T>()
            {
                if (returnType.IsConstructedGenericType && returnType.GenericTypeArguments[0] == genericType || // class C<T> { List<T> F() }
                    !returnType.IsConstructedGenericType && returnType.GetGenericArguments()[0] == genericType) // class C { List<T> F<T>() }
                {
                    newOutputType = dataType.GetGenericTypeDefinition().MakeGenericType(solvedType.Resolve(stencil));
                }
            }

            return newOutputType;
        }

        static Type ReflectType(Type otherConnectedType, Type originalType, Type genericType)
        {
            if (originalType.ContainsGenericParameters) // C<T>.F(T) or C.F<T>(T)
            {
                if (originalType == genericType) // F(T)
                {
                    return otherConnectedType;
                }

                if (originalType.IsGenericType &&
                    originalType.GetGenericArguments().FirstOrDefault() == genericType
                ) // F(List<T>)
                {
                    return otherConnectedType.GenericTypeArguments.FirstOrDefault() ?? originalType;
                }
            }

            return originalType;
        }
    }
}
