using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class EdgeUICreationTests
    {
        [Test]
        public void EdgeHasExpectedParts()
        {
            var model = new EdgeModel();
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, null);

            Assert.IsNotNull(edge.Q<EdgeControl>(Edge.k_EdgeControlPartName));
            Assert.IsFalse(edge.ClassListContains(Edge.k_EditModeModifierUssClassName));
        }

        [Test]
        public void GhostEdgeHasExpectedClass()
        {
            var model = new GhostEdgeModel(null);
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, null);

            Assert.IsTrue(edge.ClassListContains(Edge.k_GhostModifierUssClassName));
        }

        [Test]
        public void EdgeHasNotGhostClass()
        {
            var model = new EdgeModel();
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, null);

            Assert.IsFalse(edge.ClassListContains(Edge.k_GhostModifierUssClassName));
        }
    }
}
