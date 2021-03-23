using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class BlackboardMacroProvider : IBlackboardProvider
    {
        const string k_ClassLibrarySubTitle = "Macro Library";

        const int k_InputPortDeclarationsSection = 0;
        const string k_InputPortDeclarationsSectionTitle = "Input Ports";

        const int k_OutputPortDeclarationsSection = 1;
        const string k_OutputPortDeclarationsSectionTitle = "Output Ports";

        Stencil m_Stencil;
        static GUIContent[] s_Options;
        Store m_Store;

        public BlackboardMacroProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public IEnumerable<BlackboardSection> CreateSections()
        {
            BlackboardSection CreateSection(string sectionTitle, ModifierFlags modifier)
            {
                var section = new BlackboardSection { title = sectionTitle };
                var queriesSectionHeader = section.Q("sectionHeader");
                queriesSectionHeader.Add(new Button(() =>
                {
                    // TODO tech debt about naming
                    var actionName = modifier == ModifierFlags.ReadOnly ? "Input" : "Output";
                    string finalName = actionName;
                    int i = 0;
                    while (((VSGraphModel)m_Store.GetState().CurrentGraphModel).GraphVariableModels.Any(v => v.Name == finalName))
                        finalName = actionName + i++;
                    m_Store.Dispatch(new CreateGraphVariableDeclarationAction(finalName, true, typeof(float).GenerateTypeHandle(m_Stencil), modifier));
                })
                { name = "addButton", text = "+" });

                return section;
            }

            yield return CreateSection(k_InputPortDeclarationsSectionTitle, ModifierFlags.ReadOnly);
            yield return CreateSection(k_OutputPortDeclarationsSectionTitle, ModifierFlags.WriteOnly);
        }

        public string GetSubTitle()
        {
            return k_ClassLibrarySubTitle;
        }

        public void AddItemRequested<TAction>(Store store, TAction action) where TAction : IAction
        {
            throw new NotImplementedException();
        }

        public void MoveItemRequested(Store store, int index, VisualElement field)
        {
            if (field is BlackboardVariableField blackboardField)
                store.Dispatch(new ReorderGraphVariableDeclarationAction(blackboardField.VariableDeclarationModel, index));
        }

        public void RebuildSections(Blackboard blackboard)
        {
            m_Store = blackboard.Store;
            var currentGraphModel = (VSGraphModel)blackboard.Store.GetState().CurrentGraphModel;

            blackboard.ClearContents();

            if (blackboard.Sections != null && blackboard.Sections.Count > 1)
            {
                blackboard.Sections[k_InputPortDeclarationsSection].title = k_InputPortDeclarationsSectionTitle;
                blackboard.Sections[k_OutputPortDeclarationsSection].title = k_OutputPortDeclarationsSectionTitle;
            }

            if (currentGraphModel.NodeModels == null)
                return;

            foreach (VariableDeclarationModel declaration in currentGraphModel.VariableDeclarations)
            {
                var blackboardField = new BlackboardVariableField(blackboard.Store, declaration, blackboard.GraphView);
                var blackboardRow = new BlackboardRow(
                    blackboardField,
                    new BlackboardVariablePropertyView(blackboard.Store, declaration, blackboard.Rebuild, m_Stencil)
                        .WithTypeSelector()
                        .WithTooltipField())
                {
                    userData = declaration,
                    expanded = true, // TODO not pretty
                };
                if (blackboard.Sections != null)
                {
                    switch (declaration.Modifiers)
                    {
                        case ModifierFlags.ReadOnly:
                            blackboard.Sections[k_InputPortDeclarationsSection].Add(blackboardRow);
                            break;
                        case ModifierFlags.WriteOnly:
                            blackboard.Sections[k_OutputPortDeclarationsSection].Add(blackboardRow);
                            break;
                    }
                }
                blackboard.GraphVariables.Add(blackboardField);
            }
        }

        public void DisplayAppropriateSearcher(Vector2 mousePosition, Blackboard blackboard)
        {
            VisualElement picked = blackboard.panel.Pick(mousePosition);
            while (picked != null && !(picked is IVisualScriptingField))
                picked = picked.parent;

            // optimization: stop at the first IVsBlackboardField, but still exclude BlackboardThisFields
            if (picked != null && picked is BlackboardVariableField field)
                blackboard.GraphView.window.DisplayTokenDeclarationSearcher((VariableDeclarationModel)field.VariableDeclarationModel, mousePosition);
            else
                blackboard.GraphView.window.DisplayAddVariableSearcher(mousePosition);
        }

        public bool CanAddItems => false;
        public void BuildContextualMenu(DropdownMenu evtMenu, VisualElement visualElement, Store store, Vector2 mousePosition) { }
    }
}
