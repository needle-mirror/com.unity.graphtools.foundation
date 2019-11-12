using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
#if !UNITY_2020_1_OR_NEWER
    // Heavily based on VFXEditor's sticky notes (in fact most of the code is identical)
    class StickyNote : GraphElement, IHasGraphElementModel, IResizable, IMovable
    {
        public sealed override string title => stickyNoteModel.Title ?? string.Empty;
        string Contents => stickyNoteModel.Contents ?? string.Empty;
        StickyNoteColorTheme Theme => stickyNoteModel.Theme;
        StickyNoteTextSize TextSize => stickyNoteModel.TextSize;

        readonly Store m_Store;
        internal readonly IStickyNoteModel stickyNoteModel;

        readonly Label m_Title;
        TextField m_TitleField;
        readonly Label m_Contents;
        TextField m_ContentsField;
        readonly GraphView m_GraphView;

        bool m_EditTitleCancelled;
        bool m_EditContentsCancelled;

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public IGraphElementModel GraphElementModel => stickyNoteModel;

        public StickyNote(Store store, IStickyNoteModel model, Rect position, GraphView graphView)
        {
            m_Store = store;
            stickyNoteModel = model;
            m_GraphView = graphView;

            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UICreationHelper.templatePath + "StickyNote.uxml");
            template.CloneTree(this);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Selectable.uss"));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "StickyNote.uss"));

            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Selectable | Capabilities.Renamable;

            m_Title = this.MandatoryQ<Label>(name: "title");
            m_Title.text = title;
            m_Title.pickingMode = PickingMode.Ignore;

            m_TitleField = this.MandatoryQ<TextField>("title-field");
            m_TitleField.style.visibility = Visibility.Hidden;
            m_TitleField.isDelayed = true;

            m_Contents = this.MandatoryQ<Label>("contents");
            m_Contents.text = Contents;
            m_Contents.pickingMode = PickingMode.Ignore;

            m_ContentsField = this.MandatoryQ<TextField>(name: "contents-field");
            m_ContentsField.style.visibility = Visibility.Hidden;
            m_ContentsField.multiline = true;
            m_ContentsField.isDelayed = true;

            this.AddManipulator(new ContextualMenuManipulator(OnContextualMenuEvent));

            SetPosition(position);

            AddToClassList("sticky-note");
            AddToClassList("selectable");
            UpdateThemeClasses();
            UpdateSizeClasses();
        }

        void OnFieldFocus(FocusEvent evt)
        {
            VseWindow window = ((VseGraphView)m_GraphView).window;
            if (window != null)
                window.RefreshUIDisabled = true;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnFieldFocusOut()
        {
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            VseWindow window = ((VseGraphView)m_GraphView).window;
            if (window != null)
                window.RefreshUIDisabled = false;
        }

        void UndoRedoPerformed()
        {
            m_TitleField.value = m_Title.text;
            m_ContentsField.value = m_Contents.text;
            m_TitleField.Blur();
            m_ContentsField.Blur();
        }

        void OnTitleFieldKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    m_EditTitleCancelled = true;
                    m_TitleField.Blur();
                    break;
                case KeyCode.Return:
                    m_TitleField.Blur();
                    break;
            }
        }

        void OnContentsFieldKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    m_EditContentsCancelled = true;
                    m_ContentsField.Blur();
                    break;
                case KeyCode.Return:
                    if (!m_ContentsField.multiline || evt.shiftKey)
                        m_ContentsField.Blur();
                    break;
            }
        }

        void OnTitleFieldBlur(BlurEvent evt)
        {
            OnFieldFocusOut();

            m_Title.style.visibility = Visibility.Visible;
            m_TitleField.style.visibility = Visibility.Hidden;

            if (!m_EditTitleCancelled)
            {
                m_Title.text = m_TitleField.text;
                m_Store.Dispatch(new UpdateStickyNoteAction(stickyNoteModel, m_Title.text, Contents));
            }

            m_EditTitleCancelled = false;

            m_TitleField.UnregisterCallback<BlurEvent>(OnTitleFieldBlur);
            m_TitleField.UnregisterCallback<KeyDownEvent>(OnTitleFieldKeyDown);
        }

        void OnContentsFieldBlur(BlurEvent evt)
        {
            OnFieldFocusOut();

            m_Contents.style.visibility = Visibility.Visible;
            m_ContentsField.style.visibility = Visibility.Hidden;

            if (!m_EditContentsCancelled)
            {
                m_Contents.text = m_ContentsField.text;
                m_Store.Dispatch(new UpdateStickyNoteAction(stickyNoteModel, title, m_Contents.text));
            }

            m_EditContentsCancelled = false;

            m_ContentsField.UnregisterCallback<BlurEvent>(OnContentsFieldBlur);
            m_ContentsField.UnregisterCallback<KeyDownEvent>(OnContentsFieldKeyDown);
        }

        void UpdateThemeClasses()
        {
            foreach (StickyNoteColorTheme value in Enum.GetValues(typeof(StickyNoteColorTheme)))
                EnableInClassList("theme-" + value.ToString().ToLower(), value == Theme);
        }

        void UpdateSizeClasses()
        {
            foreach (StickyNoteTextSize value in Enum.GetValues(typeof(StickyNoteTextSize)))
                EnableInClassList("size-" + value.ToString().ToLower(), value == TextSize);
        }

        public void OnStartResize()
        {
        }

        public void OnResized()
        {
            var topLeftOffset = new Vector2(resolvedStyle.marginLeft + resolvedStyle.paddingLeft + resolvedStyle.borderLeftWidth,
                resolvedStyle.marginTop + resolvedStyle.paddingTop + resolvedStyle.borderTopWidth);
            m_Store.Dispatch(new ResizeStickyNoteAction(stickyNoteModel,
                new Rect(layout.position - topLeftOffset, layout.size)));
        }

        public void UpdatePinning()
        {
        }

        public bool NeedStoreDispatch => false;

        public sealed override void SetPosition(Rect rect)
        {
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }

        public override Rect GetPosition()
        {
            return new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        static void UpdateFieldRectFromLabel(Label label, ref TextField textField)
        {
            Rect rect = label.layout;
            IResolvedStyle labelStyle = label.resolvedStyle;

            textField.style.left = rect.xMin;
            textField.style.top = rect.yMin + labelStyle.marginTop;
            textField.style.width = rect.width - labelStyle.marginLeft - labelStyle.marginRight;
            textField.style.height = rect.height - labelStyle.marginTop - labelStyle.marginBottom;
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.eventTypeId == MouseDownEvent.TypeId())
            {
                var e = (MouseDownEvent)evt;

                if ((MouseButton)e.button == MouseButton.LeftMouse && e.clickCount == 2)
                {
                    if (m_Title.ContainsPoint(this.ChangeCoordinatesTo(m_Title, e.localMousePosition)))
                    {
                        GiveFocusToTitleEditor();
                        e.StopImmediatePropagation();
                        return;
                    }
                    else if (m_Contents.ContainsPoint(this.ChangeCoordinatesTo(m_Contents, e.localMousePosition)))
                    {
                        GiveFocusToContentsEditor();
                        e.StopImmediatePropagation();
                        return;
                    }
                }
            }

            base.ExecuteDefaultAction(evt);
        }

        void GiveFocusToTitleEditor()
        {
            var textInput = m_TitleField.Q(TextInputBaseField<string>.textInputUssName);
            m_TitleField.SetValueWithoutNotify(m_Title.text);
            m_TitleField.RegisterCallback<BlurEvent>(OnTitleFieldBlur);
            m_TitleField.RegisterCallback<FocusEvent>(OnFieldFocus);
            textInput.RegisterCallback<KeyDownEvent>(OnTitleFieldKeyDown);    // TODO: Remove when Esc/Return support is added to UIElements.TextField
            m_TitleField.style.visibility = Visibility.Visible;
            textInput.style.visibility = Visibility.Visible;
            m_Title.style.visibility = Visibility.Hidden;
            textInput.Focus();
            m_TitleField.SelectAll();
            UpdateFieldRectFromLabel(m_Title, ref m_TitleField);
        }

        void GiveFocusToContentsEditor()
        {
            var textInput = m_ContentsField.Q(TextInputBaseField<string>.textInputUssName);
            m_ContentsField.SetValueWithoutNotify(m_Contents.text);
            m_ContentsField.RegisterCallback<BlurEvent>(OnContentsFieldBlur);
            m_ContentsField.RegisterCallback<FocusEvent>(OnFieldFocus);
            textInput.RegisterCallback<KeyDownEvent>(OnContentsFieldKeyDown);    // TODO: Remove when Esc/Return support is added to UIElements.TextField
            m_ContentsField.style.visibility = Visibility.Visible;
            textInput.style.visibility = Visibility.Visible;
            m_Contents.style.visibility = Visibility.Hidden;
            textInput.Focus();
            m_ContentsField.SelectAll();
            UpdateFieldRectFromLabel(m_Contents, ref m_ContentsField);
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            m_GraphView.BuildContextualMenu(evt);
        }
    }
