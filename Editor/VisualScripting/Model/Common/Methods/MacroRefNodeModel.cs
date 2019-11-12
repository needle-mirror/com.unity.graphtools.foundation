using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class MacroRefNodeModel : NodeModel, IRenamableModel, IObjectReference, IExposeTitleProperty
    {
        [Serializable]
        struct CachedVariableInfos
        {
            public string Name;
            public TypeHandle Type;
        }

        public override string Title => m_Graph?.AssetModel?.Name ?? $"<{base.Title}>";

        public override string IconTypeString => "typeMacro";

        [SerializeField]
        GraphAssetModel m_GraphAsset;
        GraphModel m_Graph;

        List<IVariableDeclarationModel> m_DefinedInputVariables = new List<IVariableDeclarationModel>();
        List<IVariableDeclarationModel> m_DefinedOutputVariables = new List<IVariableDeclarationModel>();

        // To survive domain reload and reconnect edges in case of when the macro asset has been deleted
        [SerializeField]
        List<CachedVariableInfos> m_CachedInputVariables = new List<CachedVariableInfos>();
        [SerializeField]
        List<CachedVariableInfos> m_CachedOutputVariables = new List<CachedVariableInfos>();

        public IReadOnlyList<IVariableDeclarationModel> DefinedInputVariables => m_DefinedInputVariables;
        public IReadOnlyList<IVariableDeclarationModel> DefinedOutputVariables => m_DefinedOutputVariables;

        public override IReadOnlyList<IPortModel> InputsByDisplayOrder
        {
            get
            {
                DefineNode(); // the macro definition might have been modified
                return base.InputsByDisplayOrder;
            }
        }

        public override IReadOnlyList<IPortModel> OutputsByDisplayOrder
        {
            get
            {
                DefineNode(); // the macro definition might have been modified
                return base.OutputsByDisplayOrder;
            }
        }

        public IEnumerable<IPortModel> InputVariablePorts
        {
            get
            {
                return m_Graph != null
                    ? DefinedInputVariables.Select(v => InputsById[v.VariableName])
                    : m_CachedInputVariables.Select(v => InputsById[v.Name]);
            }
        }

        public IEnumerable<IPortModel> OutputVariablePorts
        {
            get
            {
                return m_Graph != null
                    ? DefinedOutputVariables.Select(v => OutputsById[v.VariableName])
                    : m_CachedOutputVariables.Select(v => OutputsById[v.Name]);
            }
        }

        public GraphModel Macro
        {
            get
            {
                if (m_Graph == null && m_GraphAsset?.GraphModel != null)
                    m_Graph = m_GraphAsset?.GraphModel as GraphModel;
                return m_Graph;
            }
            set
            {
                m_Graph = value;
                m_GraphAsset = m_Graph?.AssetModel as GraphAssetModel;
            }
        }

        [CanBeNull]
        public Object ReferencedObject => (m_Graph != null && m_Graph.AssetModel != null)
        ? (Object)m_Graph.AssetModel
        : null;

        public string TitlePropertyName => "m_Name";

        protected override void OnDefineNode()
        {
            if (m_Graph == null)
            {
                foreach (var cachedVariableInfos in m_CachedInputVariables)
                    AddDataInput(cachedVariableInfos.Name, cachedVariableInfos.Type);

                foreach (var cachedVariableInfos in m_CachedOutputVariables)
                    AddDataOutputPort(cachedVariableInfos.Name, cachedVariableInfos.Type);

                return;
            }

            if (m_DefinedInputVariables == null)
                m_DefinedInputVariables = new List<IVariableDeclarationModel>();
            else
                m_DefinedInputVariables.Clear();
            if (m_DefinedOutputVariables == null)
                m_DefinedOutputVariables = new List<IVariableDeclarationModel>();
            else
                m_DefinedOutputVariables.Clear();

            m_CachedInputVariables.Clear();
            m_CachedOutputVariables.Clear();

            foreach (var declaration in m_Graph.VariableDeclarations)
            {
                switch (declaration.Modifiers)
                {
                    case ModifierFlags.ReadOnly:
                        AddDataInput(declaration.VariableName, declaration.DataType);
                        m_DefinedInputVariables.Add(declaration);
                        m_CachedInputVariables.Add(
                            new CachedVariableInfos { Name = declaration.VariableName, Type = declaration.DataType });
                        break;
                    case ModifierFlags.WriteOnly:
                        AddDataOutputPort(declaration.VariableName, declaration.DataType);
                        m_DefinedOutputVariables.Add(declaration);
                        m_CachedOutputVariables.Add(
                            new CachedVariableInfos { Name = declaration.VariableName, Type = declaration.DataType });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            $"Variable {declaration.Name} has modifiers '{declaration.Modifiers}'");
                }
            }
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        [SuppressMessage("ReSharper", "BaseObjectGetHashCodeCallInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                if (m_Graph != null)
                    hashCode = (hashCode * 777) ^ (m_Graph.GetHashCode());
                return hashCode;
            }
        }

        public void Rename(string newName)
        {
            //TODO The Undo should be handled by a reducer and not the rename operation itself
            Undo.RegisterCompleteObjectUndo(Macro.AssetModel as VSGraphAssetModel, "Rename Macro");
            var assetPath = AssetDatabase.GetAssetPath(Macro.AssetModel as VSGraphAssetModel);
            AssetDatabase.RenameAsset(assetPath, ((VSGraphModel)GraphModel).GetUniqueName(newName));
        }

        public override CapabilityFlags Capabilities => m_Graph != null
        ? base.Capabilities | CapabilityFlags.Renamable
        : base.Capabilities;
    }
}
