using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{
    public class PrintResultPart : BaseModelUIPart
    {
        public static readonly string ussClassName = "print-result-part";

        public static PrintResultPart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
        {
            if (model is MathResult)
            {
                return new PrintResultPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        VisualElement m_Root;

        public Button Button { get; private set; }
        public override VisualElement Root => m_Root;

        protected PrintResultPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        void OnPrintResult()
        {
            float result = (m_Model as MathResult)?.Evaluate() ?? 0.0f;

            Debug.Log($"Result is {result}");
        }

        protected override void BuildPartUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            Button = new Button() { text = "Print Result" };
            Button.clicked += OnPrintResult;
            m_Root.Add(Button);

            container.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
        }
    }
}
