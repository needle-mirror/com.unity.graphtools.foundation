using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class BlackboardVariablePropertyView : BlackboardExtendedFieldView
    {
        Toggle m_ExposedToggle;
        TextField m_TooltipTextField;
        readonly VisualElement m_InitializationElement;
        readonly Store m_Store;
        readonly Stencil m_Stencil;

        protected IGTFVariableDeclarationModel VariableDeclarationModel => userData as IGTFVariableDeclarationModel;
        string TypeText => VariableDeclarationModel.DataType.GetMetadata(m_Stencil).FriendlyName;

        static readonly GUIContent k_InitializationContent = new GUIContent("");

        public BlackboardVariablePropertyView(Store store, IGTFVariableDeclarationModel variableDeclarationModel,
                                              GraphElements.Blackboard.RebuildCallback rebuildCallback, Stencil stencil)
            : base(variableDeclarationModel, rebuildCallback)
        {
            m_Store = store;
            m_Stencil = stencil;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            if (variableDeclarationModel.VariableType != VariableType.GraphVariable)
                return;

            if (variableDeclarationModel.InitializationModel == null)
            {
                if (stencil.RequiresInitialization(variableDeclarationModel))
                {
                    m_InitializationElement = new Button(OnInitializationButton) {text = "Create Init value"};
                    m_InitializationElement.AddToClassList("rowButton");
                }
            }
            else
            {
                m_InitializationElement = InlineValueEditor.CreateEditorForConstant(VariableDeclarationModel.AssetModel, variableDeclarationModel.InitializationModel, (_, v) =>
                {
                    m_Store.Dispatch(new UpdateConstantNodeActionValue(variableDeclarationModel.InitializationModel, v, null));
                }, store.GetState()?.EditorDataModel, false);

//                m_InitializationObject = new SerializedObject(variableDeclarationModel.InitializationModel.NodeAssetReference);
//                m_InitializationObject.Update();
//                m_InitializationElement = new IMGUIContainer(OnInitializationGUI);
            }
        }

        public BlackboardVariablePropertyView WithTypeSelector()
        {
            var typeButton = new Button(() =>
                SearcherService.ShowVariableTypes(
                    m_Stencil,
                    Event.current.mousePosition,
                    (t, i) => OnTypeChanged(t)
                )
                ) { text = TypeText };
            typeButton.AddToClassList("rowButton");
            AddRow("Type", typeButton);

            return this;
        }

        public BlackboardVariablePropertyView WithExposedToggle()
        {
            if (VariableDeclarationModel.VariableType == VariableType.GraphVariable)
            {
                m_ExposedToggle = new Toggle { value = VariableDeclarationModel.IsExposed };
                AddRow("Exposed", m_ExposedToggle);
            }

            return this;
        }

        public BlackboardVariablePropertyView WithTooltipField()
        {
            m_TooltipTextField = new TextField
            {
                isDelayed = true,
                value = VariableDeclarationModel.Tooltip
            };
            AddRow("Tooltip", m_TooltipTextField);

            return this;
        }

        public BlackboardVariablePropertyView WithInitializationField()
        {
            if (m_InitializationElement != null)
                AddRow("Initialization", m_InitializationElement);

            return this;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_ExposedToggle?.UnregisterValueChangedCallback(OnExposedChanged);
            m_TooltipTextField.UnregisterValueChangedCallback(OnTooltipChanged);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            schedule.Execute(() => m_ExposedToggle?.RegisterValueChangedCallback(OnExposedChanged));
            m_TooltipTextField.RegisterValueChangedCallback(OnTooltipChanged);
        }

        void RefreshUI(GraphElements.Blackboard.RebuildMode rebuildMode = GraphElements.Blackboard.RebuildMode.BlackboardAndGraphView)
        {
            m_ExposedToggle?.UnregisterValueChangedCallback(OnExposedChanged);
            m_RebuildCallback?.Invoke(rebuildMode);
        }

        void OnInitializationButton()
        {
            ((VariableDeclarationModel)userData).CreateInitializationValue();

            RefreshUI();
        }

        void OnTypeChanged(TypeHandle handle)
        {
            m_Store.Dispatch(new UpdateTypeAction((VariableDeclarationModel)VariableDeclarationModel, handle));
            RefreshUI();
        }

        void OnExposedChanged(ChangeEvent<bool> evt)
        {
            m_Store.Dispatch(new UpdateExposedAction((VariableDeclarationModel)VariableDeclarationModel, m_ExposedToggle.value));
            RefreshUI();
        }

        void OnTooltipChanged(ChangeEvent<string> evt)
        {
            m_Store.Dispatch(new UpdateTooltipAction((VariableDeclarationModel)VariableDeclarationModel, m_TooltipTextField.value));
            RefreshUI();
        }
    }
}
