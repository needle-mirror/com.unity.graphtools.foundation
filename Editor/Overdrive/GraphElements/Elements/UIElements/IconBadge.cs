using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class IconBadge : VisualElement
    {
        static CustomStyleProperty<int> s_DistanceProperty = new CustomStyleProperty<int>("--distance");

        VisualElement m_TipElement;
        VisualElement m_IconElement;
        Label m_TextElement;

        SpriteAlignment alignment { get; set; }
        VisualElement target { get; set; }

        string badgeText
        {
            set
            {
                if (m_TextElement != null)
                {
                    m_TextElement.text = value;
                }
            }
        }

        string visualStyle
        {
            set
            {
                if (m_BadgeType != value)
                {
                    string modifier = "--" + m_BadgeType;

                    RemoveFromClassList(ussClassName + modifier);

                    m_TipElement?.RemoveFromClassList(tipUssClassName + modifier);
                    m_IconElement?.RemoveFromClassList(iconUssClassName + modifier);
                    m_TextElement?.RemoveFromClassList(textUssClassName + modifier);

                    m_BadgeType = value;

                    modifier = "--" + m_BadgeType;

                    AddToClassList(ussClassName + modifier);

                    m_TipElement?.AddToClassList(tipUssClassName + modifier);
                    m_IconElement?.AddToClassList(iconUssClassName + modifier);
                    m_TextElement?.AddToClassList(textUssClassName + modifier);
                }
            }
        }

        const int kDefaultDistanceValue = 6;

        int m_Distance;

        int m_CurrentTipAngle;

        Attacher m_Attacher;
        bool m_IsAttached;
        VisualElement m_OriginalParent;

        Attacher m_TextAttacher;
        string m_BadgeType;

        public IconBadge()
        {
            m_IsAttached = false;
            m_Distance = kDefaultDistanceValue;
            var tpl = GraphElementHelper.LoadUXML("IconBadge.uxml");

            LoadTemplate(tpl);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            visualStyle = "error";
        }

        public IconBadge(VisualTreeAsset template)
        {
            m_IsAttached = false;
            m_Distance = kDefaultDistanceValue;
            LoadTemplate(template);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            visualStyle = "error";
        }

        public static readonly string ussClassName = "icon-badge";
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");
        public static readonly string tipUssClassName = ussClassName.WithUssElement("tip");
        public static readonly string textUssClassName = ussClassName.WithUssElement("text");

        static readonly string defaultStylePath = "IconBadge.uss";

        void LoadTemplate(VisualTreeAsset tpl)
        {
            tpl.CloneTree(this);

            InitBadgeComponent("tip", tipUssClassName, out m_TipElement);
            InitBadgeComponent("icon", iconUssClassName, out m_IconElement);
            InitBadgeComponent("text", textUssClassName, out m_TextElement);

            ////we make sure the tip is in the back
            m_TipElement?.SendToBack();

            name = "IconBadge";
            AddToClassList(ussClassName);

            this.AddStylesheet(defaultStylePath);

            if (m_TextElement != null)
            {
                m_TextElement.RemoveFromHierarchy();
                //we need to add the style sheet to the Text element as well since it will be parented elsewhere
                m_TextElement.AddStylesheet(defaultStylePath);
                m_TextElement.style.whiteSpace = WhiteSpace.Normal;
                m_TextElement.RegisterCallback<GeometryChangedEvent>((evt) => ComputeTextSize());
                m_TextElement.pickingMode = PickingMode.Ignore;
            }
        }

        void InitBadgeComponent<ElementType>(string elementName, string elementClassName, out ElementType outElement) where ElementType : VisualElement
        {
            outElement = this.Q<ElementType>(elementName);

            if (outElement == null)
            {
                Debug.Log($"IconBadge: Couldn't load {elementName} element from template");
            }
            else
            {
                outElement.AddToClassList(elementClassName);
            }
        }

        public static IconBadge CreateError(string message)
        {
            var result = new IconBadge();
            result.visualStyle = "error";
            result.badgeText = message;
            return result;
        }

        public static IconBadge CreateComment(string message)
        {
            var result = new IconBadge();
            result.visualStyle = "comment";
            result.badgeText = message;
            return result;
        }

        public void AttachTo(VisualElement badgeTarget, SpriteAlignment align)
        {
            Detach();
            alignment = align;
            this.target = badgeTarget;
            m_IsAttached = true;
            badgeTarget.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
            CreateAttacher();
        }

        public void Detach()
        {
            if (m_IsAttached)
            {
                target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                m_IsAttached = false;
            }
            ReleaseAttacher();
            m_OriginalParent = null;
        }

        void OnTargetDetachedFromPanel(DetachFromPanelEvent evt)
        {
            ReleaseAttacher();
            if (m_IsAttached)
            {
                m_OriginalParent = hierarchy.parent;
                RemoveFromHierarchy();

                target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                target.RegisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
            }
        }

        void OnTargetAttachedToPanel(AttachToPanelEvent evt)
        {
            if (m_IsAttached)
            {
                target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);

                //we re-add ourselves to the hierarchy
                if (m_OriginalParent != null)
                {
                    m_OriginalParent.hierarchy.Add(this);
                }
                if (m_Attacher != null)
                {
                    ReleaseAttacher();
                }
                CreateAttacher();
            }
        }

        void ReleaseAttacher()
        {
            if (m_Attacher != null)
            {
                m_Attacher.Detach();
                m_Attacher = null;
            }
        }

        void CreateAttacher()
        {
            m_Attacher = new Attacher(this, target, alignment);
            m_Attacher.distance = m_Distance;
        }

        void ComputeTextSize()
        {
            if (m_TextElement != null)
            {
                float maxWidth = m_TextElement.resolvedStyle.maxWidth == StyleKeyword.None ? float.NaN : m_TextElement.resolvedStyle.maxWidth.value;
                Vector2 newSize = m_TextElement.DoMeasure(maxWidth, MeasureMode.AtMost,
                    0, MeasureMode.Undefined);

                m_TextElement.style.width = newSize.x +
                    m_TextElement.resolvedStyle.marginLeft +
                    m_TextElement.resolvedStyle.marginRight +
                    m_TextElement.resolvedStyle.borderLeftWidth +
                    m_TextElement.resolvedStyle.borderRightWidth +
                    m_TextElement.resolvedStyle.paddingLeft +
                    m_TextElement.resolvedStyle.paddingRight;

                float height = newSize.y +
                    m_TextElement.resolvedStyle.marginTop +
                    m_TextElement.resolvedStyle.marginBottom +
                    m_TextElement.resolvedStyle.borderTopWidth +
                    m_TextElement.resolvedStyle.borderBottomWidth +
                    m_TextElement.resolvedStyle.paddingTop +
                    m_TextElement.resolvedStyle.paddingBottom;

                m_TextElement.style.height = height;

                if (m_TextAttacher != null)
                {
                    m_TextAttacher.offset = new Vector2(0, height);
                }

                PerformTipLayout();
            }
        }

        void ShowText()
        {
            if (m_TextElement != null && m_TextElement.hierarchy.parent == null)
            {
                VisualElement textParent = this;

                GraphView gv = GetFirstAncestorOfType<GraphView>();
                if (gv != null)
                {
                    textParent = gv;
                }

                textParent.Add(m_TextElement);

                if (textParent != this)
                {
                    if (m_TextAttacher == null)
                    {
                        m_TextAttacher = new Attacher(m_TextElement, m_IconElement, SpriteAlignment.TopRight);
                    }
                    else
                    {
                        m_TextAttacher.Reattach();
                    }
                }
                m_TextAttacher.distance = 0;
                m_TextElement.ResetPositionProperties();

                ComputeTextSize();
            }
        }

        void HideText()
        {
            if (m_TextElement != null && m_TextElement.hierarchy.parent != null)
            {
                m_TextAttacher?.Detach();
                m_TextElement.RemoveFromHierarchy();
            }
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                if (m_Attacher != null)
                    PerformTipLayout();
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
            {
                if (m_Attacher != null)
                {
                    m_Attacher.Detach();
                    m_Attacher = null;
                }

                HideText();
            }
            else if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                //we make sure we sit on top of whatever siblings we have
                BringToFront();
                ShowText();
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId())
            {
                HideText();
            }


            base.ExecuteDefaultAction(evt);
        }

        void PerformTipLayout()
        {
            float contentWidth = resolvedStyle.width;

            float arrowWidth = 0;
            float arrowLength = 0;

            if (m_TipElement != null)
            {
                arrowWidth = m_TipElement.resolvedStyle.width;
                arrowLength = m_TipElement.resolvedStyle.height;
            }

            float iconSize = 0f;
            if (m_IconElement != null)
            {
                iconSize = m_IconElement.GetComputedStyleWidth() == StyleKeyword.Auto ? contentWidth - arrowLength : m_IconElement.GetComputedStyleWidth().value.value;
            }

            float arrowOffset = Mathf.Floor((iconSize - arrowWidth) * 0.5f);

            Rect iconRect = new Rect(0, 0, iconSize, iconSize);
            float iconOffset = Mathf.Floor((contentWidth - iconSize) * 0.5f);

            Rect tipRect = new Rect(0, 0, arrowWidth, arrowLength);

            int tipAngle = 0;
            Vector2 tipTranslate = Vector2.zero;
            bool tipVisible = true;

            switch (alignment)
            {
                case SpriteAlignment.TopCenter:
                    iconRect.x = iconOffset;
                    iconRect.y = 0;
                    tipRect.x = iconOffset + arrowOffset;
                    tipRect.y = iconRect.height;
                    tipTranslate = new Vector2(arrowWidth, arrowLength);
                    tipAngle = 180;
                    break;
                case SpriteAlignment.LeftCenter:
                    iconRect.y = iconOffset;
                    tipRect.x = iconRect.width;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(arrowLength, 0);
                    tipAngle = 90;
                    break;
                case SpriteAlignment.RightCenter:
                    iconRect.y = iconOffset;
                    iconRect.x += arrowLength;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(0, arrowWidth);
                    tipAngle = 270;
                    break;
                case SpriteAlignment.BottomCenter:
                    iconRect.x = iconOffset;
                    iconRect.y = arrowLength;
                    tipRect.x = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(0, 0);
                    tipAngle = 0;
                    break;
                default:
                    tipVisible = false;
                    break;
            }

            if (tipAngle != m_CurrentTipAngle)
            {
                if (m_TipElement != null)
                {
                    m_TipElement.transform.rotation = Quaternion.Euler(new Vector3(0, 0, tipAngle));
                    m_TipElement.transform.position = new Vector3(tipTranslate.x, tipTranslate.y, 0);
                }
                m_CurrentTipAngle = tipAngle;
            }


            if (m_IconElement != null)
                m_IconElement.SetLayout(iconRect);

            if (m_TipElement != null)
            {
                m_TipElement.SetLayout(tipRect);

                if (m_TipElement.visible != tipVisible)
                {
                    if (tipVisible)
                        m_TipElement.style.visibility = StyleKeyword.Null;
                    else
                        m_TipElement.style.visibility = Visibility.Hidden;
                }
            }

            if (m_TextElement != null)
            {
                if (m_TextElement.parent == this)
                {
                    m_TextElement.style.position = Position.Absolute;
                    m_TextElement.style.left = iconRect.xMax;
                    m_TextElement.style.top = iconRect.y;
                }
            }
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(s_DistanceProperty, out var dist))
                m_Distance = dist;
        }
    }
}
