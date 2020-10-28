using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class TestStencil : Stencil
    {
        ISearcherDatabaseProvider m_SearcherProvider;

        public TestStencil()
        {
            m_SearcherProvider = new ClassSearcherDatabaseProvider(this);
        }

        public override Blackboard CreateBlackboard(Store store, GraphView graphView)
        {
            return null;
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherProvider;
        }
    }

    class GraphModel : BasicModel.GraphModel
    {
        public override string GetSourceFilePath()
        {
            throw new NotImplementedException();
        }

        public override IEdgeModel CreateEdge(IPortModel inputPort, IPortModel outputPort)
        {
            var edge = new EdgeModel(this);
            edge.SetPorts(inputPort, outputPort);
            m_GraphEdgeModels.Add(edge);
            return edge;
        }

        protected override INodeModel CreateNodeInternal(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SpawnFlags spawnFlags = SpawnFlags.Default, Action<INodeModel> preDefineSetup = null, GUID? guid = null)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));
            NodeModel nodeModel;

            if (typeof(NodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (NodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            nodeModel.Position = position;
            nodeModel.Guid = guid ?? GUID.Generate();
            nodeModel.Title = nodeName;
            nodeModel.SetGraphModel(this);
            preDefineSetup?.Invoke(nodeModel);
            nodeModel.DefineNode();
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

        public override IPlacematModel CreatePlacemat(string title, Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            var placemat = CreatePlacemat();
            placemat.Title = title;
            placemat.PositionAndSize = position;
            return placemat;
        }

        public IStickyNoteModel CreateStickyNote()
        {
            var sticky = new StickyNoteModel(this);
            m_GraphStickyNoteModels.Add(sticky);
            return sticky;
        }

        public override IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            var sticky = CreateStickyNote();
            sticky.PositionAndSize = position;
            sticky.Theme = StickyNoteTheme.Classic.ToString();
            sticky.TextSize = StickyNoteFontSize.Small.ToString();
            return sticky;
        }

        public override CompilationResult Compile(ITranslator translator)
        {
            throw new NotImplementedException();
        }

        public GraphModel()
        {
            Stencil = new TestStencil();
        }
    }
}
