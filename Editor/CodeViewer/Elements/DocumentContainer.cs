using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.EditorCommon;
using UnityEditor.VisualScripting.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.CodeViewer
{
    class DocumentContainer : VisualElement
    {
        readonly Store m_Store;
        readonly ListView m_DocumentListView;
        readonly VisualTreeAsset m_LineTemplate;
        readonly VisualTreeAsset m_DecoratorTemplate;

        int m_ItemHeight = 20;
        int itemHeight => m_ItemHeight;
        float m_ItemInterPadding = 8f;
        float itemInterPadding => m_ItemInterPadding;
        float m_DigitWidth = 8f;
        float digitWidth => m_DigitWidth;

        const string k_TemplatePath = PackageTransitionHelper.AssetPath + "CodeViewer/Elements/";

        static readonly CustomStyleProperty<int> k_ItemHeight = new CustomStyleProperty<int>("--unity-item-height");
        static readonly CustomStyleProperty<float> k_ItemInterPadding = new CustomStyleProperty<float>("--unity-item-inter-padding");
        static readonly CustomStyleProperty<float> k_DigitWidth = new CustomStyleProperty<float>("--unity-digit-width");

        const string k_DocumentListViewName = "documentListView";

        public DocumentContainer(Store store)
        {
            m_Store = store;
            var documentTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath + "DocumentContainer.uxml");
            m_LineTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath + "LineContainer.uxml");
            m_DecoratorTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath + "LineDecorator.uxml");

            // Create the code viewer elements hierarchy
            ClearClassList();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_TemplatePath + "CodeViewer.uss"));
            this.StretchToParentSize();
            documentTemplate.CloneTree(this);
            m_DocumentListView = this.MandatoryQ<ListView>(k_DocumentListViewName);
            m_DocumentListView.focusable = true;
            m_DocumentListView.tabIndex = 1;
#if UNITY_2020_1_OR_NEWER
            m_DocumentListView.onItemsChosen += OnDocumentListViewItemSelectedEvent;
#else
            m_DocumentListView.onItemChosen += OnDocumentListViewItemSelectedEvent;
#endif
            m_DocumentListView.selectionType = SelectionType.Multiple;

            // hack: the scrollview #unity-content-container's width is hardcoded to a value big enough to be able
            // to scroll horizontally
            var scrollView = m_DocumentListView.Q<ScrollView>();
            scrollView.showHorizontal = true;

            UpdateUI();

            var optionsButton = this.MandatoryQ("optionsButton");
            optionsButton.AddManipulator(new Clickable(OnOptionsButton));

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<KeyDownEvent>(evt =>
            {
                switch (evt.keyCode)
                {
                    case KeyCode.LeftArrow:
                        ScrollHorizontal(scrollView, -1);
                        evt.StopImmediatePropagation();
                        break;
                    case KeyCode.RightArrow:
                        ScrollHorizontal(scrollView, 1);
                        evt.StopImmediatePropagation();
                        break;
                }
            });
        }

        void ScrollHorizontal(ScrollView scrollView, int dir)
        {
            scrollView.scrollOffset += dir * Vector2.right * 20;
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (evt.commandName == "Copy")
            {
                CopyFullText();
            }
        }

        void CopyFullText()
        {
            string fullText = m_Store.GetState().Document.FullText;
            EditorGUIUtility.systemCopyBuffer = fullText;
        }

        static void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (evt.commandName == "Copy")
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(k_ItemHeight, out var newItemHeight))
                m_ItemHeight = newItemHeight;

            if (evt.customStyle.TryGetValue(k_ItemInterPadding, out var newItemInterPadding))
                m_ItemInterPadding = newItemInterPadding;

            if (evt.customStyle.TryGetValue(k_DigitWidth, out var newDigitWidth))
                m_DigitWidth = newDigitWidth;
        }

        public void UpdateUI()
        {
            m_DocumentListView.itemsSource = ToList();
            m_DocumentListView.itemHeight = ItemHeight;
            m_DocumentListView.bindItem = Bind;
            m_DocumentListView.makeItem = MakeItem;
            m_DocumentListView.Refresh();
        }

