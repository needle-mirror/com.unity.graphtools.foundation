using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    class Placemat : GraphElements.Placemat, IHasGraphElementModel, IContextualMenuBuilder
    {
        public IGraphElementModel GraphElementModel => Model as IGraphElementModel;

        VisualElement m_ContentContainer;
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        protected override void BuildElementUI()
        {
            m_ContentContainer = this.AddBorder(k_UssClassName);
            base.BuildElementUI();
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Placemat.uss"));
        }

        void IContextualMenuBuilder.BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            BuildContextualMenu(evt);
            var index = evt.menu.MenuItems().FindIndex(item => (item as DropdownMenuAction)?.name == "Change Color...");
            evt.menu.RemoveItemAt(index);
            // Also remove separator.
            evt.menu.RemoveItemAt(index);
        }
    }
}
