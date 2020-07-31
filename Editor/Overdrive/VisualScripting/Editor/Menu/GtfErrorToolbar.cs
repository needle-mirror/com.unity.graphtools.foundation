using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class GtfErrorToolbar : VisualElement
    {
        private readonly Store m_Store;
        private readonly VseGraphView m_GraphView;

        VisualElement m_ErrorIconLabel;
        ToolbarButton m_PreviousErrorButton;
        ToolbarButton m_NextErrorButton;
        Label m_ErrorCounterLabel;

        public GtfErrorToolbar(Store store, VseGraphView graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            name = "errorToolbar";
            AddToClassList("gtf-toolbar");
            styleSheets.Add(
                AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageTransitionHelper.AssetPath +
                    "VisualScripting/Editor/Menu/VseMenu.uss"));

            AssetDatabase
                .LoadAssetAtPath<VisualTreeAsset>(PackageTransitionHelper.AssetPath +
                "VisualScripting/Editor/Menu/GtfErrorToolbar.uxml").CloneTree(this);

            m_ErrorIconLabel = this.MandatoryQ("errorIconLabel");

            m_PreviousErrorButton = this.MandatoryQ<ToolbarButton>("previousErrorButton");
            m_PreviousErrorButton.tooltip = "Go To Previous Error";
            m_PreviousErrorButton.RemoveManipulator(m_PreviousErrorButton.clickable);
            m_PreviousErrorButton.AddManipulator(new Clickable(OnPreviousErrorButton));

            m_NextErrorButton = this.MandatoryQ<ToolbarButton>("nextErrorButton");
            m_NextErrorButton.tooltip = "Go To Next Error";
            m_NextErrorButton.RemoveManipulator(m_NextErrorButton.clickable);
            m_NextErrorButton.AddManipulator(new Clickable(OnNextErrorButton));

            m_ErrorCounterLabel = this.MandatoryQ<Label>("errorCounterLabel");
        }

        void OnPreviousErrorButton()
        {
            m_GraphView.FramePrev(HasErrorBadge);
        }

        void OnNextErrorButton()
        {
            m_GraphView.FrameNext(HasErrorBadge);
        }

        static bool HasErrorBadge(GraphElement element)
        {
            return element.ClassListContains("hasErrorIconBadge");
        }

        public void Update()
        {
            bool enabled = m_Store.GetState().CurrentGraphModel != null;

            int errorCount = 0;

            IGTFGraphModel graphModel = m_Store.GetState().CurrentGraphModel;
            if (graphModel != null)
            {
                if (m_Store.GetState().CompilationResultModel != null)
                {
                    errorCount = (m_Store.GetState().CompilationResultModel?.GetLastResult()?.errors?.Count).GetValueOrDefault(0);
                }
            }

            enabled &= errorCount > 0;

            m_ErrorIconLabel.SetEnabled(enabled);
            m_PreviousErrorButton.SetEnabled(enabled);
            m_NextErrorButton.SetEnabled(enabled);

            m_ErrorCounterLabel.SetEnabled(enabled);
            m_ErrorCounterLabel.text = errorCount + (errorCount == 1 ? " error" : " errors");
        }
    }
}
