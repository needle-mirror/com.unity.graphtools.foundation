using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor.ConstantEditor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Extensions
{
    internal class ConstantNodeSpawningTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test(Description = "make sure basic types have a custom editor on the Graphtools side")]
        public void TestConstantEditorExtensionMethodsExistForBasicTypes()
        {
            Assert.That(GraphModel.NodeModels.Count, Is.Zero);
            var expectedTypes = new[] { typeof(string), typeof(Boolean), typeof(Int32), typeof(Double), typeof(Single), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Color) };
            for (var i = 0; i < expectedTypes.Length; i++)
            {
                var type = expectedTypes[i];
                Type constantNodeType = GraphModel.Stencil.GetConstantNodeModelType(type);

                var constantExtMethod = ModelUtility.ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(constantNodeType, ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);
                Assert.That(constantExtMethod, Is.Not.Null, $"No constant editor for {type.Name} / {constantNodeType.Name}");

                GraphModel.CreateConstantNode(constantNodeType.Name, type.GenerateTypeHandle(Stencil), 100f * i * Vector2.right);
            }
            Assert.That(GraphModel.NodeModels.OfType<ConstantNodeModel>().Count(), Is.EqualTo(expectedTypes.Length));
        }
    }
}