#else
    class StickyNote : UnityEditor.Experimental.GraphView.StickyNote, IHasGraphElementModel, IResizable, IMovable
    {
        readonly Store m_Store;
        readonly IStickyNoteModel stickyNoteModel;
        public IGraphElementModel GraphElementModel => stickyNoteModel;

        public bool NeedStoreDispatch => false;

        // ReSharper disable once UnusedParameter.Local
        public StickyNote(Store store, IStickyNoteModel model, Rect position, GraphView graphView)
            : base(position.position)
        {
            m_Store = store;
            stickyNoteModel = model;

            theme = ConvertTheme(model.Theme);
            fontSize = (StickyNoteFontSize)model.TextSize;

            title = model.Title;
            contents = model.Contents;
            SetPosition(position);

            RegisterCallback<StickyNoteChangeEvent>(OnChange);
        }

        StickyNoteTheme ConvertTheme(StickyNoteColorTheme modelTheme)
        {
            switch (modelTheme)
            {
                case StickyNoteColorTheme.Dark:
                    return StickyNoteTheme.Black;
                default:
                    return StickyNoteTheme.Classic;
            }
        }

        public override void OnResized()
        {
            var topLeftOffset = new Vector2(resolvedStyle.marginLeft + resolvedStyle.paddingLeft + resolvedStyle.borderLeftWidth,
                resolvedStyle.marginTop + resolvedStyle.paddingTop + resolvedStyle.borderTopWidth);
            m_Store.Dispatch(new ResizeStickyNoteAction(stickyNoteModel,
                new Rect(layout.position - topLeftOffset, layout.size)));
        }

        void OnChange(StickyNoteChangeEvent evt)
        {
            switch (evt.change)
            {
                case StickyNoteChange.Title:
                case StickyNoteChange.Contents:
                    m_Store.Dispatch(new UpdateStickyNoteAction(stickyNoteModel, title, contents));
                    break;
            }
        }

        public void UpdatePinning()
        {
        }
    }
#endif
}
