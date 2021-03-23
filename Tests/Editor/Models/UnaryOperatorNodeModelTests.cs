using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Models
{
    class UnaryOperatorNodeModelTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [TestCase(typeof(bool), UnaryOperatorKind.LogicalNot, true)]
        [TestCase(typeof(bool), UnaryOperatorKind.Minus, false)]
        public void TestHasValidOperationForInput(Type dataType, UnaryOperatorKind kind, bool result)
        {
            var node = Activator.CreateInstance<UnaryOperatorNodeModel>();
            node.AssetModel = GraphModel.AssetModel;
            node.Kind = kind;

            Assert.AreEqual(result, node.HasValidOperationForInput(null, Stencil.GenerateTypeHandle(dataType)));
        }
    }
}
