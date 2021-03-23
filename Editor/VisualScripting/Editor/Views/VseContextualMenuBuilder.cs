using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.AutoDimOpacity;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;
using VisualElementExtensions = UnityEditor.VisualScripting.Editor.AutoDimOpacity.VisualElementExtensions;

namespace UnityEditor.VisualScripting.Editor
{
    class VseContextualMenuBuilder
    {
        class HasGraphElementModelComparer : IEqualityComparer<IHasGraphElementModel>
        {
            public bool Equals(IHasGraphElementModel x, IHasGraphElementModel y) => ReferenceEquals(x?.GraphElementModel, y?.GraphElementModel);
            public int GetHashCode(IHasGraphElementModel obj) => obj.GraphElementModel?.GetHashCode() ?? 0;
        }

        readonly Store m_Store;
        readonly ContextualMenuPopulateEvent m_Evt;
        readonly IList<ISelectable> m_Selection;
        readonly VseGraphView m_GraphView;
        static HasGraphElementModelComparer s_HasGraphElementModelComparer = new HasGraphElementModelComparer();

        public VseContextualMenuBuilder(Store store, ContextualMenuPopulateEvent evt, IList<ISelectable> selection, VseGraphView graphView)
        {
            m_Store = store;
            m_Evt = evt;
            m_Selection = selection;
            m_GraphView = graphView;
        }

