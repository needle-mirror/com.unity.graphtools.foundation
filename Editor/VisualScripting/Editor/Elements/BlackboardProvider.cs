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
    class BlackboardProvider : IBlackboardProvider
    {
        const int k_MainSection = 0;
        const string k_MainSectionTitle = "Graph Variables";

        const int k_CurrentScopeVariableDeclarationsSection = 1;
        const string k_CurrentScopeVariableDeclarationsSectionTitle = "Current Scope";

        const string k_ClassLibrarySubTitle = "Class Library";
        const string k_FieldName = "variable";
        readonly Stencil m_Stencil;

        public BlackboardProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        VisualElement CreateExtendedFieldView(Store store, IVariableDeclarationModel variableDeclarationModel,
            Blackboard.RebuildCallback rebuild)
        {
            return new BlackboardVariablePropertyView(store, variableDeclarationModel, rebuild, m_Stencil)
                .WithTypeSelector()
                .WithExposedToggle()
                .WithTooltipField()
                .WithInitializationField();
        }

        public IEnumerable<BlackboardSection> CreateSections()
        {
            var mainSection = new BlackboardSection { title = k_MainSectionTitle };
            mainSection.canAcceptDrop += MainCanAcceptDrop;
            yield return mainSection;
            var blackboardSection = new BlackboardSection {title = k_CurrentScopeVariableDeclarationsSectionTitle};
            blackboardSection.canAcceptDrop += _ => false;
            yield return blackboardSection;
        }

        static bool MainCanAcceptDrop(ISelectable selected)
        {
            return !(selected is BlackboardThisField);
        }

        public string GetSubTitle()
        {
            return k_ClassLibrarySubTitle;
        }

        public void AddItemRequested<TAction>(Store store, TAction _) where TAction : IAction
        {
            store.Dispatch(new CreateGraphVariableDeclarationAction(k_FieldName, true, typeof(float).GenerateTypeHandle(m_Stencil)));
        }

        public void MoveItemRequested(Store store, int index, VisualElement field)
        {
            if (field is BlackboardVariableField blackboardField)
            {
                //TODO: Index needs to be offset until we somehow find a way to add a "this"
                //      variableDeclarationModel inside the m_GraphVariableModels List.
                //      It offsets the index if the blackboard contains a this "pill"
                if (blackboardField.GraphElementModel.GraphModel.Stencil.GetThisType().IsValid)
                    index--;

                store.Dispatch(new ReorderGraphVariableDeclarationAction(blackboardField.VariableDeclarationModel, index));
            }
        }

        public void RebuildSections(Blackboard blackboard)
        {
            VSGraphModel currentGraphModel = (VSGraphModel)blackboard.Store.GetState().CurrentGraphModel;

            Dictionary<IVariableDeclarationModel, bool> expandedRows = new Dictionary<IVariableDeclarationModel, bool>();
            foreach (BlackboardSection blackBoardSection in blackboard.Sections)
            {
                foreach (VisualElement visualElement in blackBoardSection.Children())
                {
                    if (visualElement is BlackboardRow row)
                    {
                        if (row.userData is IVariableDeclarationModel model)
                            expandedRows[model] = row.expanded;
                        else if (row.userData is Tuple<IVariableDeclarationModel, bool> modelTuple)
                            expandedRows[modelTuple.Item1] = row.expanded;
                    }
                }
            }

            blackboard.ClearContents();

            if (CanDisplayThisToken)
            {
                var thisNodeModel = currentGraphModel.NodeModels.OfType<ThisNodeModel>().FirstOrDefault();
                var thisField = new BlackboardThisField(blackboard.GraphView, thisNodeModel, currentGraphModel);
                blackboard.Sections[k_MainSection].Add(thisField);
                blackboard.GraphVariables.Add(thisField);
                blackboard.RestoreSelectionForElement(thisField);
            }

            // Fetch all fields from the GraphModel in the main section
            foreach (IVariableDeclarationModel variableDeclarationModel in ((IVSGraphModel)currentGraphModel).GraphVariableModels)
            {
                var blackboardField = new BlackboardVariableField(blackboard.Store, variableDeclarationModel, blackboard.GraphView);
                var blackboardRow = new BlackboardRow(
                    blackboardField,
                    CreateExtendedFieldView(blackboard.Store, variableDeclarationModel, blackboard.Rebuild))
                {
                    userData = variableDeclarationModel,
                    expanded = expandedRows.TryGetValue(variableDeclarationModel, out var expandedValue) && expandedValue
                };
                blackboard.Sections[k_MainSection].Add(blackboardRow);
                blackboard.GraphVariables.Add(blackboardField);
                blackboard.RestoreSelectionForElement(blackboardField);
            }

            if (blackboard.selection != null)
            {
                // Fill local scope section
                foreach (Tuple<IVariableDeclarationModel, bool> variableDeclarationModelTuple in blackboard.GraphView.UIController
                         .GetAllVariableDeclarationsFromSelection(blackboard.selection))
                {
                    var blackboardField = new BlackboardVariableField(blackboard.Store, variableDeclarationModelTuple.Item1, blackboard.GraphView);

                    if (variableDeclarationModelTuple.Item1.VariableType == VariableType.FunctionParameter)
                        blackboardField.AddToClassList("parameter");

                    if (variableDeclarationModelTuple.Item2)
                    {
                        var blackboardRow = new BlackboardRow(
                            blackboardField,
                            CreateExtendedFieldView(blackboard.Store, variableDeclarationModelTuple.Item1, blackboard.Rebuild))
                        {
                            userData = variableDeclarationModelTuple,
                            expanded = expandedRows.TryGetValue(variableDeclarationModelTuple.Item1, out var expandedValue) && expandedValue
                        };
                        blackboard.Sections[k_CurrentScopeVariableDeclarationsSection].Add(blackboardRow);
                    }
                    else
                    {
                        blackboardField.AddToClassList("readonly");
                        blackboard.Sections[k_CurrentScopeVariableDeclarationsSection].Add(blackboardField);
                    }

                    blackboard.GraphVariables.Add(blackboardField);

                    blackboard.RestoreSelectionForElement(blackboardField);
                }
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

        public bool CanAddItems => true;

        public void BuildContextualMenu(DropdownMenu evtMenu, VisualElement visualElement, Store store, Vector2 mousePosition) {}

        static bool CanDisplayThisToken => true;
    }
}
