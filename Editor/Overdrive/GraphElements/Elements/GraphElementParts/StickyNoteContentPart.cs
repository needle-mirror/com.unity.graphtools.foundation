using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class StickyNoteContentPart : BaseGraphElementPart
    {
        public static StickyNoteContentPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IGTFStickyNoteModel)
            {
                return new StickyNoteContentPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected EditableLabel TextLabel { get; set; }

        public override VisualElement Root => TextLabel;

        protected StickyNoteContentPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IGTFStickyNoteModel)
            {
                TextLabel = new EditableLabel { name = PartName };
                TextLabel.multiline = true;
                TextLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
                TextLabel.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                container.Add(TextLabel);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (TextLabel != null)
            {
                var value = (m_Model as IGTFStickyNoteModel)?.Contents ?? String.Empty;
                TextLabel.SetValueWithoutNotify(value);
            }
        }

        protected void OnRename(ChangeEvent<string> e)
        {
            m_OwnerElement.Store.Dispatch(new UpdateStickyNoteAction(m_Model as IGTFStickyNoteModel, null, e.newValue));
        }
    }
}
