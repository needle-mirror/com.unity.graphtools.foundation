using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    [Serializable]
    public class LoopVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField]
        LoopStackModel.TitleComponentIcon m_TitleComponentIcon;

        public LoopStackModel.TitleComponentIcon TitleComponentIcon
        {
            get => m_TitleComponentIcon;
            set => m_TitleComponentIcon = value;
        }

        public static LoopVariableDeclarationModel CreateNoUndoRecord(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags, FunctionModel functionModel,
            LoopStackModel.TitleComponentIcon titleComponentIcon, IConstantNodeModel initializationModel = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            Assert.IsNotNull(graph);
            Assert.IsNotNull(graph.AssetModel);

            var decl = new LoopVariableDeclarationModel();
            decl.GraphModel = graph;
            decl.VariableName = variableName;
            decl.IsExposed = isExposed;
            decl.VariableType = variableType;
            decl.Modifiers = modifierFlags;
            decl.FunctionModel = functionModel;
            decl.TitleComponentIcon = titleComponentIcon;
            if (initializationModel != null)
                decl.InitializationModel = initializationModel;
            else if (!spawnFlags.IsOrphan())
                decl.CreateInitializationValue();

            if (spawnFlags.IsSerializable())
            {
                decl.DataType = dataType;
                ((VSGraphModel)graph).LastChanges.ChangedElements.Add(decl);
                EditorUtility.SetDirty((Object)graph.AssetModel);
            }

            return decl;
        }
    }
}
