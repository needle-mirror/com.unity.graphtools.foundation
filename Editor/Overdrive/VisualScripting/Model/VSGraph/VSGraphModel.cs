using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;
using Object = UnityEngine.Object;

using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Translators;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class VSGraphModel : GraphModel, IVSGraphModel
    {
        [SerializeReference]
        List<VariableDeclarationModel> m_GraphVariableModels = new List<VariableDeclarationModel>();

        [SerializeReference]
        List<VariableDeclarationModel> m_GraphPortalModels = new List<VariableDeclarationModel>();

        public IEnumerable<IVariableDeclarationModel> GraphVariableModels => m_GraphVariableModels;
        public IEnumerable<IVariableDeclarationModel> GraphPortalModels => m_GraphVariableModels;

        public override IList<VariableDeclarationModel> VariableDeclarations => m_GraphVariableModels;
        public override IList<VariableDeclarationModel> PortalDeclarations => m_GraphPortalModels;

        public override IEnumerable<INodeModel> GetAllNodes()
        {
            return NodeModels;
        }

        protected internal override void OnEnable()
        {
            base.OnEnable();

            foreach (var field in m_GraphVariableModels)
            {
                if (field != null)
                    field.VSGraphModel = this;
                else
                    Debug.LogError("Null graphVariableModels in graph. Should only happen during tests for some reason");
            }

            // needed now that nodemodels are not in separate node assets that got OnEnable() before the graph itself would
            foreach (var nodeModel in GetAllNodes())
            {
                (nodeModel as NodeModel)?.DefineNode();
            }
        }

        public VariableDeclarationModel CreateGraphVariableDeclaration(string variableName, TypeHandle variableDataType, bool isExposed)
        {
            var field = VariableDeclarationModel.Create(variableName, variableDataType, isExposed, this, VariableType.GraphVariable, ModifierFlags.None);
            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Create Graph Variable");
            m_GraphVariableModels.Add(field);
            return field;
        }

        public void ReorderGraphVariableDeclaration(IVariableDeclarationModel variableDeclarationModel, int index)
        {
            Assert.IsTrue(index >= 0);

            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Reorder Graph Variable Declaration");

            var varDeclarationModel = (VariableDeclarationModel)variableDeclarationModel;
            if (varDeclarationModel.VariableType == VariableType.GraphVariable)
            {
                var oldIndex = m_GraphVariableModels.IndexOf(varDeclarationModel);
                m_GraphVariableModels.RemoveAt(oldIndex);
                if (index > oldIndex) index--;    // the actual index could have shifted due to the removal
                if (index >= m_GraphVariableModels.Count)
                    m_GraphVariableModels.Add(varDeclarationModel);
                else
                    m_GraphVariableModels.Insert(index, varDeclarationModel);
                LastChanges.ChangedElements.Add(variableDeclarationModel);
                LastChanges.DeletedElements++;
            }
        }

        public void DeleteVariableDeclarations(IEnumerable<VariableDeclarationModel> variableModels, bool deleteUsages)
        {
            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Remove Variable Declarations");

            foreach (VariableDeclarationModel variableModel in variableModels)
            {
                if (LastChanges != null)
                {
                    LastChanges.BlackBoardChanged = true;
                }
                if (variableModel.VariableType == VariableType.GraphVariable || variableModel.VariableType == VariableType.ComponentQueryField)
                {
                    m_GraphVariableModels.Remove(variableModel);
                }
                if (deleteUsages)
                {
                    var nodesToDelete = FindUsages(variableModel).Cast<INodeModel>().ToList();
                    DeleteNodes(nodesToDelete, DeleteConnections.True);
                }
            }
        }

        public VariableDeclarationModel CreateGraphPortalDeclaration(string portalName)
        {
            var field = VariableDeclarationModel.Create(portalName, TypeHandle.Unknown, false, this, VariableType.EdgePortal, ModifierFlags.ReadWrite);
            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Create Graph Portal Declaration Model");
            m_GraphPortalModels.Add(field);
            return field;
        }

        public NodeModel DuplicateNode(INodeModel copiedNode, Dictionary<INodeModel, NodeModel> mapping, Vector2 delta)
        {
            var pastedNodeModel = (NodeModel)copiedNode.Clone();

            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.AssetModel = AssetModel;
            pastedNodeModel.Title = copiedNode.Title;
            pastedNodeModel.AssignNewGuid();
            pastedNodeModel.DefineNode();
            pastedNodeModel.ReinstantiateInputConstants();
            mapping.Add(copiedNode, pastedNodeModel);

            AddNode(pastedNodeModel);
            pastedNodeModel.Position += delta;

            return pastedNodeModel;
        }

        public void MoveVariableDeclaration(IVariableDeclarationModel variableDeclarationModel, IHasVariableDeclaration destination)
        {
            var currentOwner = variableDeclarationModel.Owner;
            var model = (VariableDeclarationModel)variableDeclarationModel;

            Undo.RegisterCompleteObjectUndo(model.SerializableAsset, "Move Variable Declaration");

            currentOwner.VariableDeclarations.Remove(model);
            destination.VariableDeclarations.Add(model);
            LastChanges.ChangedElements.Add(model);
            model.Owner = destination;
        }

//        public virtual string GetUniqueName(string baseName)
//        {
        // TODO: fixme - kept for later
//            var roslynTranslator = stencil.CreateTranslator() as RoslynTranslator;
//            if (roslynTranslator == null)
//                return baseName;
//            var syntaxTree = roslynTranslator.Translate(this, CompilationOptions.Default);
//            return UniqueNameGenerator.CreateUniqueVariableName(syntaxTree, baseName);
//            return baseName;
//        }

        public IEnumerable<IHasVariableDeclarationModel> FindUsages(VariableDeclarationModel decl)
        {
            return decl.FindReferencesInGraph();
        }

        public IEnumerable<T> FindUsages<T>(VariableDeclarationModel decl) where T : IHasVariableDeclarationModel
        {
            return decl.FindReferencesInGraph().OfType<T>();
        }

        public CompilationResult Compile(AssemblyType assemblyType, ITranslator translator, CompilationOptions compilationOptions, IEnumerable<IPluginHandler> pluginHandlers = null)
        {
            Stencil.PreProcessGraph(this);
            CompilationResult result;

            try
            {
                result = translator.TranslateAndCompile(this, assemblyType, compilationOptions);

                if (result.status == CompilationStatus.Failed)
                {
                    Stencil.OnCompilationFailed(this, result);
                }
                else
                {
                    Stencil.OnCompilationSucceeded(this, result);
                }
            }
            catch (Exception e)
            {
                result = null;
                Debug.LogException(e);
            }

            return result;
        }

        public bool CheckIntegrity(Verbosity errors)
        {
            Assert.IsTrue((Object)AssetModel, "graph asset is invalid");
            bool failed = false;
            for (var i = 0; i < m_EdgeModels.Count; i++)
            {
                var edge = m_EdgeModels[i];
                if (edge.ToPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} input is null, output: {edge.FromPort}");
                }

                if (edge.FromPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} output is null, input: {edge.ToPort}");
                }
            }

            CheckNodeList(m_GraphNodeModels);
            if (!failed && errors == Verbosity.Verbose)
                Debug.Log("Integrity check succeeded");
            return true;
        }

        void CheckNodeList(IList<INodeModel> nodeModels, Dictionary<GUID, int> existingGuids = null)
        {
            if (existingGuids == null)
                existingGuids = new Dictionary<GUID, int>(nodeModels.Count * 4); // wild guess of total number of nodes, including stacked nodes
            for (var i = 0; i < nodeModels.Count; i++)
            {
                INodeModel node = nodeModels[i];

                Assert.IsTrue(node.VSGraphModel != null, $"Node {i} {node} graph is null");
                Assert.IsTrue(node.SerializableAsset != null, $"Node {i} {node} asset is null");
                Assert.IsNotNull(node, $"Node {i} is null");
                Assert.IsTrue(AssetModel.IsSameAsset(node.AssetModel), $"Node {i} asset is not matching its actual asset");
                Assert.IsFalse(node.Guid.Empty(), $"Node {i} ({node.GetType()}) has an empty Guid");
                Assert.IsFalse(existingGuids.TryGetValue(node.Guid, out var oldIndex), $"duplicate GUIDs: Node {i} ({node.GetType()}) and Node {oldIndex} have the same guid {node.Guid}");
                existingGuids.Add(node.Guid, i);

                if (node.Destroyed)
                    continue;
                CheckNodePorts(node.InputsById);
                CheckNodePorts(node.OutputsById);

                if (node is VariableNodeModel variableNode && variableNode.DeclarationModel != null)
                {
                    if (variableNode.DeclarationModel.VariableType == VariableType.GraphVariable)
                    {
                        var originalDeclarations = GraphVariableModels.Where(d => d.GetId() == variableNode.DeclarationModel.GetId());
                        Assert.IsTrue(originalDeclarations.Count() <= 1);
                        var originalDeclaration = originalDeclarations.SingleOrDefault();
                        Assert.IsNotNull(originalDeclaration, $"Variable Node {i} {variableNode.Title} has a declaration model, but it was not present in the graph's variable declaration list");
                        Assert.IsTrue(ReferenceEquals(originalDeclaration, variableNode.DeclarationModel), $"Variable Node {i} {variableNode.Title} has a declaration model that was not ReferenceEquals() to the matching one in the graph");
                    }
                }
            }
        }

        static void CheckNodePorts(IReadOnlyDictionary<string, IPortModel> portsById)
        {
            foreach (var kv in portsById)
            {
                string portId = portsById[kv.Key].UniqueId;
                Assert.AreEqual(kv.Key, portId, $"Node {kv.Key} port and its actual id {portId} mismatch");
            }
        }

        public void QuickCleanup()
        {
            for (var i = m_EdgeModels.Count - 1; i >= 0; i--)
            {
                var edge = m_EdgeModels[i];
                if (edge?.ToPort == null || edge.FromPort == null)
                    m_EdgeModels.RemoveAt(i);
            }

            CleanupNodes(m_GraphNodeModels);
        }

        static void CleanupNodes(IList<INodeModel> models)
        {
            for (var i = models.Count - 1; i >= 0; i--)
            {
                if (models[i].Destroyed)
                    models.RemoveAt(i);
            }
        }

        public string SourceFilePath => Stencil?.GetSourceFilePath(this);

        public string TypeName => TypeSystem.CodifyString(AssetModel.Name);

        public ITranslator CreateTranslator()
        {
            return Stencil.CreateTranslator();
        }

        public List<VariableDeclarationModel> DuplicateGraphVariableDeclarations(List<IVariableDeclarationModel> variableDeclarationModels)
        {
            List<VariableDeclarationModel> duplicatedModels = new List<VariableDeclarationModel>();
            foreach (IVariableDeclarationModel original in variableDeclarationModels)
            {
                if (original.VariableType != VariableType.GraphVariable)
                    continue;
                string uniqueName = GetUniqueName(original.Name);
                VariableDeclarationModel copy = ((VariableDeclarationModel)original).Clone();
                copy.Name = uniqueName;
                if (copy.InitializationModel != null)
                {
                    copy.CreateInitializationValue();
                    ((ConstantNodeModel)copy.InitializationModel).ObjectValue = original.InitializationModel.ObjectValue;
                }

                EditorUtility.SetDirty((Object)AssetModel);

                duplicatedModels.Add(copy);
                LastChanges.ChangedElements.Add(copy);
            }

            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Create Graph Variables");
            m_GraphVariableModels.AddRange(duplicatedModels);

            return duplicatedModels;
        }

        public IEnumerable<INodeModel> GetEntryPoints()
        {
            return Stencil.GetEntryPoints(this);
        }
    }
}
