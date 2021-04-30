using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardRow : GraphElement
    {
        public static new readonly string ussClassName = "ge-blackboard-row";
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");
        public static readonly string headerContainerUssClassName = ussClassName.WithUssElement("header-container");
        public static readonly string collapseButtonUssClassName = ussClassName.WithUssElement("collapse-button");
        public static readonly string propertyViewUssClassName = ussClassName.WithUssElement("property-view-container");
        public static readonly string expandedModifierUssClassName = ussClassName.WithUssModifier("collapsed");

        public static readonly string rowFieldPartName = "blackboard-row-field-part";
        public static readonly string rowPropertiesPartName = "blackboard-row-properties-part";

        protected VisualElement m_HeaderContainer;
        protected VisualElement m_PropertyViewContainer;
        protected CollapseButton m_CollapseButton;

        public VisualElement FieldSlot => m_HeaderContainer;
        public VisualElement PropertiesSlot => m_PropertyViewContainer;

        public BlackboardRow()
        {
            RegisterCallback<PromptSearcherEvent>(OnPromptSearcher);
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(BlackboardVariablePart.Create(rowFieldPartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardVariablePropertiesPart.Create(rowPropertiesPartName, Model, this, ussClassName));
        }

        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            var header = new VisualElement { name = "row-header" };
            header.AddToClassList(headerUssClassName);

            m_CollapseButton = new CollapseButton();
            m_CollapseButton.AddToClassList(collapseButtonUssClassName);
            header.Add(m_CollapseButton);

            m_HeaderContainer = new VisualElement { name = "row-header-container" };
            m_HeaderContainer.AddToClassList(headerContainerUssClassName);
            header.Add(m_HeaderContainer);

            Add(header);

            m_PropertyViewContainer = new VisualElement { name = "property-view-container" };
            m_PropertyViewContainer.AddToClassList(propertyViewUssClassName);
            Add(m_PropertyViewContainer);

            m_CollapseButton.RegisterCallback<ChangeEvent<bool>>(OnCollapseButtonChange);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is IVariableDeclarationModel vdm)
            {
                bool isExpanded = CommandDispatcher.State.BlackboardViewState.GetVariableDeclarationModelExpanded(vdm);

                EnableInClassList(expandedModifierUssClassName, !isExpanded);
                m_CollapseButton.SetValueWithoutNotify(!isExpanded);
            }
        }

        void OnCollapseButtonChange(ChangeEvent<bool> e)
        {
            CommandDispatcher.Dispatch(new ExpandOrCollapseBlackboardRowCommand(!e.newValue, Model as IVariableDeclarationModel));
        }

        void OnPromptSearcher(PromptSearcherEvent e)
        {
            if (!(Model is IVariableDeclarationModel vdm))
            {
                return;
            }

            SearcherService.ShowVariableTypes(
                (Stencil)Model.GraphModel.Stencil,
                e.MenuPosition,
                (t, i) =>
                {
                    CommandDispatcher.Dispatch(new ChangeVariableTypeCommand(vdm, t));
                });

            e.StopPropagation();
        }
    }
}
