using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class EdgeModelUpdateTests
    {
        [Test]
        public void ChangingEditModeAddClassName()
        {
            var model = new EdgeModel(null, null);
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, null, null);

            Assert.IsFalse(edge.ClassListContains(Edge.k_EditModeModifierUssClassName));

            model.EditMode = true;
            edge.UpdateFromModel();
            Assert.IsTrue(edge.ClassListContains(Edge.k_EditModeModifierUssClassName));
        }
    }
}
