using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    class VseContextualMenuBuilder
    {
        class GraphElementModelComparer : IEqualityComparer<IGraphElement>
        {
            public bool Equals(IGraphElement x, IGraphElement y) => ReferenceEquals(x?.Model, y?.Model);
            public int GetHashCode(IGraphElement obj) => obj.Model?.GetHashCode() ?? 0;
        }

        readonly Store m_Store;
        readonly ContextualMenuPopulateEvent m_Evt;
        readonly IList<ISelectableGraphElement> m_Selection;
        readonly VseGraphView m_GraphView;
        static GraphElementModelComparer s_GraphElementModelComparer = new GraphElementModelComparer();

        public VseContextualMenuBuilder(Store store, ContextualMenuPopulateEvent evt, IList<ISelectableGraphElement> selection, VseGraphView graphView)
        {
            m_Store = store;
            m_Evt = evt;
            m_Selection = selection;
            m_GraphView = graphView;
        }

        public void BuildContextualMenu()
        {
            var selectedModelsDictionary = m_Selection
                .OfType<IGraphElement>()
                .Where(x => !(x is BlackboardThisField)) // this blackboard field
                .Distinct(s_GraphElementModelComparer)
                .ToDictionary(x => x.Model);

            IReadOnlyCollection<IGTFGraphElementModel> selectedModelsKeys = selectedModelsDictionary.Keys.ToList();

            BuildBlackboardContextualMenu();

            var originatesFromBlackboard = (m_Evt.target as VisualElement)?.GetFirstOfType<Blackboard>() != null;
            if (!originatesFromBlackboard || m_Evt.target is IGraphElement)
            {
                BuildGraphViewContextualMenu();
                if (!originatesFromBlackboard)
                {
                    BuildNodeContextualMenu(selectedModelsDictionary);
                    BuildEdgeContextualMenu(selectedModelsDictionary);
                }

                BuildVariableNodeContextualMenu(selectedModelsKeys);
                BuildPortalContextualMenu(selectedModelsKeys);
                if (!originatesFromBlackboard)
                {
                    BuildConstantNodeContextualMenu(selectedModelsKeys);
                    BuildSpecialContextualMenu(selectedModelsKeys);
                    BuildPlacematContextualMenu();
                    BuildStickyNoteContextualMenu();
                    BuildRefactorContextualMenu(selectedModelsKeys);
                }

                if (selectedModelsDictionary.Any())
                {
                    m_Evt.menu.AppendAction("Delete", menuAction =>
                    {
                        m_Store.Dispatch(new DeleteElementsAction(selectedModelsKeys.ToArray()));
                    }, eventBase => DropdownMenuAction.Status.Normal);
                }
            }

            if (originatesFromBlackboard && !(m_Evt.target is IGraphElement))
            {
                var currentGraphModel = m_Store.GetState().CurrentGraphModel;
                currentGraphModel?.Stencil.GetBlackboardProvider()
                    .BuildContextualMenu(m_Evt.menu,
                        (VisualElement)m_Evt.target,
                        m_Store,
                        m_Evt.mousePosition);
            }

            var renamable = originatesFromBlackboard && m_Evt.target is IRenamable ? m_Evt.target as IRenamable :
                (!originatesFromBlackboard && selectedModelsDictionary.Count == 1) ? selectedModelsDictionary.Single().Value as IRenamable : null;
            if (renamable != null)
            {
                m_Evt.menu.AppendAction("Rename", menuAction =>
                {
                    renamable.Rename(true);
                    m_Evt.PreventDefault();
                    m_Evt.StopImmediatePropagation();
                }, eventBase => DropdownMenuAction.Status.Normal);
            }

            if (m_Evt.target is IContextualMenuBuilder contextualMenuBuilder)
            {
                contextualMenuBuilder.BuildContextualMenu(m_Evt);
            }

            // PF: Ugliest hack. This will not be necessary when Contextual menu building is done properly.
            if (m_Evt.target is Port port)
            {
                // Ports are transparent.
                var node = port.GetFirstAncestorOfType<Node>() as IContextualMenuBuilder;
                node?.BuildContextualMenu(m_Evt);
            }
        }

        void BuildGraphViewContextualMenu()
        {
            if (!(m_Evt.target is GraphView) && !(m_Evt.target is Placemat))
                return;

            m_Evt.menu.AppendAction("Create Node", menuAction =>
            {
                m_GraphView.window.DisplaySmartSearch(menuAction);
            });

            m_Evt.menu.AppendAction("Create Placemat", menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                Vector2 graphPosition = m_GraphView.contentViewContainer.WorldToLocal(mousePosition);

                m_Store.Dispatch(new CreatePlacematAction(null, new Rect(graphPosition.x, graphPosition.y, 200, 200)));
            });

            m_Evt.menu.AppendSeparator();

            if (!(m_Evt.target is GraphView))
                return;

            m_Evt.menu.AppendAction("Cut", menuAction => m_GraphView.InvokeCutSelectionCallback(),
                x => m_GraphView.CanCutSelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_Evt.menu.AppendAction("Copy", menuAction => m_GraphView.InvokeCopySelectionCallback(),
                x => m_GraphView.CanCopySelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_Evt.menu.AppendAction("Paste", menuAction => m_GraphView.InvokePasteCallback(),
                x => m_GraphView.CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (Unsupported.IsDeveloperBuild())
            {
                m_Evt.menu.AppendSeparator();

                m_Evt.menu.AppendAction("Internal/Refresh All UI", menuAction => m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All)));
            }
        }

        void BuildBlackboardContextualMenu()
        {
            // Nothing at the moment.
//            var blackboard = (m_Evt.target as VisualElement)?.GetFirstOfType<Blackboard>();
//            if (blackboard == null)
//                return;
        }

        void BuildNodeContextualMenu(Dictionary<IGTFGraphElementModel, IGraphElement> selectedModels)
        {
            var selectedModelsKeys = selectedModels.Keys.ToArray();
            if (!selectedModelsKeys.Any())
                return;

            var models = selectedModelsKeys.OfType<NodeModel>().ToArray();
            var connectedModels = models.Where(x => x.InputsByDisplayOrder.Any(y => y.IsConnected) && x.OutputsByDisplayOrder.Any(y => y.IsConnected)).ToArray();
            bool canSelectionBeBypassed = connectedModels.Any();

            m_Evt.menu.AppendSeparator();

            m_Evt.menu.AppendAction("Align Item (Q)", menuAction => m_GraphView.AlignSelection(false));
            m_Evt.menu.AppendAction("Align Hierarchy (Shift+Q)", menuAction => m_GraphView.AlignSelection(true));

            var content = selectedModels.Values.OfType<GraphElement>().Where(e => (e.parent is GraphView.Layer) && (e is GraphElements.Node || e is StickyNote)).ToList();
            m_Evt.menu.AppendAction("Create Placemat Under Selection", menuAction =>
            {
                Rect bounds = new Rect();
                if (GraphElements.Placemat.ComputeElementBounds(ref bounds, content))
                {
                    m_Store.Dispatch(new CreatePlacematAction(null, bounds));
                }
            }, action => (content.Count == 0) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            m_Evt.menu.AppendSeparator();

            m_Evt.menu.AppendAction("Cut", menuAction => m_GraphView.InvokeCutSelectionCallback(),
                x => m_GraphView.CanCutSelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_Evt.menu.AppendAction("Copy", menuAction => m_GraphView.InvokeCopySelectionCallback(),
                x => m_GraphView.CanCopySelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_Evt.menu.AppendAction("Paste", menuAction => m_GraphView.InvokePasteCallback(),
                x => m_GraphView.CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_Evt.menu.AppendSeparator();

            m_Evt.menu.AppendAction("Delete", menuAction =>
            {
                m_Store.Dispatch(new DeleteElementsAction(selectedModelsKeys.ToArray()));
            }, eventBase => DropdownMenuAction.Status.Normal);

            m_Evt.menu.AppendSeparator();

            if (models.Any())
            {
                m_Evt.menu.AppendAction("Disconnect", menuAction =>
                {
                    m_Store.Dispatch(new DisconnectNodeAction(models));
                });
            }

            if (canSelectionBeBypassed)
            {
                m_Evt.menu.AppendAction("Remove", menuAction =>
                {
                    m_Store.Dispatch(new RemoveNodesAction(connectedModels, models));
                }, eventBase => DropdownMenuAction.Status.Normal);
            }

            var placemats = selectedModelsKeys.OfType<PlacematModel>().ToArray();
            if (models.Any() || placemats.Any())
            {
                m_Evt.menu.AppendAction("Color/Change...", menuAction =>
                {
                    void ChangeNodesColor(Color pickedColor)
                    {
                        m_Store.Dispatch(new ChangeElementColorAction(pickedColor, models, placemats));
                    }

                    var defaultColor = new Color(0.5f, 0.5f, 0.5f);
                    if (!models.Any() && placemats.Length == 1)
                    {
                        defaultColor = placemats[0].Color;
                    }
                    else if (models.Length == 1 && !placemats.Any())
                    {
                        defaultColor = models[0].Color;
                    }

                    GraphViewStaticBridge.ShowColorPicker(ChangeNodesColor, defaultColor, true);
                }, eventBase => DropdownMenuAction.Status.Normal);

                m_Evt.menu.AppendAction("Color/Reset", menuAction =>
                {
                    m_Store.Dispatch(new ResetElementColorAction(models, placemats));
                }, eventBase => DropdownMenuAction.Status.Normal);

                if (m_GraphView.selection.OfType<GraphElement>().Where(elem => !(elem is Edge) && elem.visible).ToList().Count > 1)
                {
                    m_Evt.menu.AppendAction("Alignment/Top", menuAction => AutoAlignmentHelper.SendAlignAction(m_GraphView, AutoAlignmentHelper.AlignmentReference.Top));
                    m_Evt.menu.AppendAction("Alignment/Bottom", menuAction => AutoAlignmentHelper.SendAlignAction(m_GraphView, AutoAlignmentHelper.AlignmentReference.Bottom));
                    m_Evt.menu.AppendAction("Alignment/Left", menuAction => AutoAlignmentHelper.SendAlignAction(m_GraphView, AutoAlignmentHelper.AlignmentReference.Left));
                    m_Evt.menu.AppendAction("Alignment/Right", menuAction => AutoAlignmentHelper.SendAlignAction(m_GraphView, AutoAlignmentHelper.AlignmentReference.Right));
                    m_Evt.menu.AppendAction("Alignment/Horizontal Center", menuAction => AutoAlignmentHelper.SendAlignAction(m_GraphView, AutoAlignmentHelper.AlignmentReference.HorizontalCenter));
                    m_Evt.menu.AppendAction("Alignment/Vertical Center", menuAction => AutoAlignmentHelper.SendAlignAction(m_GraphView, AutoAlignmentHelper.AlignmentReference.VerticalCenter));
                }
            }
            else
            {
                m_Evt.menu.AppendAction("Color", menuAction => {}, eventBase => DropdownMenuAction.Status.Disabled);
            }
        }

        void BuildEdgeContextualMenu(Dictionary<IGTFGraphElementModel, IGraphElement> selectedModels)
        {
            var allEdgeModels = selectedModels.Keys.OfType<IEdgeModel>().ToList();
            bool addSeparator = false;

            if (allEdgeModels.Any())
            {
                var edgeData = selectedModels.Where(s => s.Value is Edge).Select(
                    s =>
                    {
                        var e = s.Value as Edge;
                        var outputPort = e.Output.GetUI<GraphElements.Port>(e.GraphView);
                        var inputPort = e.Input.GetUI<GraphElements.Port>(e.GraphView);
                        var outputNode = e.Output.NodeModel.GetUI<GraphElements.Node>(e.GraphView);
                        var inputNode = e.Input.NodeModel.GetUI<GraphElements.Node>(e.GraphView);
                        return (s.Key as IGTFEdgeModel,
                            outputPort.ChangeCoordinatesTo(outputNode.parent, outputPort.layout.center),
                            inputPort.ChangeCoordinatesTo(inputNode.parent, inputPort.layout.center));
                    }).ToList();

                m_Evt.menu.AppendAction("Create Portals", menuAction =>
                {
                    m_Store.Dispatch(new ConvertEdgesToPortalsAction(edgeData));
                },
                    eventBase => DropdownMenuAction.Status.Normal);
                addSeparator = true;
            }

            var eventTarget = m_Evt.triggerEvent.target as VisualElement;
            var edge = eventTarget?.GetFirstAncestorOfType<Edge>();
            if (edge?.EdgeModel != null)
            {
                if (eventTarget is EdgeControl edgeControlElement)
                {
                    if (addSeparator)
                        m_Evt.menu.AppendSeparator();

                    var p = edgeControlElement.WorldToLocal(m_Evt.triggerEvent.originalMousePosition);
                    edgeControlElement.FindNearestCurveSegment(p, out _, out var controlPointIndex, out _);
                    p = edge.WorldToLocal(m_Evt.triggerEvent.originalMousePosition);
                    if (edge.EdgeModel.EditMode)
                    {
                        m_Evt.menu.AppendAction("Stop editing edge", menuAction =>
                        {
                            m_Store.Dispatch(new SetEdgeEditModeAction(edge.EdgeModel, false));
                        });
                        m_Evt.menu.AppendAction("Add control point", menuAction =>
                        {
                            m_Store.Dispatch(new AddControlPointOnEdgeAction(edge.EdgeModel, controlPointIndex, p));
                        });
                    }
                    else
                    {
                        if (edge.EdgeModel.FromPort.HasReorderableEdges)
                        {
                            var siblingEdges = edge.EdgeModel.FromPort.ConnectedEdges.ToList();
                            var siblingEdgesCount = siblingEdges.Count;

                            var index = siblingEdges.IndexOf(edge.EdgeModel);
                            m_Evt.menu.AppendAction("Move first", a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveFirst),
                                siblingEdgesCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                            m_Evt.menu.AppendAction("Move up", a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveUp),
                                siblingEdgesCount > 1 && index > 0  ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                            m_Evt.menu.AppendAction("Move down", a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveDown),
                                siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                            m_Evt.menu.AppendAction("Move last", a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveLast),
                                siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                            m_Evt.menu.AppendSeparator();

                            void ReorderEdges(ReorderEdgeAction.ReorderType reorderType)
                            {
                                m_Store.Dispatch(new ReorderEdgeAction(edge.EdgeModel, reorderType));
                                // Refresh the edge bubbles
                                ((IHasPorts)edge.EdgeModel.FromPort.NodeModel).RevealReorderableEdgesOrder(true, edge.EdgeModel);
                                edge.EdgeModel.FromPort.NodeModel.GetUI<Node>(m_GraphView)?.UpdateOutgoingExecutionEdges();
                            }
                        }

                        m_Evt.menu.AppendAction("Edit edge", menuAction =>
                        {
                            m_Store.Dispatch(new SetEdgeEditModeAction(edge.EdgeModel, true));
                        });
                    }
                }
                else if (eventTarget is EdgeControlPoint edgeControlPointElement)
                {
                    if (addSeparator)
                        m_Evt.menu.AppendSeparator();

                    m_Evt.menu.AppendAction("Stop editing edge", menuAction =>
                    {
                        m_Store.Dispatch(new SetEdgeEditModeAction(edge.EdgeModel, false));
                    });
                    m_Evt.menu.AppendAction("Remove control point", menuAction =>
                    {
                        var graphView = edgeControlPointElement.GetFirstAncestorOfType<VseGraphView>();
                        if (graphView == null)
                            return;

                        int controlPointIndex = edgeControlPointElement.parent.Children().IndexOf(edgeControlPointElement);
                        graphView.store.Dispatch(new RemoveEdgeControlPointAction(edge.EdgeModel, controlPointIndex));
                    });
                }
            }
        }

        void BuildVariableNodeContextualMenu(IReadOnlyCollection<IGTFGraphElementModel> selectedModels)
        {
            IGTFVariableNodeModel[] models = selectedModels.Where(x => x is VariableNodeModel).Cast<IGTFVariableNodeModel>().ToArray();
            if (!models.Any())
                return;

            m_Evt.menu.AppendAction("Variable/Convert",
                menuAction =>
                {
                    m_Store.Dispatch(new ConvertVariableNodesToConstantNodesAction(models));
                }, x => DropdownMenuAction.Status.Normal);
            m_Evt.menu.AppendAction("Variable/Itemize",
                menuAction =>
                {
                    m_Store.Dispatch(new ItemizeVariableNodeAction(models));
                }, x => DropdownMenuAction.Status.Normal);
        }

        void BuildPortalContextualMenu(IReadOnlyCollection<IGTFGraphElementModel> selectedModels)
        {
            var models = selectedModels.OfType<IGTFEdgePortalModel>().ToList();
            if (!models.Any())
                return;

            //var stencil = m_Store?.GetState()?.CurrentGraphModel?.Stencil;
            var canCreate = models.Where(p => p.CanCreateOppositePortal()).ToList();
            m_Evt.menu.AppendAction("Portals/Create Opposite",
                menuAction =>
                {
                    m_Store.Dispatch(new CreatePortalsOppositeAction(canCreate));
                }, x => canCreate.Any() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void BuildConstantNodeContextualMenu(IReadOnlyCollection<IGTFGraphElementModel> selectedModels)
        {
            var models = selectedModels.Where(x => x is IConstantNodeModel).Cast<IConstantNodeModel>().ToArray();
            if (!models.Any())
                return;

            m_Evt.menu.AppendAction("Constant/Convert",
                menuAction => m_Store.Dispatch(new ConvertConstantNodesToVariableNodesAction(models)), x => DropdownMenuAction.Status.Normal);
            m_Evt.menu.AppendAction("Constant/Itemize",
                menuAction => m_Store.Dispatch(new ItemizeConstantNodeAction(models)), x => DropdownMenuAction.Status.Normal);
            m_Evt.menu.AppendAction("Constant/Lock",
                menuAction => m_Store.Dispatch(new ToggleLockConstantNodeAction(models)), x => DropdownMenuAction.Status.Normal);
        }

        void BuildSpecialContextualMenu(IReadOnlyCollection<IGTFGraphElementModel> selectedModels)
        {
            var graphElementModels = selectedModels.ToList();
            if (graphElementModels.Count == 2)
            {
                if (graphElementModels.FirstOrDefault(x => x is IEdgeModel) is IEdgeModel edgeModel &&
                    graphElementModels.FirstOrDefault(x => x is IGTFNodeModel) is IGTFNodeModel nodeModel)
                {
                    m_Evt.menu.AppendAction("Insert", menuAction => m_Store.Dispatch(new SplitEdgeAndInsertNodeAction(edgeModel, nodeModel)),
                        eventBase => DropdownMenuAction.Status.Normal);
                }
            }
        }

        void BuildPlacematContextualMenu()
        {
            Placemat.BuildContextualMenu(m_Evt.target as Placemat, m_Evt.menu);
        }

        void BuildStickyNoteContextualMenu()
        {
            var stickyNoteSelection = m_Selection?.OfType<StickyNote>();
            if (stickyNoteSelection == null || !stickyNoteSelection.Any())
                return;

            var stickyNoteModels = stickyNoteSelection.Select(m => m.StickyNoteModel).ToArray();

            DropdownMenuAction.Status GetThemeStatus(DropdownMenuAction a)
            {
                if (stickyNoteModels.Length == 0)
                    return DropdownMenuAction.Status.Normal;

                if (stickyNoteModels.Any(noteModel => noteModel.Theme != stickyNoteModels.First().Theme))
                {
                    // Values are not all the same.
                    return DropdownMenuAction.Status.Normal;
                }

                return stickyNoteModels.First().Theme == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            }

            DropdownMenuAction.Status GetSizeStatus(DropdownMenuAction a)
            {
                if (stickyNoteModels.Length == 0)
                    return DropdownMenuAction.Status.Normal;

                if (stickyNoteModels.Any(noteModel => noteModel.TextSize != stickyNoteModels.First().TextSize))
                {
                    // Values are not all the same.
                    return DropdownMenuAction.Status.Normal;
                }

                return stickyNoteModels.First().TextSize == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            }

            foreach (var value in GraphElements.StickyNote.GetThemes())
                m_Evt.menu.AppendAction("Theme/" + value,
                    menuAction => m_Store.Dispatch(new UpdateStickyNoteThemeAction(stickyNoteModels, menuAction.userData as string)),
                    GetThemeStatus, value);

            foreach (var value in GraphElements.StickyNote.GetSizes())
                m_Evt.menu.AppendAction("Text Size/" + value,
                    menuAction => m_Store.Dispatch(new UpdateStickyNoteTextSizeAction(stickyNoteModels, menuAction.userData as string)),
                    GetSizeStatus, value);
        }

        void BuildRefactorContextualMenu(IReadOnlyCollection<IGTFGraphElementModel> selectedModels)
        {
            var models = selectedModels.OfType<IGTFNodeModel>().ToArray();
            if (!models.Any())
                return;

            var canDisable = models.Any();
            var willDisable = models.Any(n => n.State == ModelState.Enabled);

            if (canDisable)
                m_Evt.menu.AppendAction(willDisable ? "Disable Selection" : "Enable Selection", menuAction =>
                {
                    m_Store.Dispatch(new SetNodeEnabledStateAction(models, willDisable ? ModelState.Disabled : ModelState.Enabled));
                });


            if (Unsupported.IsDeveloperBuild())
            {
                m_Evt.menu.AppendAction("Internal/Redefine Node",
                    action =>
                    {
                        foreach (var model in selectedModels.OfType<NodeModel>())
                            model.DefineNode();
                    }, _ => DropdownMenuAction.Status.Normal);

                m_Evt.menu.AppendAction("Internal/Refresh Selected Element(s)",
                    menuAction => { m_Store.Dispatch(new RefreshUIAction(selectedModels.Cast<IGTFGraphElementModel>().ToList())); },
                    _ => DropdownMenuAction.Status.Normal);
            }
        }
    }
}
