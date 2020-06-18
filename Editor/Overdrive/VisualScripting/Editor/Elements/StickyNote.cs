using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    class StickyNote : GraphElements.StickyNote, IHasGraphElementModel
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
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "StickyNote.uss"));
        }
    }
}
