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

        public override string Title => m_GraphAsset != null ? m_GraphAsset.Name : $"<{base.Title}>";

        public override string IconTypeString => "typeMacro";

        [SerializeField]
        GraphAssetModel m_GraphAsset;

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
                return m_GraphAsset != null
                    ? DefinedInputVariables.Select(v => InputsById[v.VariableName])
                    : m_CachedInputVariables.Select(v => InputsById[v.Name]);
            }
        }

        public IEnumerable<IPortModel> OutputVariablePorts
        {
            get
            {
                return m_GraphAsset != null
                    ? DefinedOutputVariables.Select(v => OutputsById[v.VariableName])
                    : m_CachedOutputVariables.Select(v => OutputsById[v.Name]);
            }
        }

        public GraphAssetModel GraphAssetModel
        {
            get => m_GraphAsset;
            set => m_GraphAsset = value;
        }

        [CanBeNull]
        public Object ReferencedObject => m_GraphAsset != null ? m_GraphAsset : null;

        public string TitlePropertyName => "m_Name";

        protected override void OnDefineNode()
        {
            if (m_GraphAsset == null)
            {
                foreach (var cachedVariableInfos in m_CachedInputVariables)
                    AddDataInputPort(cachedVariableInfos.Name, cachedVariableInfos.Type);

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

            foreach (var declaration in ((GraphModel)m_GraphAsset.GraphModel).VariableDeclarations)
            {
                switch (declaration.Modifiers)
                {
                    case ModifierFlags.ReadOnly:
                        AddDataInputPort(declaration.VariableName, declaration.DataType);
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
                if (m_GraphAsset != null)
                    hashCode = (hashCode * 777) ^ (m_GraphAsset.GetHashCode());
                return hashCode;
            }
        }

        public void Rename(string newName)
        {
            //TODO The Undo should be handled by a reducer and not the rename operation itself
            Undo.RegisterCompleteObjectUndo(GraphAssetModel.GraphModel.AssetModel as VSGraphAssetModel, "Rename Macro");
            var assetPath = AssetDatabase.GetAssetPath(GraphAssetModel.GraphModel.AssetModel as VSGraphAssetModel);
            AssetDatabase.RenameAsset(assetPath, ((VSGraphModel)GraphModel).GetUniqueName(newName));
        }

        public override CapabilityFlags Capabilities => m_GraphAsset != null
        ? base.Capabilities | CapabilityFlags.Renamable
        : base.Capabilities;
    }
}
