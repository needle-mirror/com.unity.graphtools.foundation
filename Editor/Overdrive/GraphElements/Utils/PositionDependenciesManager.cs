using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
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

        readonly GraphView m_GraphView;
        readonly Dictionary<GUID, Dictionary<GUID, IDependency>> m_DependenciesByNode = new Dictionary<GUID, Dictionary<GUID, IDependency>>();
        readonly Dictionary<GUID, Dictionary<GUID, IDependency>> m_PortalDependenciesByNode = new Dictionary<GUID, Dictionary<GUID, IDependency>>();
        readonly HashSet<INodeModel> m_ModelsToMove = new HashSet<INodeModel>();
        readonly HashSet<INodeModel> m_TempMovedModels = new HashSet<INodeModel>();

        Vector2 m_StartPos;
        Preferences m_Preferences;

        public PositionDependenciesManager(GraphView graphView, Preferences preferences)
        {
            m_GraphView = graphView;
            m_Preferences = preferences;
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
            if (m_Preferences?.GetBool(BoolPref.DependenciesLogging) ?? false)
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

                var graphElement = dependency.Value.DependentNode.GetUI<Node>(m_GraphView);
                if (graphElement != null)
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

        void ProcessDependencyModel(INodeModel nodeModel, Action<IDependency, INodeModel> dependencyCallback)
        {
            Log($"ProcessDependencyModel {nodeModel}");

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

                dependencyCallback(dependency.Value, nodeModel);
                ProcessDependencyModel(dependency.Value.DependentNode, dependencyCallback);
            }
        }

        void ProcessMovedNodeModels(Action<IDependency, INodeModel> dependencyCallback)
        {
            Profiler.BeginSample("VS.ProcessMovedNodeModel");

            m_TempMovedModels.Clear();
            foreach (INodeModel nodeModel in m_ModelsToMove)
                ProcessDependencyModel(nodeModel, dependencyCallback);

            Profiler.EndSample();
        }

        public void UpdateNodeState()
        {
            HashSet<GUID> processed = new HashSet<GUID>();
            void SetNodeState(INodeModel nodeModel, ModelState state)
            {
                if (nodeModel.State == ModelState.Disabled)
                    state = ModelState.Disabled;

                var nodeUI = nodeModel.GetUI<Node>(m_GraphView);
                if (nodeUI != null && state == ModelState.Enabled)
                {
                    nodeUI.EnableInClassList(Node.disabledModifierUssClassName, false);
                    nodeUI.EnableInClassList(Node.unusedModifierUssClassName, false);
                }

                Dictionary<GUID, IDependency> dependencies = null;

                if (nodeModel is IEdgePortalModel edgePortalModel)
                    m_PortalDependenciesByNode.TryGetValue(edgePortalModel.Guid, out dependencies);

                if ((dependencies == null || !dependencies.Any()) &&
                    !m_DependenciesByNode.TryGetValue(nodeModel.Guid, out dependencies))
                    return;

                foreach (var dependency in dependencies)
                {
                    if (processed.Add(dependency.Key))
                        SetNodeState(dependency.Value.DependentNode, state);
                }
            }

            m_GraphView.Nodes.ForEach(node =>
            {
                var nodeModel = node.NodeModel;
                if (nodeModel.State == ModelState.Disabled)
                {
                    node.EnableInClassList(Node.disabledModifierUssClassName, true);
                    node.EnableInClassList(Node.unusedModifierUssClassName, false);
                }
                else
                {
                    node.EnableInClassList(Node.disabledModifierUssClassName, false);
                    node.EnableInClassList(Node.unusedModifierUssClassName, true);
                }
            });

            var graphModel = m_GraphView.Store.State.GraphModel;
            foreach (var root in graphModel.Stencil.GetEntryPoints(graphModel))
            {
                SetNodeState(root, ModelState.Enabled);
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

        IGraphModel m_GraphModel;

        public void StartNotifyMove(List<ISelectableGraphElement> selection, Vector2 lastMousePosition)
        {
            m_StartPos = lastMousePosition;
            m_ModelsToMove.Clear();
            m_GraphModel = null;

            foreach (var element in selection.OfType<IGraphElement>())
            {
                if (element.Model is INodeModel nodeModel)
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

            if (m_GraphModel?.AssetModel != null)
            {
                Undo.RegisterCompleteObjectUndo((Object)m_GraphModel.AssetModel, "Move dependency");
                EditorUtility.SetDirty((Object)m_GraphModel.AssetModel);
            }

            ProcessMovedNodes(Vector2.zero, (element, model, _, __) =>
            {
                model.DependentNode.Position = element.GetPosition().position;
            });

            m_ModelsToMove.Clear();
        }

        void AlignDependency(IDependency dependency, INodeModel prev)
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

                        linked.DependentNode.Position = position;
                        m_GraphView.Store.State.MarkChanged(linked.DependentNode);
                    }
                    break;
            }
        }

        public void AlignNodes(bool follow, IReadOnlyList<IGraphElementModel> entryPoints)
        {
            HashSet<INodeModel> topMostModels = new HashSet<INodeModel>();

            topMostModels.Clear();

            bool anyEdge = false;
            foreach (var edgeModel in entryPoints.OfType<IEdgeModel>())
            {
                if (!edgeModel.GraphModel.Stencil.CreateDependencyFromEdge(edgeModel, out LinkedNodesDependency dependency, out INodeModel parent))
                    continue;
                anyEdge = true;

                AlignDependency(dependency, parent);
                topMostModels.Add(dependency.DependentNode);
            }

            if (anyEdge && !follow)
                return;

            if (!topMostModels.Any())
            {
                foreach (var nodeModel in entryPoints.OfType<INodeModel>())
                {
                    topMostModels.Add(nodeModel);
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
                        AlignDependency(dependency.Value, model);
                    }
                }
            }
            else
            {
                // Align recursively
                m_ModelsToMove.AddRange(topMostModels);
                ProcessMovedNodeModels(AlignDependency);
            }

            m_ModelsToMove.Clear();
            m_TempMovedModels.Clear();
        }

        public void AddPositionDependency(IEdgeModel model)
        {
            if (!model.GraphModel.Stencil.CreateDependencyFromEdge(model, out var dependency, out INodeModel parent))
                return;
            AddEdgeDependency(parent, dependency);
            LogDependencies();
        }

        public void AddPortalDependency(IEdgePortalModel model)
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

        public void RemovePortalDependency(INodeModel model)
        {
            foreach (var dependencies in m_PortalDependenciesByNode.Values)
            {
                dependencies.Remove(model.Guid);
            }

            m_PortalDependenciesByNode.Remove(model.Guid);
        }
    }
}