#if UNITY_2020_1_OR_NEWER
        void OnDocumentListViewItemSelectedEvent(IEnumerable<object> o)
#else
        void OnDocumentListViewItemSelectedEvent(object o)
#endif
        {
#if UNITY_2020_1_OR_NEWER
            if (o.FirstOrDefault() is Line line)
#else
            if (o is Line line)
#endif
            {
                m_Store.Dispatch(new ActivateLineAction(line));
            }
        }

        void OnOptionsButton()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Line Number"), m_Store.GetState().ViewerSettings.ShowLineNumber,
                text => { m_Store.Dispatch(new ToggleShowLineNumberAction()); }, null);
            menu.AddItem(new GUIContent("Line Icons"), m_Store.GetState().ViewerSettings.ShowLineIcons,
                text => { m_Store.Dispatch(new ToggleShowLineIconsAction()); }, null);
            menu.AddItem(new GUIContent("Copy entire text"), false , CopyFullText);
            menu.ShowAsContext();
        }

        IList ToList()
        {
            var document = m_Store.GetState().Document;
            return document?.Lines.ToList();
        }

        int ItemHeight => itemHeight;

        VisualElement MakeItem()
        {
            return m_LineTemplate.CloneTree().MandatoryQ("lineContainer");
        }

        static Regex s_KeywordsRegex = new Regex(@"=|\[|\]|(""[^""]*"")|(\b(abstract|add|base|bool|break|byte|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|get|goto|if|implicit|in|int|interface|internal|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|remove|return|sbyte|sealed|set|short|sizeof|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unsafe|ushort|using|var|virtual|where|while)\b)", RegexOptions.Compiled);

        void Bind(VisualElement target, int index)
        {
            var line = m_Store.GetState().Document.Lines[index];

            // Add line number
            var lineNumber = target.MandatoryQ<Label>("lineNumber");
            if (m_Store.GetState().ViewerSettings.ShowLineNumber)
            {
                lineNumber.text = line.LineNumber.ToString();
                lineNumber.style.width = m_Store.GetState().Document.Lines.Count.ToString().Length * digitWidth + itemInterPadding * 2;
                lineNumber.style.paddingLeft = itemInterPadding;
                lineNumber.style.paddingRight = itemInterPadding;
            }
            else
            {
                lineNumber.text = "";
                lineNumber.style.width = 0;
                lineNumber.style.paddingLeft = 0;
                lineNumber.style.paddingRight = 0;
            }


            // Add decorators
            var decorators = target.MandatoryQ("lineDecorators");
            decorators.Clear();

            if (m_Store.GetState().ViewerSettings.ShowLineIcons)
            {
                foreach (var decorator in line.Decorators)
                {
                    var lineDecorator = m_DecoratorTemplate.CloneTree().MandatoryQ("lineDecorator");
                    var icon = lineDecorator.MandatoryQ("lineDecoratorIcon");
                    icon.style.backgroundImage = decorator.Icon;
                    icon.style.width = decorator.Icon.width;
                    icon.style.height = decorator.Icon.height;
                    lineDecorator.tooltip = decorator.Tooltip;
                    decorators.Add(lineDecorator);
                    break;
                }
                decorators.style.paddingLeft = itemInterPadding;
                decorators.style.paddingRight = itemInterPadding;
            }
            else
            {
                decorators.style.width = 0;
                decorators.style.paddingLeft = 0;
                decorators.style.paddingRight = 0;
            }

            // Add text
            var lineText = target.MandatoryQ<Label>("lineText");
            lineText.text = line.Text;
            target.MandatoryQ("lineContainer").tooltip = line.Text;
            lineText.style.paddingLeft = itemInterPadding;
            var lineText2 = target.MandatoryQ<Label>("lineText2");

            MatchCollection matches = s_KeywordsRegex.Matches(lineText.text);
            int prev = 0;
            StringBuilder sb = new StringBuilder(lineText.text.Length);
            foreach (Match match in matches)
            {
                if (prev != match.Index)
                    sb.Append(' ', match.Index - prev);
                sb.Append(match.Value);
                prev = match.Index + match.Length;
            }
            lineText2.text = sb.ToString();
            lineText2.style.paddingLeft = itemInterPadding;
        }
    }
}
