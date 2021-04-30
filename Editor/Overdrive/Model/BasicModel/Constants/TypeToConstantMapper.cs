using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public static class TypeToConstantMapper
    {
        static Dictionary<TypeHandle, Type> s_TypeToConstantNodeModelTypeCache;

        public static Type GetConstantNodeType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantNodeModelTypeCache == null)
            {
                s_TypeToConstantNodeModelTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { TypeHandle.Bool, typeof(BooleanConstant) },
                    { TypeHandle.Double, typeof(DoubleConstant) },
                    { TypeHandle.Float, typeof(FloatConstant) },
                    { TypeHandle.Int, typeof(IntConstant) },
                    { TypeHandle.Quaternion, typeof(QuaternionConstant) },
                    { TypeHandle.String, typeof(StringConstant) },
                    { TypeHandle.Vector2, typeof(Vector2Constant) },
                    { TypeHandle.Vector3, typeof(Vector3Constant) },
                    { TypeHandle.Vector4, typeof(Vector4Constant) },
                    { typeof(Color).GenerateTypeHandle(), typeof(ColorConstant) },
                    { typeof(AnimationClip).GenerateTypeHandle(), typeof(AnimationClipConstant) },
                    { typeof(Mesh).GenerateTypeHandle(), typeof(MeshConstant) },
                    { typeof(Texture2D).GenerateTypeHandle(), typeof(Texture2DConstant) },
                    { typeof(Texture3D).GenerateTypeHandle(), typeof(Texture3DConstant) },
                };
            }

            if (s_TypeToConstantNodeModelTypeCache.TryGetValue(typeHandle, out var result))
                return result;

            Type t = typeHandle.Resolve();
            if (t.IsEnum || t == typeof(Enum))
                return typeof(EnumConstant);

            return null;
        }
    }
}