        public void BuildContextualMenu()
        {
            var selectedModelsDictionary = m_Selection
                .OfType<IHasGraphElementModel>()
                .Where(x => !(x is BlackboardThisField)) // this blackboard field
                .Distinct(s_HasGraphElementModelComparer)
                .ToDictionary(x => x.GraphElementModel);

            IReadOnlyCollection<IGraphElementModel> selectedModelsKeys = selectedModelsDictionary.Keys.ToList();

            BuildBlackboardContextualMenu();

            var originatesFromBlackboard = (m_Evt.target as VisualElement)?.GetFirstOfType<Blackboard>() != null;
            if (!originatesFromBlackboard || m_Evt.target is IHasGraphElementModel)
            {
                BuildGraphViewContextualMenu();
                if (!originatesFromBlackboard)
                {
                    BuildNodeContextualMenu(selectedModelsDictionary);
                    BuildStackContextualMenu(selectedModelsKeys);
                    BuildEdgeContextualMenu(selectedModelsKeys);
                }

                BuildVariableNodeContextualMenu(selectedModelsKeys);
                if (!originatesFromBlackboard)
                {
                    BuildConstantNodeContextualMenu(selectedModelsKeys);
                    BuildStaticConstantNodeContextualMenu(selectedModelsKeys);
                    BuildPropertyNodeContextualMenu(selectedModelsKeys);
                    BuildSpecialContextualMenu(selectedModelsKeys);
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

            if (originatesFromBlackboard && !(m_Evt.target is IHasGraphElementModel))
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
        }

        void BuildGraphViewContextualMenu()
        {
            if (!(m_Evt.target is GraphView))
                return;

            m_Evt.menu.AppendAction("Create Node", menuAction =>
            {
                m_GraphView.window.DisplaySmartSearch(menuAction);
            });

            m_GraphView.AddContextualMenuEntries(m_Evt);

#if UNITY_2020_1_OR_NEWER
            m_Evt.menu.AppendAction("Create Placemat", menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                Vector2 graphPosition = m_GraphView.contentViewContainer.WorldToLocal(mousePosition);

                m_Store.Dispatch(new CreatePlacematAction(null, new Rect(graphPosition.x, graphPosition.y, 200, 200)));
            });
#endif

            m_Evt.menu.AppendSeparator();

            m_Evt.menu.AppendAction("Cut", menuAction => m_GraphView.InvokeCutSelectionCallback(),
                x => m_GraphView.CanCutSelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_Evt.menu.AppendAction("Copy", menuAction => m_GraphView.InvokeCopySelectionCallback(),
                x => m_GraphView.CanCopySelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_Evt.menu.AppendAction("Paste", menuAction => m_GraphView.InvokePasteCallback(),
                x => m_GraphView.CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void BuildBlackboardContextualMenu()
        {
            // Nothing at the moment.
            //            var blackboard = (m_Evt.target as VisualElement)?.GetFirstOfType<Blackboard>();
            //            if (blackboard == null)
            //                return;
        }

        void BuildNodeContextualMenu(Dictionary<IGraphElementModel, IHasGraphElementModel> selectedModels)
        {
            var selectedModelsKeys = selectedModels.Keys.ToArray();
            if (!selectedModelsKeys.Any())
                return;

            var models = selectedModelsKeys.OfType<NodeModel>().ToArray();
            var connectedModels = models.Where(x => x.InputsByDisplayOrder.Any(y => y.Connected) && x.OutputsByDisplayOrder.Any(y => y.Connected)).ToArray();
            bool canSelectionBeBypassed = connectedModels.Any();

            m_Evt.menu.AppendSeparator();

            m_Evt.menu.AppendAction("Align Item (Q)", menuAction => m_GraphView.AlignSelection(false));
            m_Evt.menu.AppendAction("Align Hierarchy (Shift+Q)", menuAction => m_GraphView.AlignSelection(true));

#if UNITY_2020_1_OR_NEWER
            var content = selectedModels.Values.OfType<GraphElement>().Where(e => (e.parent is GraphView.Layer) && (e is Experimental.GraphView.Node || e is StickyNote)).ToList();
            m_Evt.menu.AppendAction("Create Placemat Under Selection", menuAction =>
            {
                Rect bounds = new Rect();
                if (Experimental.GraphView.Placemat.ComputeElementBounds(ref bounds, content))
                {
                    m_Store.Dispatch(new CreatePlacematAction(null, bounds));
                }
            }, action => (content.Count == 0) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
#endif

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
                m_Store.Dispatch(new DeleteElementsAction(selectedModelsKeys));
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

            //TODO reenable and make it work after 0.3
            //            evt.menu.AppendAction("Documentation...", menuAction =>
            //                {
            //                    m_Store.Dispatch(new OpenDocumentationAction(models));
            //                }, x => DropdownMenuAction.Status.Normal);

#if UNITY_2020_1_OR_NEWER
            var placemats = selectedModelsKeys.OfType<PlacematModel>().ToArray();
            if (models.Any() || placemats.Any())
            {
                m_Evt.menu.AppendAction("Color/Change...", menuAction =>
                {
                    // TODO: make ColorPicker.Show(...) public in trunk
                    Type t = typeof(EditorWindow).Assembly.GetTypes().FirstOrDefault(ty => ty.Name == "ColorPicker");
                    MethodInfo m = t?.GetMethod("Show", new[] { typeof(Action<Color>), typeof(Color), typeof(bool), typeof(bool) });

                    void ChangeNodesColor(Color pickedColor)
                    {
                        foreach (ICustomColor node in selectedModels.Values.OfType<ICustomColor>())
                            node.SetColor(pickedColor);
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

                    m?.Invoke(null, new object[] { (Action<Color>)ChangeNodesColor, defaultColor, true, false});
                }, eventBase => DropdownMenuAction.Status.Normal);

                m_Evt.menu.AppendAction("Color/Reset", menuAction =>
                {
                    foreach (ICustomColor node in selectedModels.Values.OfType<ICustomColor>())
                        node.ResetColor();

                    m_Store.Dispatch(new ResetElementColorAction(models, placemats));
                }, eventBase => DropdownMenuAction.Status.Normal);
            }
            else
            {
                m_Evt.menu.AppendAction("Color", menuAction => {}, eventBase => DropdownMenuAction.Status.Disabled);
            }
#else
            if (models.Any())
            {
                m_Evt.menu.AppendAction("Color/Change...", menuAction =>
                {
                    // TODO: make ColorPicker.Show(...) public in trunk
                    Type t = typeof(EditorWindow).Assembly.GetTypes().FirstOrDefault(ty => ty.Name == "ColorPicker");
                    MethodInfo m = t?.GetMethod("Show", new[] { typeof(Action<Color>), typeof(Color), typeof(bool), typeof(bool) });

                    void ChangeNodesColor(Color pickedColor)
                    {
                        foreach (ICustomColor node in selectedModels.Values.OfType<ICustomColor>())
                            node.SetColor(pickedColor);
                        m_Store.Dispatch(new ChangeElementColorAction(pickedColor, models));
                    }

                    var defaultColor = new Color(0.5f, 0.5f, 0.5f);
                    if (models.Length == 1)
                    {
                        defaultColor = models[0].Color;
                    }

                    m?.Invoke(null, new object[] { (Action<Color>)ChangeNodesColor, defaultColor, true, false });
                }, eventBase => DropdownMenuAction.Status.Normal);

                m_Evt.menu.AppendAction("Color/Reset", menuAction =>
                {
                    foreach (ICustomColor node in selectedModels.Values.OfType<ICustomColor>())
                        node.ResetColor();

                    m_Store.Dispatch(new ResetElementColorAction(models));
                }, eventBase => DropdownMenuAction.Status.Normal);
            }
            else
            {
                m_Evt.menu.AppendAction("Color", menuAction => { }, eventBase => DropdownMenuAction.Status.Disabled);
            }
#endif
        }

        void BuildStackContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
        {
            IStackModel firstModel = selectedModels.Where(x => x is IStackModel).Cast<IStackModel>().FirstOrDefault();

            if (firstModel != null)
            {
                m_Evt.menu.AppendAction("Create Node", menuAction =>
                {
                    m_GraphView.window.DisplaySmartSearch(menuAction);
                }, DropdownMenuAction.AlwaysEnabled);
            }
        }

        void BuildEdgeContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
        {
            IEdgeModel firstMatchingModel = selectedModels
                .OfType<IEdgeModel>()
                .FirstOrDefault(x => x.InputPortModel?.NodeModel is IStackModel &&
                    x.OutputPortModel?.NodeModel is IStackModel);
            if (firstMatchingModel != null)
            {
                m_Evt.menu.AppendAction("Edge/Merge", menuAction => m_Store.Dispatch(new MergeStackAction(
                    (StackBaseModel)firstMatchingModel.OutputPortModel.NodeModel,
                    (StackBaseModel)firstMatchingModel.InputPortModel.NodeModel)),
                    eventBase => DropdownMenuAction.Status.Normal);
            }
        }

        void BuildPropertyNodeContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
        {
            var models = selectedModels.Where(x => x is PropertyGroupBaseNodeModel).Cast<PropertyGroupBaseNodeModel>().ToArray();
            if (!models.Any())
                return;

            if (models.Length == 1)
            {
                m_Evt.menu.AppendAction("Property/Edit Ports", menuAction =>
                {
                    var mousePosition = menuAction.eventInfo.mousePosition;
                    m_GraphView.window.DisplayPropertySearcher(models[0], mousePosition);
                }, DropdownMenuAction.AlwaysEnabled);
            }
        }

        void BuildVariableNodeContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
        {
            IVariableModel[] models = selectedModels.Where(x => x is VariableNodeModel).Cast<IVariableModel>().ToArray();
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

        void BuildConstantNodeContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
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

        void BuildStaticConstantNodeContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
        {
            var models = selectedModels.OfType<ISystemConstantNodeModel>().ToArray();
            if (!models.Any())
                return;

            m_Evt.menu.AppendAction("System Constant/Itemize",
                menuAction => m_Store.Dispatch(new ItemizeSystemConstantNodeAction(models)), x => DropdownMenuAction.Status.Normal);
        }

        void BuildSpecialContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
        {
            var graphElementModels = selectedModels.ToList();
            if (graphElementModels.Count == 2)
            {
                if (graphElementModels.FirstOrDefault(x => x is IEdgeModel) is IEdgeModel edgeModel &&
                    graphElementModels.FirstOrDefault(x => x is INodeModel) is INodeModel nodeModel)
                {
                    m_Evt.menu.AppendAction("Insert", menuAction => m_Store.Dispatch(new SplitEdgeAndInsertNodeAction(edgeModel, nodeModel)),
                        eventBase => DropdownMenuAction.Status.Normal);
                }
            }
        }

        void BuildStickyNoteContextualMenu()
        {
            List<StickyNote> stickyNoteSelection = m_Selection?.OfType<StickyNote>().ToList();
            if (stickyNoteSelection == null || !stickyNoteSelection.Any())
                return;

            List<IStickyNoteModel> stickyNoteModels = stickyNoteSelection.Select(m => (IStickyNoteModel)m.GraphElementModel).ToList();

            foreach (StickyNoteColorTheme value in Enum.GetValues(typeof(StickyNoteColorTheme)))
                m_Evt.menu.AppendAction("Theme/" + value,
                    menuAction => m_Store.Dispatch(new UpdateStickyNoteThemeAction(stickyNoteModels, value)),
                    e => DropdownMenuAction.Status.Normal);

            foreach (StickyNoteTextSize value in Enum.GetValues(typeof(StickyNoteTextSize)))
                m_Evt.menu.AppendAction(value + " Text Size",
                    menuAction => m_Store.Dispatch(new UpdateStickyNoteTextSizeAction(stickyNoteModels, value)),
                    e => DropdownMenuAction.Status.Normal);
        }

        void BuildRefactorContextualMenu(IReadOnlyCollection<IGraphElementModel> selectedModels)
        {
            var models = selectedModels.OfType<INodeModel>().ToArray();
            if (!models.Any())
                return;

            var enableConvertToFunction = true;
            var enableExtractMacro = true;
            var enableExtractFunction = true;
            var canDisable = models.Any();
            var willDisable = models.Any(n => n.State == ModelState.Enabled);

            if (models.Length > 1)
                enableConvertToFunction = false;

            if (models.Any(x => x.IsStacked))
                enableExtractMacro = false;
            else
            {
                enableExtractFunction = false;
                enableConvertToFunction = false;
            }

            if (models.OfType<IStackModel>().Any() || (!m_GraphView.store.GetState().CurrentGraphModel.Stencil.Capabilities.HasFlag(StencilCapabilityFlags.SupportsMacros)))
            {
                enableConvertToFunction = false;
                enableExtractMacro = false;
                enableExtractFunction = false;
            }

            if (enableConvertToFunction || enableExtractMacro || enableExtractFunction)
                m_Evt.menu.AppendSeparator();

            if (enableConvertToFunction)
            {
                m_Evt.menu.AppendAction("Refactor/Convert to Function",
                    _ => m_Store.Dispatch(new RefactorConvertToFunctionAction(models.Single())),
                    _ => DropdownMenuAction.Status.Normal);
            }

            if (canDisable)
                m_Evt.menu.AppendAction(willDisable ? "Disable Selection" : "Enable Selection", menuAction =>
                {
                    m_Store.Dispatch(new SetNodeEnabledStateAction(models, willDisable ? ModelState.Disabled : ModelState.Enabled));
                });


            if (Unsupported.IsDeveloperBuild())
            {
                m_Evt.menu.AppendAction("[DBG] Redefine Node",
                    action =>
                    {
                        foreach (var model in selectedModels.OfType<NodeModel>())
                            model.DefineNode();
                    }, _ => DropdownMenuAction.Status.Normal);

                m_Evt.menu.AppendAction("[DBG] Refresh Element(s)",
                    menuAction => { m_Store.Dispatch(new RefreshUIAction(selectedModels.ToList())); },
                    _ => DropdownMenuAction.Status.Normal);
            }

            if (enableExtractMacro)
            {
                m_Evt.menu.AppendAction("Refactor/Extract Macro",
                    menuAction =>
                    {
                        var rectToFit = new Rect();
                        bool first = true;
                        foreach (var model in models)
                        {
                            GraphElement ge = null;
                            if (m_GraphView.UIController.ModelsToNodeMapping?.TryGetValue(model, out ge) ?? false)
                            {
                                var r = ge.GetPosition();
                                rectToFit = first ? r : RectUtils.Encompass(rectToFit, r);
                                first = false;
                            }
                        }

                        string assetPath = AssetDatabase.GetAssetPath(m_GraphView.store.GetState()?.CurrentGraphModel?.AssetModel as VSGraphAssetModel);
                        string macroPath = null;
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            var assetFolder = Path.GetDirectoryName(assetPath);
                            macroPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(assetFolder ?? "", "MyMacro.asset"));
                        }

                        m_Store.Dispatch(new RefactorExtractMacroAction(m_Selection.OfType<IHasGraphElementModel>().Select(x => x.GraphElementModel).ToList(), rectToFit.center, macroPath));
                    }, x => DropdownMenuAction.Status.Normal);
            }

            if (enableExtractFunction)
            {
                m_Evt.menu.AppendAction("Refactor/Extract Function",
                    menuAction =>
                    {
                        var rectToFit = new Rect();
                        bool first = true;
                        foreach (var model in models)
                        {
                            GraphElement ge = null;
                            if (m_GraphView.UIController.ModelsToNodeMapping?.TryGetValue(model, out ge) ?? false)
                            {
                                var r = ge.GetPosition();
                                rectToFit = first ? r : RectUtils.Encompass(rectToFit, r);
                                first = false;
                            }
                        }

                        m_Store.Dispatch(new RefactorExtractFunctionAction(m_Selection));
                    }, x => DropdownMenuAction.Status.Normal);
            }
        }
    }
}
