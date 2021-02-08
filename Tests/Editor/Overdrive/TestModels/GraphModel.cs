using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class TestStencil : Stencil
    {
        ISearcherDatabaseProvider m_SearcherProvider;

        public TestStencil()
        {
            m_SearcherProvider = new ClassSearcherDatabaseProvider(this);
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherProvider;
        }
    }

    class GraphModel : BasicModel.GraphModel
    {
        public override Type DefaultStencilType => typeof(TestStencil);

        public override IEdgeModel CreateEdge(IPortModel toPort, IPortModel fromPort)
        {
            var edge = new EdgeModel(this);
            edge.SetPorts(toPort, fromPort);
            m_GraphEdgeModels.Add(edge);
            return edge;
        }

        protected override INodeModel CreateNodeInternal(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SpawnFlags spawnFlags = SpawnFlags.Default, Action<INodeModel> preDefineSetup = null, SerializableGUID guid = default)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));
            NodeModel nodeModel;

            if (typeof(NodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (NodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            nodeModel.Position = position;
            nodeModel.Guid = guid.Valid ? guid : SerializableGUID.Generate();
            nodeModel.Title = nodeName;
            nodeModel.SetGraphModel(this);
            preDefineSetup?.Invoke(nodeModel);
            nodeModel.OnCreateNode();
            if (!spawnFlags.IsOrphan())
            {
                AddNode(nodeModel);
            }
            return nodeModel;
        }

        public IPlacematModel CreatePlacemat()
        {
            var placemat = new PlacematModel(this);
            m_GraphPlacematModels.Add(placemat);
            return placemat;
        }

        public override IPlacematModel CreatePlacemat(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var placemat = CreatePlacemat();
            placemat.PositionAndSize = position;
            return placemat;
        }

        public IStickyNoteModel CreateStickyNote()
        {
            var sticky = new StickyNoteModel(this);
            m_GraphStickyNoteModels.Add(sticky);
            return sticky;
        }

        public override IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var sticky = CreateStickyNote();
            sticky.PositionAndSize = position;
            sticky.Theme = StickyNoteTheme.Classic.ToString();
            sticky.TextSize = StickyNoteFontSize.Small.ToString();
            return sticky;
        }

        public override bool CheckIntegrity(Verbosity errors)
        {
            return true;
        }
    }
}
