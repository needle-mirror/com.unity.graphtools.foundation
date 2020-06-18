using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Models
{
    public class QuaternionNodeModelTests
    {
        [Test]
        public void TestQuaternionConstantDefaultValue()
        {
            var node = new QuaternionConstantModel();
            node.PredefineSetup(TypeHandle.Quaternion);
            Assert.AreEqual(Quaternion.identity, node.value);
        }
    }
}
