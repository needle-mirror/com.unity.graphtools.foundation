using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.EditorCommon;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.CodeViewer
{
    [PublicAPI]
    public class CodeViewerWindow : EditorWindow, IHasCustomMenu
    {
        static CodeViewerWindow s_Instance;

        DocumentContainer m_CodeViewContainer;
        Store m_Store;

        [SerializeField]
        LockTracker m_LockTracker = new LockTracker();

        [MenuItem("Window/Code Viewer", false, 2102)]
        static void Init()
        {
            var window = (CodeViewerWindow)GetWindow(typeof(CodeViewerWindow));
            window.Show();
        }

        void Update()
        {
            m_Store.Update();
        }

        protected void OnEnable()
        {
            m_Store = new Store(new CodeViewerState());
            Reducers.Register(m_Store);

            titleContent = new GUIContent("Code Viewer");
            m_CodeViewContainer = new DocumentContainer(m_Store);

            rootVisualElement.Clear();
            rootVisualElement.style.overflow = Overflow.Hidden;
            rootVisualElement.pickingMode = PickingMode.Ignore;
            rootVisualElement.style.flexDirection = FlexDirection.Row;
            rootVisualElement.Add(m_CodeViewContainer);

            m_Store.StateChanged += StoreOnStateChanged;
            Selection.selectionChanged += SelectionChanged;
            m_LockTracker.lockStateChanged.AddListener(OnLockStateChanged);

            OnLockStateChanged(m_LockTracker.IsLocked);

            s_Instance = this;
        }

        protected void OnDisable()
        {
            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= SelectionChanged;
            m_Store.Dispose();
            s_Instance = null;
        }

        void StoreOnStateChanged()
        {
            m_CodeViewContainer.UpdateUI();
        }

        static void SelectionChanged()
        {
            if (Selection.activeObject is MonoScript)
            {
                var script = (MonoScript)Selection.activeObject;
                ShowCode(script.text);
            }
        }

        void OnLockStateChanged(bool locked)
        {
            m_Store.Dispatch(new ChangeLockStateAction(locked));
        }

        static void ShowCode(string code, Action<object> callback = null)
        {
            var document = SplitCode(code);
            document.Callback = callback;
            SetDocument(document);
        }

        public static void SetDocument(Document document)
        {
            if (s_Instance == null)
                return;

            s_Instance.m_Store?.Dispatch(new ChangeDocumentAction(document));

            // Force "Update()" to be called on the CodeViewer's Store in order to refresh the viewer's UI.
            s_Instance.Repaint();
        }

        public static Document SplitCode(string code)
        {
            string[] splitCode = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            List<ILine> lines = new List<ILine>();

            for (int i = 0; i < splitCode.Length; i++)
            {
                lines.Add(new Line(i + 1, splitCode[i]));
            }

            var document = new Document(lines.ToArray());

            return document;
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu);
        }

        protected virtual void ShowButton(Rect r)
        {
            m_LockTracker.ShowButton(r);
        }
    }
}
