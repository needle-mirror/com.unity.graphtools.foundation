using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardProvider : IBlackboardProvider
    {
        const int k_MainSection = 0;
        const string k_MainSectionTitle = "Graph Variables";

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

        static bool MainCanAcceptDrop(ISelectableGraphElement selected)
        {
            return true;
        }

        public string GetSubTitle()
        {
            return k_ClassLibrarySubTitle;
        }

        public void AddItemRequested<TAction>(Store store, TAction _) where TAction : BaseAction
        {
            store.Dispatch(new CreateGraphVariableDeclarationAction(k_FieldName, true, typeof(float).GenerateTypeHandle()));
        }

        public void MoveItemRequested(Store store, int index, VisualElement field)
        {
            if (field is BlackboardVariableField blackboardField)
            {
                //TODO: Index needs to be offset until we somehow find a way to add a "this"
                //      variableDeclarationModel inside the m_GraphVariableModels List.
                //      It offsets the index if the blackboard contains a this "pill"
                if (blackboardField.Model.GraphModel.Stencil.GetThisType().IsValid)
                    index--;

                store.Dispatch(new ReorderGraphVariableDeclarationAction(blackboardField.VariableDeclarationModel, index));
            }
        }

        public void RebuildSections(Blackboard blackboard)
        {
            var currentGraphModel = blackboard.Store.GetState().CurrentGraphModel;

            Dictionary<IVariableDeclarationModel, bool> expandedRows = new Dictionary<IVariableDeclarationModel, bool>();
            foreach (BlackboardSection blackBoardSection in blackboard.Sections)
            {
                foreach (VisualElement visualElement in blackBoardSection.Children())
                {
                    if (visualElement is BlackboardRow row)
                    {
                        if (row.Model is IVariableDeclarationModel model)
                            expandedRows[model] = row.expanded;
                    }
                }
            }

            blackboard.ClearContents();

            // Fetch all fields from the GraphModel in the main section
            foreach (var variableDeclarationModel in currentGraphModel.VariableDeclarations)
            {
                var blackboardField = new BlackboardVariableField(blackboard.Store, variableDeclarationModel, blackboard.GraphView);
                var blackboardRow = new BlackboardRow(
                    blackboardField,
                    CreateExtendedFieldView(blackboard.Store, variableDeclarationModel, blackboard.Rebuild))
                {
                    Model = variableDeclarationModel,
                    expanded = expandedRows.TryGetValue(variableDeclarationModel, out var expandedValue) && expandedValue
                };
                blackboard.Sections[k_MainSection].Add(blackboardRow);
                blackboard.GraphVariables.Add(blackboardField);
                blackboard.RestoreSelectionForElement(blackboardField);
            }
        }

        static void DisplayAddVariableSearcher(Store store, Vector2 pos)
        {
            if (store.GetState().CurrentGraphModel == null)
            {
                return;
            }

            SearcherService.ShowVariableTypes(
                store.GetState().CurrentGraphModel.Stencil,
                pos,
                (t, i) =>
                {
                    var graphModel = store.GetState().CurrentGraphModel;
                    var declaration = graphModel.CreateGraphVariableDeclaration(
                        "newVariable",
                        t,
                        ModifierFlags.None,
                        true
                    );

                    store.GetState().EditorDataModel.ElementModelToRename = declaration;
                    store.ForceRefreshUI(UpdateFlags.All);
                });
        }

        public static void DisplayTokenDeclarationSearcher(Store store, IVariableDeclarationModel declaration, Vector2 pos)
        {
            if (store.GetState().CurrentGraphModel == null || !(declaration is VariableDeclarationModel vdm))
            {
                return;
            }

            SearcherService.ShowVariableTypes(
                store.GetState().CurrentGraphModel.Stencil,
                pos,
                (t, i) =>
                {
                    var graphModel = store.GetState().CurrentGraphModel;
                    vdm.DataType = t;

                    foreach (var usage in graphModel.FindReferencesInGraph<VariableNodeModel>(vdm))
                    {
                        usage.UpdateTypeFromDeclaration();
                    }

                    store.ForceRefreshUI(UpdateFlags.All);
                });
        }

        public void DisplayAppropriateSearcher(Vector2 mousePosition, Blackboard blackboard)
        {
            VisualElement picked = blackboard.panel.Pick(mousePosition);
            while (picked != null && !(picked is IVisualScriptingField))
                picked = picked.parent;

            // optimization: stop at the first IVsBlackboardField, but still exclude BlackboardThisFields
            if (picked != null && picked is BlackboardVariableField field)
                DisplayTokenDeclarationSearcher(blackboard.Store, field.VariableDeclarationModel, mousePosition);
            else
                DisplayAddVariableSearcher(blackboard.Store, mousePosition);
        }

        public bool CanAddItems => true;
    }
}
