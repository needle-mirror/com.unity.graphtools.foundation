using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    public class CopyPasteData
    {
        public static CopyPasteData s_LastCopiedData;

        public List<INodeModel> nodes;
        public List<IEdgeModel> edges;
        public List<IVariableDeclarationModel> variableDeclarations;
        public Vector2 topLeftNodePosition;
        public List<IStickyNoteModel> stickyNotes;
        public List<IPlacematModel> placemats;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public bool IsEmpty() => (!nodes.Any() && !edges.Any() &&
            !variableDeclarations.Any() && !stickyNotes.Any() && !placemats.Any());

        internal static CopyPasteData GatherCopiedElementsData(IReadOnlyCollection<IGraphElementModel> graphElementModels)
        {
            var originalNodes = graphElementModels.OfType<INodeModel>().ToList();

            List<IVariableDeclarationModel> variableDeclarationsToCopy = graphElementModels
                .OfType<IVariableDeclarationModel>()
                .ToList();

            List<IStickyNoteModel> stickyNotesToCopy = graphElementModels
                .OfType<IStickyNoteModel>()
                .ToList();

            List<IPlacematModel> placematsToCopy = graphElementModels
                .OfType<IPlacematModel>()
                .ToList();

            List<IEdgeModel> edgesToCopy = graphElementModels
                .OfType<IEdgeModel>()
                .ToList();

            Vector2 topLeftNodePosition = Vector2.positiveInfinity;
            foreach (var n in originalNodes)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.Position);
            }
            foreach (var n in stickyNotesToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }
            foreach (var n in placematsToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }
            if (topLeftNodePosition == Vector2.positiveInfinity)
            {
                topLeftNodePosition = Vector2.zero;
            }

            CopyPasteData copyPasteData = new CopyPasteData
            {
                topLeftNodePosition = topLeftNodePosition,
                nodes = originalNodes,
                edges = edgesToCopy,
                variableDeclarations = variableDeclarationsToCopy,
                stickyNotes = stickyNotesToCopy,
                placemats = placematsToCopy
            };

            return copyPasteData;
        }

        internal static void PasteSerializedData(IGraphModel graph, TargetInsertionInfo targetInfo,
            GraphViewStateComponent.StateUpdater graphViewUpdater,
            SelectionStateComponent.StateUpdater selectionStateUpdater,
            CopyPasteData copyPasteData)
        {
            var elementMapping = new Dictionary<string, IGraphElementModel>();

            if (copyPasteData.variableDeclarations.Any())
            {
                List<IVariableDeclarationModel> variableDeclarationModels =
                    copyPasteData.variableDeclarations.Cast<IVariableDeclarationModel>().ToList();
                List<IVariableDeclarationModel> duplicatedModels = new List<IVariableDeclarationModel>();

                foreach (var sourceModel in variableDeclarationModels)
                {
                    duplicatedModels.Add(graph.DuplicateGraphVariableDeclaration(sourceModel));
                }

                graphViewUpdater?.MarkNew(duplicatedModels);
                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            var nodeMapping = new Dictionary<INodeModel, INodeModel>();
            foreach (var originalModel in copyPasteData.nodes)
            {
                if (!graph.Stencil.CanPasteNode(originalModel, graph))
                    continue;

                var pastedNode = graph.DuplicateNode(originalModel, targetInfo.Delta);
                graphViewUpdater.MarkNew(pastedNode);
                selectionStateUpdater?.SelectElements(new[] { pastedNode }, true);
                nodeMapping[originalModel] = pastedNode;
            }

            // PF FIXME we could do this in the foreach above
            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.Guid.ToString(), nodeModel.Value);
            }

            foreach (var edge in copyPasteData.edges)
            {
                elementMapping.TryGetValue(edge.ToPort.NodeModel.Guid.ToString(), out var newInput);
                elementMapping.TryGetValue(edge.FromPort.NodeModel.Guid.ToString(), out var newOutput);

                var copiedEdge = graph.DuplicateEdge(edge, newInput as INodeModel, newOutput as INodeModel);
                if (copiedEdge != null)
                {
                    elementMapping.Add(edge.Guid.ToString(), copiedEdge);
                    graphViewUpdater?.MarkNew(copiedEdge);
                    selectionStateUpdater?.SelectElements(new[] { copiedEdge }, true);
                }
            }

            foreach (var stickyNote in copyPasteData.stickyNotes)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position + targetInfo.Delta, stickyNote.PositionAndSize.size);
                var pastedStickyNote = (IStickyNoteModel)graph.CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                graphViewUpdater.MarkNew(pastedStickyNote);
                selectionStateUpdater?.SelectElements(new[] { pastedStickyNote }, true);
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<IPlacematModel> pastedPlacemats = new List<IPlacematModel>();
            // Keep placemats relative order
            foreach (var placemat in copyPasteData.placemats.OrderBy(p => p.ZOrder))
            {
                var newPosition = new Rect(placemat.PositionAndSize.position + targetInfo.Delta, placemat.PositionAndSize.size);
                var newTitle = "Copy of " + placemat.Title;
                var pastedPlacemat = (IPlacematModel)graph.CreatePlacemat(newPosition);
                pastedPlacemat.Title = newTitle;
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElements = placemat.HiddenElements;
                graphViewUpdater.MarkNew(pastedPlacemat);
                selectionStateUpdater?.SelectElements(new[] { pastedPlacemat }, true);
                pastedPlacemats.Add(pastedPlacemat);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            // Update hidden content to new node ids.
            foreach (var pastedPlacemat in pastedPlacemats)
            {
                if (pastedPlacemat.Collapsed)
                {
                    List<IGraphElementModel> pastedHiddenContent = new List<IGraphElementModel>();
                    foreach (var elementGUID in pastedPlacemat.HiddenElements.Select(t => t.Guid.ToString()))
                    {
                        IGraphElementModel pastedElement;
                        if (elementMapping.TryGetValue(elementGUID, out pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement);
                        }
                    }

                    pastedPlacemat.HiddenElements = pastedHiddenContent;
                }
            }
        }
    }
}
