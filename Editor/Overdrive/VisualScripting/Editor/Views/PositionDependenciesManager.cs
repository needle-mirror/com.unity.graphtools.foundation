using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    interface IDependency
    {
        INodeModel DependentNode { get; }
    }

    public class PortalNodesDependency : IDependency
    {
        public INodeModel DependentNode { get; set; }
    }

    public class LinkedNodesDependency : IDependency
    {
        public IPortModel DependentPort;
        public IPortModel ParentPort;
        public INodeModel DependentNode => DependentPort.NodeModel;
        public int count;
    }

    class PositionDependenciesManager
    {
        const int k_AlignHorizontalOffset = 30;

        readonly VseGraphView m_VseGraphView;
        readonly Dictionary<GUID, Dictionary<GUID, IDependency>> m_DependenciesByNode = new Dictionary<GUID, Dictionary<GUID, IDependency>>();
        readonly Dictionary<GUID, Dictionary<GUID, IDependency>> m_PortalDependenciesByNode = new Dictionary<GUID, Dictionary<GUID, IDependency>>();
        readonly HashSet<INodeModel> m_ModelsToMove = new HashSet<INodeModel>();
        readonly HashSet<INodeModel> m_TempMovedModels = new HashSet<INodeModel>();

        Vector2 m_StartPos;
        List<KeyValuePair<Node, Rect>> m_ScheduledItems = new List<KeyValuePair<Node, Rect>>();
        VSPreferences m_Preferences;
        public Dictionary<PortModel, GraphElements.Port> portModelToPort;

        public PositionDependenciesManager(VseGraphView vseGraphView, VSPreferences vsPreferences)
        {
            m_VseGraphView = vseGraphView;
            m_Preferences = vsPreferences;
        }

        void AddEdgeDependency(INodeModel parent, IDependency child)
        {
            if (!m_DependenciesByNode.TryGetValue(parent.Guid, out Dictionary<GUID, IDependency> link))
                m_DependenciesByNode.Add(parent.Guid, new Dictionary<GUID, IDependency> { {child.DependentNode.Guid, child }});
            else
            {
                if (link.TryGetValue(child.DependentNode.Guid, out IDependency dependency))
                {
                    if (dependency is LinkedNodesDependency linked)
                        linked.count++;
                    else
                        Debug.LogWarning($"Dependency between nodes {parent} && {child.DependentNode} registered both as a {dependency.GetType().Name} and a {nameof(LinkedNodesDependency)}");
                }
                else
                {
                    link.Add(child.DependentNode.Guid, child);
                }
            }
        }

        internal List<IDependency> GetDependencies(INodeModel parent)
        {
            if (!m_DependenciesByNode.TryGetValue(parent.Guid, out var link))
                return null;
            return link.Values.ToList();
        }

        internal List<IDependency> GetPortalDependencies(IEdgePortalModel parent)
        {
            if (!m_PortalDependenciesByNode.TryGetValue(parent.Guid, out var link))
                return null;
            return link.Values.ToList();
        }

        public void Remove(GUID a, GUID b)
        {
            GUID parent;
            GUID child;
            if (m_DependenciesByNode.TryGetValue(a, out var link) &&
                link.TryGetValue(b, out var dependency))
            {
                parent = a;
                child = b;
            }
            else if (m_DependenciesByNode.TryGetValue(b, out link) &&
                     link.TryGetValue(a, out dependency))
            {
                parent = b;
                child = a;
            }
            else
                return;

            if (dependency is LinkedNodesDependency linked)
            {
                linked.count--;
                if (linked.count <= 0)
                    link.Remove(child);
            }
            else
                link.Remove(child);
            if (link.Count == 0)
                m_DependenciesByNode.Remove(parent);
        }

        public void Clear()
        {
            foreach (KeyValuePair<GUID, Dictionary<GUID, IDependency>> pair in m_DependenciesByNode)
                pair.Value.Clear();
            m_DependenciesByNode.Clear();

            foreach (KeyValuePair<GUID, Dictionary<GUID, IDependency>> pair in m_PortalDependenciesByNode)
                pair.Value.Clear();
            m_PortalDependenciesByNode.Clear();
        }

        public void LogDependencies()
        {
            Log("Dependencies :" + String.Join("\r\n", m_DependenciesByNode.Select(n =>
            {
                var s = String.Join(",", n.Value.Select(p => p.Key));
                return $"{n.Key}: {s}";
            })));

            Log("Portal Dependencies :" + String.Join("\r\n", m_PortalDependenciesByNode.Select(n =>
            {
                var s = String.Join(",", n.Value.Select(p => p.Key));
                return $"{n.Key}: {s}";
            })));
        }

        void Log(string message)
        {
            if (m_Preferences.GetBool(VSPreferences.BoolPref.DependenciesLogging))
                Debug.Log(message);
        }

        void ProcessDependency(INodeModel nodeModel, Vector2 delta, Action<GraphElement, IDependency, Vector2, INodeModel> dependencyCallback)
        {
            Log($"ProcessDependency {nodeModel}");

            if (!m_DependenciesByNode.TryGetValue(nodeModel.Guid, out Dictionary<GUID, IDependency> link))
                return;

            foreach (KeyValuePair<GUID, IDependency> dependency in link)
            {
                if (m_ModelsToMove.Contains(dependency.Value.DependentNode))
                    continue;
                if (!m_TempMovedModels.Add(dependency.Value.DependentNode))
                {
                    Log($"Skip ProcessDependency {dependency.Value.DependentNode}");
                    continue;
                }

                if (m_VseGraphView.UIController.ModelsToNodeMapping.TryGetValue(dependency.Value.DependentNode, out var graphElement))
                    dependencyCallback(graphElement, dependency.Value, delta, nodeModel);
                else
                    Log($"Cannot find ui node for model: {dependency.Value.DependentNode} dependency from {nodeModel}");

                ProcessDependency(dependency.Value.DependentNode, delta, dependencyCallback);
            }
        }

        void ProcessMovedNodes(Vector2 lastMousePosition, Action<GraphElement, IDependency, Vector2, INodeModel> dependencyCallback)
        {
            Profiler.BeginSample("VS.ProcessMovedNodes");

            m_TempMovedModels.Clear();
            Vector2 delta = lastMousePosition - m_StartPos;
            foreach (INodeModel nodeModel in m_ModelsToMove)
                ProcessDependency(nodeModel, delta, dependencyCallback);

            Profiler.EndSample();
        }

        public void UpdateNodeState(Dictionary<IGraphElementModel, GraphElement> modelsToNodeMapping)
        {
            HashSet<GUID> processed = new HashSet<GUID>();
            void ProcessDependency(INodeModel nodeModel, ModelState state)
            {
                if (nodeModel.State == ModelState.Disabled)
                    state = ModelState.Disabled;
                if (modelsToNodeMapping.TryGetValue(nodeModel, out var nodeUI) &&
                    nodeUI is INodeState nodeState &&
                    state == ModelState.Enabled)
                    nodeState.UIState = NodeUIState.Enabled;

                Dictionary<GUID, IDependency> dependencies = null;

                if (nodeModel is IEdgePortalModel edgePortalModel)
                    m_PortalDependenciesByNode.TryGetValue(edgePortalModel.Guid, out dependencies);

                if ((dependencies == null || !dependencies.Any()) &&
                    !m_DependenciesByNode.TryGetValue(nodeModel.Guid, out dependencies))
                    return;

                foreach (var dependency in dependencies)
                {
                    if (processed.Add(dependency.Key))
                        ProcessDependency(dependency.Value.DependentNode, state);
                }
            }

            foreach (var node in modelsToNodeMapping.Values.OfType<INodeState>())
            {
                node.UIState = node.GraphElementModel is INodeModel nodeModel && nodeModel.State == ModelState.Disabled ? NodeUIState.Disabled : NodeUIState.Unused;
            }

            foreach (var root in ((VSGraphModel)m_VseGraphView.store.GetState().CurrentGraphModel).GetEntryPoints())
            {
                ProcessDependency(root, ModelState.Enabled);
            }

            foreach (var node in modelsToNodeMapping.Values.OfType<INodeState>())
            {
                node.ApplyNodeState();
            }
        }

        public void ProcessMovedNodes(Vector2 lastMousePosition)
        {
            ProcessMovedNodes(lastMousePosition, OffsetDependency);
        }

        static void OffsetDependency(GraphElement element, IDependency model, Vector2 delta, INodeModel _)
        {
            Vector2 prevPos = model.DependentNode.Position;
            Rect posRect = element.GetPosition();
            posRect.position = prevPos + delta;
            element.SetPosition(posRect);
        }

        VSGraphModel m_GraphModel;

        public void StartNotifyMove(List<ISelectableGraphElement> selection, Vector2 lastMousePosition)
        {
            m_StartPos = lastMousePosition;
            m_ModelsToMove.Clear();
            m_GraphModel = null;

            foreach (GraphElement element in selection.OfType<GraphElement>())
            {
                if (element is IHasGraphElementModel hasModel &&
                    hasModel.GraphElementModel is INodeModel nodeModel)
                {
                    if (m_GraphModel == null)
                        m_GraphModel = (VSGraphModel)nodeModel.VSGraphModel;
                    else
                        Assert.AreEqual(nodeModel.VSGraphModel, m_GraphModel);
                    m_ModelsToMove.Add(nodeModel);
                }
            }
        }

        public void CancelMove()
        {
            ProcessMovedNodes(Vector2.zero, (element, model, _, __) =>
            {
                element.SetPosition(new Rect(model.DependentNode.Position, Vector2.zero));
            });
            m_ModelsToMove.Clear();
        }

        public void StopNotifyMove()
        {
            // case when drag and dropping a declaration to the graph
            if (m_GraphModel == null)
                return;

            Undo.RegisterCompleteObjectUndo((Object)m_GraphModel.AssetModel, "Move dependency");

            ProcessMovedNodes(Vector2.zero, (element, model, _, __) =>
            {
                model.DependentNode.Position = element.GetPosition().position;
            });
            m_GraphModel.AssetModel.SetAssetDirty();
            m_ModelsToMove.Clear();
        }

        public void ScheduleStackedNodesRelayout(IReadOnlyCollection<Node> elements, out bool firstElementScheduled)
        {
            firstElementScheduled = m_ScheduledItems.Count == 0;
            foreach (Node element in elements)
                m_ScheduledItems.Add(new KeyValuePair<Node, Rect>(element, element.layout));
        }

        public void RelayoutScheduledStackedNodes()
        {
            m_TempMovedModels.Clear();
            foreach (KeyValuePair<Node, Rect> pair in m_ScheduledItems)
            {
                VisualElement hierarchyParent = pair.Key.hierarchy.parent;
                if (hierarchyParent != null)
                {
                    Rect cur = hierarchyParent.ChangeCoordinatesTo(m_VseGraphView.contentViewContainer, pair.Key.layout);

                    Vector2 d = cur.position - pair.Value.position;
                    ProcessDependency(pair.Key.NodeModel, d, (graphElement, model, delta, _) =>
                    {
                        Vector2 prevPos = model.DependentNode.Position;
                        Rect posRect = graphElement.GetPosition();
                        posRect.position = prevPos + delta;
                        graphElement.SetPosition(posRect);
                        model.DependentNode.Position = posRect.position;
                    });
                }
            }

            m_ScheduledItems.Clear();
        }

        void AlignDependency(GraphElement element, IDependency dependency, Vector2 _, INodeModel prev)
        {
            // Warning: Don't try to use the VisualElement.layout Rect as it is not up to date yet.
            // Use Node.GetPosition() when possible

            var parentUI = (GraphElements.Node)m_VseGraphView.UIController.ModelsToNodeMapping[prev];
            var depUI = (GraphElements.Node)m_VseGraphView.UIController.ModelsToNodeMapping[dependency.DependentNode];

            switch (dependency)
            {
                case LinkedNodesDependency linked:

                    Vector2 position;

                    VisualElement input = portModelToPort[(PortModel)linked.ParentPort];

                    Vector2 inputPortPos = input.parent.ChangeCoordinatesTo(parentUI, input.layout.center);
                    Vector2 inputPos = prev.Position;
                    VisualElement output = portModelToPort[(PortModel)linked.DependentPort];
                    Vector2 outputPortPos = output.parent.ChangeCoordinatesTo(depUI, output.layout.center);

                    position = new Vector2(
                        prev.Position.x + (linked.ParentPort.Direction == Direction.Output
                            ? parentUI.layout.width + k_AlignHorizontalOffset
                            : -k_AlignHorizontalOffset - depUI.layout.width),
                        inputPos.y + inputPortPos.y - outputPortPos.y
                    );
                    Log($"  pos {position} parent NOT stackNode");

                    Undo.RegisterCompleteObjectUndo(linked.DependentNode.SerializableAsset, "Align node");
                    linked.DependentNode.Position = position;
                    element.SetPosition(new Rect(position, element.layout.size));
                    break;
            }
        }

        public void AlignNodes(VseGraphView vseGraphView, bool follow, List<ISelectableGraphElement> selection)
        {
            HashSet<INodeModel> topMostModels = new HashSet<INodeModel>();

            topMostModels.Clear();

            bool anyEdge = false;
            foreach (Edge edge in selection.OfType<Edge>())
            {
                if (!edge.GraphElementModel.VSGraphModel.Stencil.CreateDependencyFromEdge(edge.VSEdgeModel, out LinkedNodesDependency dependency, out INodeModel parent))
                    continue;
                anyEdge = true;

                GraphElement element = vseGraphView.UIController.ModelsToNodeMapping[dependency.DependentNode];
                AlignDependency(element, dependency, Vector2.zero, parent);
                topMostModels.Add(dependency.DependentNode);
            }

            if (anyEdge && !follow)
                return;

            if (!topMostModels.Any())
            {
                foreach (GraphElement element in selection.OfType<GraphElement>())
                {
                    if (element is IHasGraphElementModel hasModel &&
                        hasModel.GraphElementModel is INodeModel nodeModel)
                    {
                        topMostModels.Add(nodeModel);
                    }
                }
            }

            if (!anyEdge && !follow)
            {
                // Align each top-most node then move dependencies by the same delta
                foreach (INodeModel model in topMostModels)
                {
                    if (!m_DependenciesByNode.TryGetValue(model.Guid, out Dictionary<GUID, IDependency> dependencies))
                        continue;
                    foreach (KeyValuePair<GUID, IDependency> dependency in dependencies)
                    {
                        INodeModel dependentNode = dependency.Value.DependentNode;
                        GraphElement element = vseGraphView.UIController.ModelsToNodeMapping[dependentNode];
                        Vector2 startPos = dependentNode.Position;
                        AlignDependency(element, dependency.Value, Vector2.zero, model);
                        Vector2 endPos = dependentNode.Position;
                        Vector2 delta = endPos - startPos;

                        OffsetNodeDependencies(dependentNode, delta);
                    }
                }
            }
            else
            {
                // Align recursively
                m_ModelsToMove.AddRange(topMostModels);
                ProcessMovedNodes(Vector2.zero, AlignDependency);
            }

            m_ModelsToMove.Clear();
            m_TempMovedModels.Clear();
        }

        void OffsetNodeDependencies(INodeModel dependentNode, Vector2 delta)
        {
            Log($"Moving all dependencies of {dependentNode.GetType().Name} by {delta}");

            m_TempMovedModels.Clear();
            m_ModelsToMove.Clear();
            m_ModelsToMove.Add(dependentNode);
            ProcessDependency(dependentNode, delta, OffsetDependency);
        }

        public void AddPositionDependency(IEdgeModel model)
        {
            if (!model.VSGraphModel.Stencil.CreateDependencyFromEdge(model, out var dependency, out INodeModel parent))
                return;
            AddEdgeDependency(parent, dependency);
            LogDependencies();
        }

        public void AddPortalDependency(IEdgePortalModel model)
        {
            var stencil = model.VSGraphModel.Stencil;

            // Update all portals linked to this portal definition.
            foreach (var portalModel in ((VariableDeclarationModel)model.DeclarationModel).FindReferencesInGraph().OfType<IEdgePortalModel>())
            {
                m_PortalDependenciesByNode[portalModel.Guid] =
                    stencil.GetPortalDependencies(portalModel)
                        .ToDictionary(p => p.Guid, p => (IDependency) new PortalNodesDependency {DependentNode = p});
            }
            LogDependencies();
        }

        public void RemovePortalDependency(IEdgePortalModel model)
        {
            foreach (var dependencies in m_PortalDependenciesByNode.Values)
            {
                dependencies.Remove(model.Guid);
            }

            m_PortalDependenciesByNode.Remove(model.Guid);
        }
    }
}
