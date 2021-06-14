using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class EdgeUICreationTests
    {
        [Test]
        public void EdgeHasExpectedParts()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new EdgeModel();
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, graphView);

            Assert.IsNotNull(edge.SafeQ<EdgeControl>(Edge.edgeControlPartName));
            Assert.IsFalse(edge.ClassListContains(Edge.editModeModifierUssClassName));
        }

        [Test]
        public void GhostEdgeHasExpectedClass()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new GhostEdgeModel(null);
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, graphView);

            Assert.IsTrue(edge.ClassListContains(Edge.ghostModifierUssClassName));
        }

        [Test]
        public void EdgeHasNotGhostClass()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new EdgeModel();
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, graphView);

            Assert.IsFalse(edge.ClassListContains(Edge.ghostModifierUssClassName));
        }
    }
}
