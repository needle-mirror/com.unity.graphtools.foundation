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
        public List<VariableDeclarationModel> variableDeclarations;
        public Vector2 topLeftNodePosition;
        public List<StickyNoteModel> stickyNotes;
        public List<PlacematModel> placemats;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public bool IsEmpty() => (!nodes.Any() && !edges.Any() &&
            !variableDeclarations.Any() && !stickyNotes.Any() && !placemats.Any());

        internal static CopyPasteData GatherCopiedElementsData(IReadOnlyCollection<IGraphElementModel> graphElementModels)
        {
            var originalNodes = graphElementModels.OfType<INodeModel>().ToList();

            List<VariableDeclarationModel> variableDeclarationsToCopy = graphElementModels
                .OfType<VariableDeclarationModel>()
                .ToList();

            List<StickyNoteModel> stickyNotesToCopy = graphElementModels
                .OfType<StickyNoteModel>()
                .ToList();

            List<PlacematModel> placematsToCopy = graphElementModels
                .OfType<PlacematModel>()
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

        static INodeModel PasteNode(string operationName, INodeModel copiedNode, IGraphModel graph,
            SelectionStateComponent selectionState, Vector2 delta)
        {
            var pastedNodeModel = graph.DuplicateNode(copiedNode, delta);
            selectionState?.SelectElementsUponCreation(new[] { pastedNodeModel }, true);
            return pastedNodeModel;
        }

        internal static void PasteSerializedData(IGraphModel graph, TargetInsertionInfo targetInfo, SelectionStateComponent selectionState, CopyPasteData copyPasteData)
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

                selectionState?.SelectElementsUponCreation(duplicatedModels, true);
            }

            var nodeMapping = new Dictionary<INodeModel, INodeModel>();
            foreach (var originalModel in copyPasteData.nodes)
            {
                if (!graph.Stencil.CanPasteNode(originalModel, graph))
                    continue;

                var pastedNode = PasteNode(targetInfo.OperationName, originalModel, graph, selectionState, targetInfo.Delta);
                nodeMapping[originalModel] = pastedNode;
            }

            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.Guid.ToString(), nodeModel.Value);
            }

            foreach (var edge in copyPasteData.edges)
            {
                elementMapping.TryGetValue(edge.ToNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(edge.FromNodeGuid.ToString(), out var newOutput);

                var copiedEdge = graph.DuplicateEdge(edge, newInput as INodeModel, newOutput as INodeModel);
                if (copiedEdge != null)
                {
                    elementMapping.Add(edge.Guid.ToString(), copiedEdge);
                    selectionState?.SelectElementsUponCreation(new[] { copiedEdge }, true);
                }
            }

            foreach (var stickyNote in copyPasteData.stickyNotes)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position + targetInfo.Delta, stickyNote.PositionAndSize.size);
                var pastedStickyNote = (StickyNoteModel)graph.CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                selectionState?.SelectElementsUponCreation(new[] { pastedStickyNote }, true);
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<PlacematModel> pastedPlacemats = new List<PlacematModel>();
            // Keep placemats relative order
            foreach (var placemat in copyPasteData.placemats.OrderBy(p => p.ZOrder))
            {
                var newPosition = new Rect(placemat.PositionAndSize.position + targetInfo.Delta, placemat.PositionAndSize.size);
                var newTitle = "Copy of " + placemat.Title;
                var pastedPlacemat = (PlacematModel)graph.CreatePlacemat(newPosition);
                pastedPlacemat.Title = newTitle;
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElementsGuid = placemat.HiddenElementsGuid;
                selectionState?.SelectElementsUponCreation(new[] { pastedPlacemat }, true);
                pastedPlacemats.Add(pastedPlacemat);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            // Update hidden content to new node ids.
            foreach (var pastedPlacemat in pastedPlacemats)
            {
                if (pastedPlacemat.Collapsed)
                {
                    List<string> pastedHiddenContent = new List<string>();
                    foreach (var guid in pastedPlacemat.HiddenElementsGuid)
                    {
                        IGraphElementModel pastedElement;
                        if (elementMapping.TryGetValue(guid, out pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement.Guid.ToString());
                        }
                    }

                    pastedPlacemat.HiddenElementsGuid = pastedHiddenContent;
                }
            }
        }
    }
}
