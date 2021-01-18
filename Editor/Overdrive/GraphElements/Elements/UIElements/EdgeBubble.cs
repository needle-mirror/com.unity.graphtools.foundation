using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class EdgeBubble : Label
    {
        public new static readonly string ussClassName = "ge-edge-bubble";

        Attacher m_Attacher;

        TextField TextField { get; }

        public override string text
        {
            get => base.text;
            set
            {
                if (base.text == value)
                    return;
                base.text = value;
            }
        }

        public EdgeBubble()
        {
            TextField = new TextField { isDelayed = true };

            AddToClassList(ussClassName);
        }

        void OnBlur(BlurEvent evt)
        {
            SaveAndClose();
        }

        void SaveAndClose()
        {
            text = TextField.text;
            Close();
        }

        void Close()
        {
            TextField.value = text;
            TextField.RemoveFromHierarchy();
            TextField.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            TextField.UnregisterCallback<BlurEvent>(OnBlur);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.KeypadEnter:
                case KeyCode.Return:
                    SaveAndClose();
                    break;
                case KeyCode.Escape:
                    Close();
                    break;
            }
        }

        public void AttachTo(VisualElement edgeControlTarget, SpriteAlignment align)
        {
            if (m_Attacher?.Target == edgeControlTarget && m_Attacher?.Alignment == align)
                return;

            Detach();

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Attacher = new Attacher(this, edgeControlTarget, align);
        }

        public void Detach()
        {
            if (m_Attacher == null)
                return;

            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Attacher.Detach();
            m_Attacher = null;
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ComputeTextSize();
        }

        public void SetAttacherOffset(Vector2 offset)
        {
            if (m_Attacher != null)
                m_Attacher.Offset = offset;
        }

        void ComputeTextSize()
        {
            if (style.fontSize == 0)
                return;

            var newSize = DoMeasure(resolvedStyle.maxWidth.value, MeasureMode.AtMost, 0, MeasureMode.Undefined);

            style.width = newSize.x +
                resolvedStyle.marginLeft +
                resolvedStyle.marginRight +
                resolvedStyle.borderLeftWidth +
                resolvedStyle.borderRightWidth +
                resolvedStyle.paddingLeft +
                resolvedStyle.paddingRight;

            style.height = newSize.y +
                resolvedStyle.marginTop +
                resolvedStyle.marginBottom +
                resolvedStyle.borderTopWidth +
                resolvedStyle.borderBottomWidth +
                resolvedStyle.paddingTop +
                resolvedStyle.paddingBottom;
        }
    }
}
