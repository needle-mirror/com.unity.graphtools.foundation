using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicGraphModel : IGTFGraphModel
    {
        readonly List<BasicNodeModel> m_Nodes = new List<BasicNodeModel>();
        public IEnumerable<BasicNodeModel> Nodes => m_Nodes;

        readonly List<BasicEdgeModel> m_Edges = new List<BasicEdgeModel>();
        public IEnumerable<BasicEdgeModel> Edges => m_Edges;

        readonly List<BasicPlacematModel> m_Placemats = new List<BasicPlacematModel>();
        public IEnumerable<BasicPlacematModel> Placemats => m_Placemats;

        readonly List<BasicStickyNoteModel> m_StickyNoteModels = new List<BasicStickyNoteModel>();
        public IEnumerable<BasicStickyNoteModel> Stickies => m_StickyNoteModels;

        public TNodeModel CreateNode<TNodeModel>(string title = "") where TNodeModel : BasicNodeModel, new()
        {
            var node = new TNodeModel {Title = title, GraphModel = this};
            m_Nodes.Add(node);
            return node;
        }

        public IGTFEdgeModel CreateEdgeGTF(IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            var edge = new BasicEdgeModel(inputPort, outputPort);
            m_Edges.Add(edge);
            edge.GraphModel = this;
            return edge;
        }

        public BasicStickyNoteModel CreateStickyNodeGTF(string title = "", string contents = "", Rect stickyRect = default)
        {
            var sticky = new BasicStickyNoteModel
            {
                Title = title,
                Contents = contents,
                PositionAndSize = stickyRect,
                Theme = StickyNoteTheme.Classic.ToString(),
                TextSize = StickyNoteFontSize.Small.ToString()
            };

            m_StickyNoteModels.Add(sticky);
            sticky.GraphModel = this;
            return sticky;
        }

        public void DeleteElements(IEnumerable<IGTFGraphElementModel> graphElementModels)
        {
            foreach (var model in graphElementModels)
            {
                switch (model)
                {
                    case BasicNodeModel basicNodeModel:
                        m_Nodes.Remove(basicNodeModel);
                        break;
                    case BasicEdgeModel basicEdgeModel:
                        m_Edges.Remove(basicEdgeModel);
                        break;
                    case BasicPlacematModel basicPlacematModel:
                        m_Placemats.Remove(basicPlacematModel);
                        break;
                    case BasicStickyNoteModel basicStickyNoteModel:
                        m_StickyNoteModels.Remove(basicStickyNoteModel);
                        break;
                }
            }
        }

        public void Disconnect(IGTFEdgeModel edge)
        {
            m_Edges.Remove(edge as BasicEdgeModel);
        }

        public BasicPlacematModel CreatePlacemat(string title, Rect posAndDim, int zOrder)
        {
            var placemat = new BasicPlacematModel(title);
            m_Placemats.Add(placemat);
            placemat.GraphModel = this;
            placemat.PositionAndSize = posAndDim;
            placemat.ZOrder = zOrder;
            return placemat;
        }

        public void Dispose() {}
    }
}
