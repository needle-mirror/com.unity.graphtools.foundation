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

        public Button Button { get; private set; }
        public override VisualElement Root => Button;

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
            Button = new Button() { text = "Print Result" };
            Button.clicked += OnPrintResult;
            container.Add(Button);
        }

        protected override void UpdatePartFromModel()
        {
        }
    }
}
