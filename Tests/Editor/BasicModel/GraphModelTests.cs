using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.BasicModelTests
{
    public class GraphModelTests
    {
        IGraphAssetModel m_GraphAsset;

        [SetUp]
        public void SetUp()
        {
            m_GraphAsset = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test");
            m_GraphAsset.CreateGraph("Graph");
        }

        [Test]
        public void TryGetModelByGUIDWorks()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = graphModel.CreateNode<Type0FakeNodeModel>();
            var edge = graphModel.CreateEdge(node1.ExeInput0, node2.ExeOutput0);
            var placemat = graphModel.CreatePlacemat(new Rect(100, 100, 300, 300));
            var stickyNote = graphModel.CreateStickyNote(new Rect(-100, -100, 100, 100));
            var constant = graphModel.CreateConstantNode(TypeHandle.Float, "constant", new Vector2(42, 42));
            var variableDeclaration = graphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "varDecl", ModifierFlags.None, true);
            var variable = graphModel.CreateVariableNode(variableDeclaration, new Vector2(-76, 245));
            var portal = graphModel.CreateEntryPortalFromEdge(edge);
            var badge = new BadgeModel(node1);
            graphModel.AddBadge(badge);

            var graphElements = new IGraphElementModel[] { node1, node2, edge, placemat, stickyNote, constant, variableDeclaration, variable, portal, badge };
            foreach (var element in graphElements)
            {
                Assert.IsTrue(graphModel.TryGetModelFromGuid(element.Guid, out var retrieved), element + " was not found");
                Assert.AreSame(element, retrieved);
            }

            graphModel.DeleteBadges();
            graphModel.DeleteEdges(new[] { edge });
            graphModel.DeleteNodes(new IInputOutputPortsNodeModel[] { node1, node2, constant, variable, portal }, true);
            graphModel.DeletePlacemats(new[] { placemat });
            graphModel.DeleteStickyNotes(new[] { stickyNote });
            graphModel.DeleteVariableDeclarations(new[] { variableDeclaration });
            foreach (var element in graphElements)
            {
                Assert.IsFalse(graphModel.TryGetModelFromGuid(element.Guid, out _), element + " was found after removal");
            }
        }
    }
}
