using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class ValueBadge : VisualElement
    {
        static VisualTreeAsset s_ValueTemplate;
        Label m_TextElement;
        Attacher m_Attacher;
        VisualElement m_OriginalParent;
        bool m_IsAttached;
        VisualElement m_Target;
        SpriteAlignment m_Alignment;
        Image m_Image;

        public Color BadgeColor
        {
            set
            {
                m_Image.tintColor = value;

                style.borderLeftColor = value;
                style.borderRightColor = value;
                style.borderTopColor = value;
                style.borderBottomColor = value;
            }
        }

        public ValueBadge()
        {
            if (s_ValueTemplate == null)
                s_ValueTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UICreationHelper.templatePath + "ValueBadge.uxml");
            s_ValueTemplate.CloneTree(this);
            m_TextElement = this.Q<Label>("desc");
            m_Image = this.Q<Image>();
        }

        void OnTargetAttachedToPanel(AttachToPanelEvent evt)
        {
            if (!m_IsAttached)
                return;
            m_OriginalParent = hierarchy.parent;
            m_Target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
            m_Target.UnregisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
            m_OriginalParent?.hierarchy.Add(this);
            if (m_Attacher != null)
                ReleaseAttacher();
            CreateAttacher();
        }

        void OnTargetDetachedFromPanel(DetachFromPanelEvent evt)
        {
            ReleaseAttacher();
            if (!m_IsAttached)
                return;
            m_OriginalParent = hierarchy.parent;
            RemoveFromHierarchy();
            m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
            m_Target.RegisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
        }

        void CreateAttacher()
        {
            m_Attacher = new Attacher(this, m_Target, m_Alignment);
            m_Attacher.distance = 0;
            Vector2 kOffset = new Vector2(-8, 5);
            m_Attacher.offset = kOffset;
        }

        void ReleaseAttacher()
        {
            if (m_Attacher == null)
                return;
            m_Attacher.Detach();
            m_Attacher = null;
        }

        public void AttachTo(VisualElement target, SpriteAlignment alignment)
        {
            if (m_IsAttached && target == m_Target)
                return;

            Detach();
            m_Target = target;
            m_Alignment = alignment;
            m_IsAttached = true;
            target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
            CreateAttacher();
        }

        void Detach()
        {
            if (m_IsAttached)
            {
                m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                m_IsAttached = false;
            }

            ReleaseAttacher();
            m_OriginalParent = null;
        }

        public string Text
        {
            get => m_TextElement.text;
            set => m_TextElement.text = value;
        }
    }
}
