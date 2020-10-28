using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    class ConstantNodeSpawningTests
    {
        [Test]
        public void TestConstantEditorExtensionMethodsExistForBasicTypes()
        {
            var expectedTypes = new[] { typeof(string), typeof(Boolean), typeof(Int32), typeof(Double), typeof(Single), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Color) };
            for (var i = 0; i < expectedTypes.Length; i++)
            {
                var type = expectedTypes[i];

                var constantExtMethod = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(type, ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

                Assert.IsNotNull(constantExtMethod, $"No constant editor for {type.Name}");
            }
        }
    }
}
