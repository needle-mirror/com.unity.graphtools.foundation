using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class EdgeModelUpdateTests
    {
        [Test]
        public void ChangingEditModeAddClassName()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new EdgeModel();
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, graphView);

            Assert.IsFalse(edge.ClassListContains(Edge.editModeModifierUssClassName));

            model.EditMode = true;
            edge.UpdateFromModel();
            Assert.IsTrue(edge.ClassListContains(Edge.editModeModifierUssClassName));
        }
    }
}
