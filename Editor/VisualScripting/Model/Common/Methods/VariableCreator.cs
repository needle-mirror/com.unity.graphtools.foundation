using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public class VariableCreator
    {
        readonly FunctionModel m_LoopStackModel;
        int m_NextIndex;
        public int DeclaredVariablesCount => m_NextIndex;

        public VariableCreator(FunctionModel loopStackModel)
        {
            m_LoopStackModel = loopStackModel;
            Log($"Var creator for {loopStackModel.Title} in graph {loopStackModel.GraphModel.Name}");
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        static void Log(string message)
        {
#if false
            Debug.Log(message);
#endif
        }

        public TDecl DeclareVariable<TDecl>(string defaultItemName, TypeHandle type, LoopStackModel.TitleComponentIcon item, VariableFlags variableFlags, bool forceInsert = false) where TDecl : LoopVariableDeclarationModel, new()
        {
            var list = (List<VariableDeclarationModel>)m_LoopStackModel.FunctionParameterModels;

            bool createNew = false;
            int insertIndex = -1;
            LoopVariableDeclarationModel existing = null;

            if (forceInsert)
            {
                createNew = true;
                insertIndex = m_NextIndex;
            }
            else
            {
                while (m_NextIndex < list.Count)
                {
                    var variableDeclarationModel = list[m_NextIndex++];
                    if (variableDeclarationModel == null)
                        continue;
                    existing = variableDeclarationModel as LoopVariableDeclarationModel;
                    if (variableDeclarationModel.variableFlags.HasFlag(VariableFlags.Generated))
                        break;
                }

                if (existing != null)
                {
                    insertIndex = m_NextIndex;
                }
                else
                {
                    Log($"  {defaultItemName}: create new, out of range");
                    createNew = true;
                }
            }

            if (existing == null || existing.GetType() != typeof(TDecl)) // try to reuse
            {
                var cause = existing == null ? "not existing" : "wrong type";
                Log($"  {defaultItemName}: create new, cannot reuse, cause: {cause}");
                createNew = true;
            }
            else
                Log($"  {defaultItemName}: reused");

            if (createNew)
            {
                m_NextIndex++;
                existing = VariableDeclarationModel.Create<TDecl>(
                    defaultItemName,
                    type,
                    false,
                    (GraphModel)m_LoopStackModel.GraphModel,
                    VariableType.FunctionParameter,
                    ModifierFlags.None,
                    m_LoopStackModel,
                    variableFlags);

                Log($"    {defaultItemName}: index {insertIndex}");
                if (insertIndex == -1)
                    list.Add(existing);
                else
                {
                    list.Insert(insertIndex, existing);

                    //                        m_NextIndex++; // just inserted one so
                }
            }
            else
            {
                VariableDeclarationModel.SetupDeclaration(defaultItemName,
                    type,
                    false,
                    (GraphModel)m_LoopStackModel.GraphModel,
                    VariableType.FunctionParameter,
                    ModifierFlags.None,
                    variableFlags,
                    m_LoopStackModel,
                    existing);
            }

            existing.TitleComponentIcon = item;
            existing.IsExposed = variableFlags.HasFlag(VariableFlags.Hidden); // TODO use hidden parameter
            return (TDecl)existing;
        }

        public void Flush()
        {
            var vsGraphModel = (VSGraphModel)m_LoopStackModel.GraphModel;

            List<VariableDeclarationModel> variableDeclarationModels = (List<VariableDeclarationModel>)m_LoopStackModel.FunctionParameterModels;

            // we need to refresh the usages of recycled variable declarations so they adjust their port types
            for (int i = 0; i < m_NextIndex; i++)
            {
                var decl = variableDeclarationModels[i];
                if (decl != null)
                    foreach (VariableNodeModel usage in vsGraphModel.FindUsages(decl))
                        usage.UpdateTypeFromDeclaration();
            }

            for (int i = variableDeclarationModels.Count - 1; i >= m_NextIndex; i--)
            {
                if (variableDeclarationModels[i] == null)
                {
                    variableDeclarationModels.RemoveAt(i--);
                    continue;
                }
                if (variableDeclarationModels[i].variableFlags.HasFlag(VariableFlags.Generated))
                    vsGraphModel.DeleteVariableDeclarations(new[] { variableDeclarationModels[i] }, true);
            }
        }
    }
}
