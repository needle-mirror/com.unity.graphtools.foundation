using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Models
{
    public class QuaternionNodeModelTests
    {
        [Test]
        public void TestQuaternionConstantDefaultValue()
        {
            var node = new QuaternionConstant();
            Assert.AreEqual(Quaternion.identity, node.DefaultValue);
        }
    }
}
