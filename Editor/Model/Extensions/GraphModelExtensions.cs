using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Verbosity of <see cref="GraphModelExtensions.CheckIntegrity"/>.
    /// </summary>
    public enum Verbosity
    {
        Errors,
        Verbose
    }

    /// <summary>
    /// Extension methods for <see cref="IGraphModel"/>.
    /// </summary>
    public static class GraphModelExtensions
    {
        static readonly Vector2 k_PortalOffset = Vector2.right * 150;

        public static IEnumerable<IHasDeclarationModel> FindReferencesInGraph(this IGraphModel self, IDeclarationModel variableDeclarationModel)
        {
            return self.NodeModels.OfType<IHasDeclarationModel>().Where(v => v.DeclarationModel != null && variableDeclarationModel.Guid == v.DeclarationModel.Guid);
        }

        public static IEnumerable<T> FindReferencesInGraph<T>(this IGraphModel self, IDeclarationModel variableDeclarationModel) where T : IHasDeclarationModel
        {
            return self.FindReferencesInGraph(variableDeclarationModel).OfType<T>();
        }

        public static IEnumerable<IPortModel> GetPortModels(this IGraphModel self)
        {
            return self.NodeModels.OfType<IPortNodeModel>().SelectMany(nodeModel => nodeModel.Ports);
        }

        /// <summary>
        /// Creates a new node in a graph.
        /// </summary>
        /// <param name="self">The graph to add a node to.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <typeparam name="TNodeType">The type of the new node to create.</typeparam>
        /// <returns>The newly created node.</returns>
        public static TNodeType CreateNode<TNodeType>(this IGraphModel self, string nodeName = "", Vector2 position = default,
            SerializableGUID guid = default, Action<TNodeType> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
            where TNodeType : class, INodeModel
        {
            Action<INodeModel> setupWrapper = null;
            if (initializationCallback != null)
            {
                setupWrapper = n => initializationCallback.Invoke(n as TNodeType);
            }

            return (TNodeType)self.CreateNode(typeof(TNodeType), nodeName, position, guid, setupWrapper, spawnFlags);
        }

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="self">The graph to add a variable declaration to.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <typeparam name="TDeclType">The type of variable declaration to create.</typeparam>
        /// <returns>The newly created variable declaration.</returns>
        public static TDeclType CreateGraphVariableDeclaration<TDeclType>(this IGraphModel self, TypeHandle variableDataType,
            string variableName, ModifierFlags modifierFlags, bool isExposed, IConstant initializationModel = null,
            SerializableGUID guid = default, Action<TDeclType, IConstant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.Default)
            where TDeclType : class, IVariableDeclarationModel
        {
            return (TDeclType)self.CreateGraphVariableDeclaration(typeof(TDeclType), variableDataType, variableName,
                modifierFlags, isExposed, initializationModel, guid, (d, c) => initializationCallback?.Invoke((TDeclType)d, c), spawnFlags);
        }

        public static IEdgePortalModel CreateOppositePortal(this IGraphModel self, IEdgePortalModel edgePortalModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var offset = Vector2.zero;
            switch (edgePortalModel)
            {
                case IEdgePortalEntryModel _:
                    offset = k_PortalOffset;
                    break;
                case IEdgePortalExitModel _:
                    offset = -k_PortalOffset;
                    break;
            }
            var currentPos = edgePortalModel.Position;
            return self.CreateOppositePortal(edgePortalModel, currentPos + offset, spawnFlags);
        }

        public static IReadOnlyCollection<IGraphElementModel> DeleteVariableDeclaration(this IGraphModel self,
            IVariableDeclarationModel variableDeclarationToDelete, bool deleteUsages)
        {
            return self.DeleteVariableDeclarations(new[] { variableDeclarationToDelete }, deleteUsages);
        }

        public static IReadOnlyCollection<IGraphElementModel> DeleteNode(this IGraphModel self, INodeModel nodeToDelete, bool deleteConnections)
        {
            return self.DeleteNodes(new[] { nodeToDelete }, deleteConnections);
        }

        public static IReadOnlyCollection<IGraphElementModel> DeleteEdge(this IGraphModel self, IEdgeModel edgeToDelete)
        {
            return self.DeleteEdges(new[] { edgeToDelete });
        }

        public static IReadOnlyCollection<IGraphElementModel> DeleteStickyNote(this IGraphModel self, IStickyNoteModel stickyNoteToDelete)
        {
            return self.DeleteStickyNotes(new[] { stickyNoteToDelete });
        }

        public static IReadOnlyCollection<IGraphElementModel> DeletePlacemat(this IGraphModel self, IPlacematModel placematToDelete)
        {
            return self.DeletePlacemats(new[] { placematToDelete });
        }

        public static IEnumerable<IGraphElementModel> DeleteElements(this IGraphModel self, IReadOnlyCollection<IGraphElementModel> graphElementModels)
        {
            var stickyNoteModels = new HashSet<IStickyNoteModel>();
            var placematModels = new HashSet<IPlacematModel>();
            var variableDeclarationsModels = new HashSet<IVariableDeclarationModel>();
            var edgeModels = new HashSet<IEdgeModel>();
            var nodeModels = new HashSet<INodeModel>();

            foreach (var element in graphElementModels)
            {
                switch (element)
                {
                    case IStickyNoteModel stickyNoteModel:
                        stickyNoteModels.Add(stickyNoteModel);
                        break;
                    case IPlacematModel placematModel:
                        placematModels.Add(placematModel);
                        break;
                    case IVariableDeclarationModel variableDeclarationModel:
                        variableDeclarationsModels.Add(variableDeclarationModel);
                        break;
                    case IEdgeModel edgeModel:
                        edgeModels.Add(edgeModel);
                        break;
                    case INodeModel nodeModel:
                        nodeModels.Add(nodeModel);
                        break;
                }
            }

            // Add nodes that would be backed by declaration models.
            nodeModels.AddRangeInternal(variableDeclarationsModels.SelectMany(d => self.FindReferencesInGraph<IHasDeclarationModel>(d).OfType<INodeModel>()));

            // Add edges connected to the deleted nodes.
            foreach (var portModel in nodeModels.OfType<IPortNodeModel>().SelectMany(n => n.Ports))
                edgeModels.AddRangeInternal(self.EdgeModels.Where(e => e.ToPort == portModel || e.FromPort == portModel));

            return self.DeleteStickyNotes(stickyNoteModels)
                .Concat(self.DeletePlacemats(placematModels))
                .Concat(self.DeleteEdges(edgeModels))
                .Concat(self.DeleteVariableDeclarations(variableDeclarationsModels, deleteUsages: false))
                .Concat(self.DeleteNodes(nodeModels, deleteConnections: false)).ToList();
        }

        public static IReadOnlyList<T> GetListOf<T>(this IGraphModel self) where T : IGraphElementModel
        {
            switch (typeof(T))
            {
                case Type x when x == typeof(INodeModel):
                    return (IReadOnlyList<T>)self.NodeModels;

                case Type x when x == typeof(IEdgeModel):
                    return (IReadOnlyList<T>)self.EdgeModels;

                case Type x when x == typeof(IStickyNoteModel):
                    return (IReadOnlyList<T>)self.StickyNoteModels;

                case Type x when x == typeof(IPlacematModel):
                    return (IReadOnlyList<T>)self.PlacematModels;

                case Type x when x == typeof(IVariableDeclarationModel):
                    return (IReadOnlyList<T>)self.VariableDeclarations;

                case Type x when x == typeof(IDeclarationModel):
                    return (IReadOnlyList<T>)self.PortalDeclarations;

                default:
                    throw new ArgumentException($"{typeof(T).Name} isn't a supported type of graph element");
            }
        }

        public static void MoveBefore<T>(this IGraphModel self, IReadOnlyList<T> models, T insertBefore) where T : class, IGraphElementModel
        {
            List<T> list = (List<T>)self.GetListOf<T>();

            if (insertBefore != null)
            {
                var insertBeforeIndex = list.IndexOf(insertBefore);
                while (insertBeforeIndex < list.Count && models.Contains(list[insertBeforeIndex]))
                {
                    insertBeforeIndex++;
                }

                if (insertBeforeIndex < list.Count)
                    insertBefore = list[insertBeforeIndex];
                else
                    insertBefore = null;
            }

            foreach (var model in models)
            {
                list.Remove(model);
            }

            var insertionIndex = list.Count;
            if (insertBefore != null)
                insertionIndex = list.IndexOf(insertBefore);

            foreach (var model in models)
            {
                list.Insert(insertionIndex++, model);
            }
        }

        public static void MoveAfter<T>(this IGraphModel self, IReadOnlyList<T> models, T insertAfter) where T : class, IGraphElementModel
        {
            List<T> list = (List<T>)self.GetListOf<T>();

            if (insertAfter != null)
            {
                var insertAfterIndex = list.IndexOf(insertAfter);
                while (insertAfterIndex >= 0 && models.Contains(list[insertAfterIndex]))
                {
                    insertAfterIndex--;
                }

                if (insertAfterIndex >= 0)
                    insertAfter = list[insertAfterIndex];
                else
                    insertAfter = null;
            }

            foreach (var model in models)
            {
                list.Remove(model);
            }

            var insertionIndex = 0;
            if (insertAfter != null)
                insertionIndex = list.IndexOf(insertAfter) + 1;

            foreach (var model in models)
            {
                list.Insert(insertionIndex++, model);
            }
        }

        public static IEnumerable<IEdgeModel> GetEdgesConnections(this IGraphModel self, IPortModel portModel)
        {
            return self.EdgeModels.Where(e => portModel.Direction == PortDirection.Input ? e.ToPort.Equivalent(portModel) : e.FromPort.Equivalent(portModel));
        }

        public static IEnumerable<IEdgeModel> GetEdgesConnections(this IGraphModel self, INodeModel nodeModel)
        {
            return self.EdgeModels.Where(e => e.ToPort?.NodeModel.Guid == nodeModel.Guid
                || e.FromPort?.NodeModel.Guid == nodeModel.Guid);
        }

        public static IEnumerable<IPortModel> GetConnections(this IGraphModel self, IPortModel portModel)
        {
            return self.GetEdgesConnections(portModel)
                .Select(e => portModel.Direction == PortDirection.Input ? e.FromPort : e.ToPort)
                .Where(p => p != null);
        }

        public static IEdgeModel GetEdgeConnectedToPorts(this IGraphModel self, IPortModel toPort, IPortModel output)
        {
            return self.EdgeModels.FirstOrDefault(e => e.ToPort == toPort && e.FromPort == output);
        }

        /// <summary>
        /// Get the smallest Z order for the placemats in the graph.
        /// </summary>
        /// <returns>The smallest Z order for the placemats in the graph; 0 if the graph has no placemats.</returns>
        public static int GetPlacematMinZOrder(this IGraphModel self)
        {
            return self.PlacematModels.Any() ? self.PlacematModels.Min(m => m.ZOrder) : 0;
        }

        /// <summary>
        /// Get the largest Z order for the placemats in the graph.
        /// </summary>
        /// <returns>The largest Z order for the placemats in the graph; 0 if the graph has no placemats.</returns>
        public static int GetPlacematMaxZOrder(this IGraphModel self)
        {
            return self.PlacematModels.Any() ? self.PlacematModels.Max(m => m.ZOrder) : 0;
        }

        /// <summary>
        /// Get a list of placemats sorted by their Z order.
        /// </summary>
        /// <returns>A list of placemats sorted by their Z order.</returns>
        public static IReadOnlyList<IPlacematModel> GetSortedPlacematModels(this IGraphModel self)
        {
            return self.PlacematModels.OrderBy(p => p.ZOrder).ToList();
        }

        public static void QuickCleanup(this IGraphModel self)
        {
            var toRemove = self.EdgeModels.Where(e => e?.ToPort == null || e.FromPort == null).Cast<IGraphElementModel>()
                .Concat(self.NodeModels.Where(m => m.Destroyed))
                .ToList();
            self.DeleteElements(toRemove);
        }

        public static void Repair(this IGraphModel self)
        {
            var toRemove = self.NodeModels.Where(n => n == null).Cast<IGraphElementModel>()
                .Concat(self.StickyNoteModels.Where(s => s == null))
                .Concat(self.PlacematModels.Where(p => p == null))
                .Concat(self.EdgeModels.Where(e => e?.ToPort == null || e.FromPort == null))
                .ToList();
            self.DeleteElements(toRemove);
        }

        public static bool CheckIntegrity(this IGraphModel self, Verbosity errors)
        {
            Assert.IsTrue((Object)self.AssetModel, "graph asset is invalid");
            bool failed = false;
            for (var i = 0; i < self.EdgeModels.Count; i++)
            {
                var edge = self.EdgeModels[i];
                if (edge.ToPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} toPort is null, output: {edge.FromPort}");
                }

                if (edge.FromPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} output is null, toPort: {edge.ToPort}");
                }
            }

            self.CheckNodeList();
            if (!failed && errors == Verbosity.Verbose)
                Debug.Log("Integrity check succeeded");
            return !failed;
        }

        static void CheckNodeList(this IGraphModel self)
        {
            var existingGuids = new Dictionary<SerializableGUID, int>(self.NodeModels.Count * 4); // wild guess of total number of nodes, including stacked nodes
            for (var i = 0; i < self.NodeModels.Count; i++)
            {
                INodeModel node = self.NodeModels[i];

                Assert.IsTrue(node.GraphModel != null, $"Node {i} {node} graph is null");
                Assert.IsTrue(node.AssetModel != null, $"Node {i} {node} asset is null");
                Assert.IsNotNull(node, $"Node {i} is null");
                Assert.IsTrue(self.AssetModel.GetHashCode() == node.AssetModel?.GetHashCode(), $"Node {i} asset is not matching its actual asset");
                Assert.IsFalse(!node.Guid.Valid, $"Node {i} ({node.GetType()}) has an empty Guid");
                Assert.IsFalse(existingGuids.TryGetValue(node.Guid, out var oldIndex), $"duplicate GUIDs: Node {i} ({node.GetType()}) and Node {oldIndex} have the same guid {node.Guid}");
                existingGuids.Add(node.Guid, i);

                if (node.Destroyed)
                    continue;

                if (node is IInputOutputPortsNodeModel portHolder)
                {
                    CheckNodePorts(portHolder.InputsById);
                    CheckNodePorts(portHolder.OutputsById);
                }

                if (node is IVariableNodeModel variableNode && variableNode.DeclarationModel != null)
                {
                    var originalDeclarations = self.VariableDeclarations.Where(d => d.Guid == variableNode.DeclarationModel.Guid).ToList();
                    Assert.IsTrue(originalDeclarations.Count <= 1);
                    var originalDeclaration = originalDeclarations.SingleOrDefault();
                    Assert.IsNotNull(originalDeclaration, $"Variable Node {i} {variableNode.Title} has a declaration model, but it was not present in the graph's variable declaration list");
                    Assert.IsTrue(ReferenceEquals(originalDeclaration, variableNode.DeclarationModel), $"Variable Node {i} {variableNode.Title} has a declaration model that was not ReferenceEquals() to the matching one in the graph");
                }
            }
        }

        static void CheckNodePorts(IReadOnlyDictionary<string, IPortModel> portsById)
        {
            foreach (var kv in portsById)
            {
                string portId = portsById[kv.Key].UniqueName;
                Assert.AreEqual(kv.Key, portId, $"Node {kv.Key} port and its actual id {portId} mismatch");
            }
        }
    }
}
