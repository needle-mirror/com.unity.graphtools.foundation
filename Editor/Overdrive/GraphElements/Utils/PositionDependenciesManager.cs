using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    interface IDependency
    {
        IGTFNodeModel DependentNode { get; }
    }

    public class PortalNodesDependency : IDependency
    {
        public IGTFNodeModel DependentNode { get; set; }
    }

    public class LinkedNodesDependency : IDependency
    {
        public IGTFPortModel DependentPort;
        public IGTFPortModel ParentPort;
        public IGTFNodeModel DependentNode => DependentPort.NodeModel;
        public int count;
    }

    class PositionDependenciesManager
    {
        const int k_AlignHorizontalOffset = 30;

        readonly GraphView m_GraphView;
        readonly Dictionary<GUID, Dictionary<GUID, IDependency>> m_DependenciesByNode = new Dictionary<GUID, Dictionary<GUID, IDependency>>();
        readonly Dictionary<GUID, Dictionary<GUID, IDependency>> m_PortalDependenciesByNode = new Dictionary<GUID, Dictionary<GUID, IDependency>>();
        readonly HashSet<IGTFNodeModel> m_ModelsToMove = new HashSet<IGTFNodeModel>();
        readonly HashSet<IGTFNodeModel> m_TempMovedModels = new HashSet<IGTFNodeModel>();

        Vector2 m_StartPos;
        Preferences m_Preferences;

        public PositionDependenciesManager(GraphView graphView, Preferences preferences)
        {
            m_GraphView = graphView;
            m_Preferences = preferences;
        }

        void AddEdgeDependency(IGTFNodeModel parent, IDependency child)
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

        internal List<IDependency> GetDependencies(IGTFNodeModel parent)
        {
            if (!m_DependenciesByNode.TryGetValue(parent.Guid, out var link))
                return null;
            return link.Values.ToList();
        }

        internal List<IDependency> GetPortalDependencies(IGTFEdgePortalModel parent)
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
            if (m_Preferences.GetBool(BoolPref.DependenciesLogging))
                Debug.Log(message);
        }

        void ProcessDependency(IGTFNodeModel nodeModel, Vector2 delta, Action<GraphElement, IDependency, Vector2, IGTFNodeModel> dependencyCallback)
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

                var graphElement = dependency.Value.DependentNode.GetUI<Node>(m_GraphView);
                if (graphElement != null)
                    dependencyCallback(graphElement, dependency.Value, delta, nodeModel);
                else
                    Log($"Cannot find ui node for model: {dependency.Value.DependentNode} dependency from {nodeModel}");

                ProcessDependency(dependency.Value.DependentNode, delta, dependencyCallback);
            }
        }

        void ProcessMovedNodes(Vector2 lastMousePosition, Action<GraphElement, IDependency, Vector2, IGTFNodeModel> dependencyCallback)
        {
            Profiler.BeginSample("VS.ProcessMovedNodes");

            m_TempMovedModels.Clear();
            Vector2 delta = lastMousePosition - m_StartPos;
            foreach (IGTFNodeModel nodeModel in m_ModelsToMove)
                ProcessDependency(nodeModel, delta, dependencyCallback);

            Profiler.EndSample();
        }

        public void UpdateNodeState()
        {
            HashSet<GUID> processed = new HashSet<GUID>();
            void ProcessDependency(IGTFNodeModel nodeModel, ModelState state)
            {
                if (nodeModel.State == ModelState.Disabled)
                    state = ModelState.Disabled;

                var nodeUI = nodeModel.GetUI<Node>(m_GraphView);
                if (nodeUI != null && state == ModelState.Enabled)
                {
                    nodeUI.EnableInClassList(Node.k_DisabledModifierUssClassName, false);
                    nodeUI.EnableInClassList(Node.k_UnusedModifierUssClassName, false);
                }

                Dictionary<GUID, IDependency> dependencies = null;

                if (nodeModel is IGTFEdgePortalModel edgePortalModel)
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

            m_GraphView.Nodes.ForEach(node =>
            {
                var nodeModel = node.NodeModel;
                if (nodeModel.State == ModelState.Disabled)
                {
                    node.EnableInClassList(Node.k_DisabledModifierUssClassName, true);
                    node.EnableInClassList(Node.k_UnusedModifierUssClassName, false);
                }
                else
                {
                    node.EnableInClassList(Node.k_DisabledModifierUssClassName, false);
                    node.EnableInClassList(Node.k_UnusedModifierUssClassName, true);
                }
            });

            var graphModel = m_GraphView.Store.GetState().CurrentGraphModel;
            foreach (var root in graphModel.Stencil.GetEntryPoints(graphModel))
            {
                ProcessDependency(root, ModelState.Enabled);
            }
        }

        public void ProcessMovedNodes(Vector2 lastMousePosition)
        {
            ProcessMovedNodes(lastMousePosition, OffsetDependency);
        }

        static void OffsetDependency(GraphElement element, IDependency model, Vector2 delta, IGTFNodeModel _)
        {
            Vector2 prevPos = model.DependentNode.Position;
            Rect posRect = element.GetPosition();
            posRect.position = prevPos + delta;
            element.SetPosition(posRect);
        }

        IGTFGraphModel m_GraphModel;

        public void StartNotifyMove(List<ISelectableGraphElement> selection, Vector2 lastMousePosition)
        {
            m_StartPos = lastMousePosition;
            m_ModelsToMove.Clear();
            m_GraphModel = null;

            foreach (var element in selection.OfType<IGraphElement>())
            {
                if (element.Model is IGTFNodeModel nodeModel)
                {
                    if (m_GraphModel == null)
                        m_GraphModel = nodeModel.GraphModel;
                    else
                        Assert.AreEqual(nodeModel.GraphModel, m_GraphModel);
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
            EditorUtility.SetDirty((Object)m_GraphModel.AssetModel);
            m_ModelsToMove.Clear();
        }

        void AlignDependency(GraphElement element, IDependency dependency, Vector2 _, IGTFNodeModel prev)
        {
            // Warning: Don't try to use the VisualElement.layout Rect as it is not up to date yet.
            // Use Node.GetPosition() when possible

            var parentUI = prev.GetUI<Node>(m_GraphView);
            var depUI = dependency.DependentNode.GetUI<Node>(m_GraphView);

            if (parentUI == null || depUI == null)
                return;

            switch (dependency)
            {
                case LinkedNodesDependency linked:

                    Vector2 position;

                    VisualElement input = linked.ParentPort.GetUI<Port>(m_GraphView);
                    VisualElement output = linked.DependentPort.GetUI<Port>(m_GraphView);

                    if (input != null && output != null)
                    {
                        Vector2 inputPortPos = input.parent.ChangeCoordinatesTo(parentUI, input.layout.center);
                        Vector2 inputPos = prev.Position;
                        Vector2 outputPortPos = output.parent.ChangeCoordinatesTo(depUI, output.layout.center);

                        position = new Vector2(
                            prev.Position.x + (linked.ParentPort.Direction == Direction.Output
                                ? parentUI.layout.width + k_AlignHorizontalOffset
                                : -k_AlignHorizontalOffset - depUI.layout.width),
                            inputPos.y + inputPortPos.y - outputPortPos.y
                        );
                        Log($"  pos {position} parent NOT stackNode");

                        Undo.RegisterCompleteObjectUndo((Object)linked.DependentNode.GraphModel.AssetModel, "Align node");
                        linked.DependentNode.Position = position;
                        element.SetPosition(new Rect(position, element.layout.size));
                    }
                    break;
            }
        }

        public void AlignNodes(GraphView graphView, bool follow, List<ISelectableGraphElement> selection)
        {
            HashSet<IGTFNodeModel> topMostModels = new HashSet<IGTFNodeModel>();

            topMostModels.Clear();

            bool anyEdge = false;
            foreach (Edge edge in selection.OfType<Edge>())
            {
                if (!edge.EdgeModel.GraphModel.Stencil.CreateDependencyFromEdge(edge.EdgeModel, out LinkedNodesDependency dependency, out IGTFNodeModel parent))
                    continue;
                anyEdge = true;

                GraphElement element = dependency.DependentNode.GetUI<Node>(graphView);
                AlignDependency(element, dependency, Vector2.zero, parent);
                topMostModels.Add(dependency.DependentNode);
            }

            if (anyEdge && !follow)
                return;

            if (!topMostModels.Any())
            {
                foreach (GraphElement element in selection.OfType<GraphElement>())
                {
                    if (element.Model is IGTFNodeModel nodeModel)
                    {
                        topMostModels.Add(nodeModel);
                    }
                }
            }

            if (!anyEdge && !follow)
            {
                // Align each top-most node then move dependencies by the same delta
                foreach (IGTFNodeModel model in topMostModels)
                {
                    if (!m_DependenciesByNode.TryGetValue(model.Guid, out Dictionary<GUID, IDependency> dependencies))
                        continue;
                    foreach (KeyValuePair<GUID, IDependency> dependency in dependencies)
                    {
                        IGTFNodeModel dependentNode = dependency.Value.DependentNode;
                        GraphElement element = dependentNode.GetUI<Node>(graphView);
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

        void OffsetNodeDependencies(IGTFNodeModel dependentNode, Vector2 delta)
        {
            Log($"Moving all dependencies of {dependentNode.GetType().Name} by {delta}");

            m_TempMovedModels.Clear();
            m_ModelsToMove.Clear();
            m_ModelsToMove.Add(dependentNode);
            ProcessDependency(dependentNode, delta, OffsetDependency);
        }

        public void AddPositionDependency(IGTFEdgeModel model)
        {
            if (!model.GraphModel.Stencil.CreateDependencyFromEdge(model, out var dependency, out IGTFNodeModel parent))
                return;
            AddEdgeDependency(parent, dependency);
            LogDependencies();
        }

        public void AddPortalDependency(IGTFEdgePortalModel model)
        {
            var stencil = model.GraphModel.Stencil;

            // Update all portals linked to this portal definition.
            foreach (var portalModel in stencil.GetLinkedPortals(model))
            {
                m_PortalDependenciesByNode[portalModel.Guid] =
                    stencil.GetPortalDependencies(portalModel)
                        .ToDictionary(p => p.Guid, p => (IDependency) new PortalNodesDependency {DependentNode = p});
            }
            LogDependencies();
        }

        public void RemovePortalDependency(IGTFNodeModel model)
        {
            foreach (var dependencies in m_PortalDependenciesByNode.Values)
            {
                dependencies.Remove(model.Guid);
            }

            m_PortalDependenciesByNode.Remove(model.Guid);
        }
    }
}
