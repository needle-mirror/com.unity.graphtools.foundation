using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScriptingTests.UI
{
    class LostEdgesTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [UnityTest]
        public IEnumerator LostEdgesAreDrawn()
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            var edge = (EdgeModel)GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);

            // simulate a renamed port by changing the edge's port id

            var field = typeof(EdgeModel).GetField("m_InputPortReference", BindingFlags.Instance | BindingFlags.NonPublic);
            var inputPortReference = (EdgeModel.PortReference)field.GetValue(edge);
            inputPortReference.UniqueId = "asd";
            field.SetValue(edge, inputPortReference);

            edge.UndoRedoPerformed(); // get rid of cached port models

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            var lostPortsAdded = GraphView.Query(className: "ge-port--data-type-missing-port").Build().ToList().Count;
            Assert.AreEqual(1, lostPortsAdded);
        }
    }
}
