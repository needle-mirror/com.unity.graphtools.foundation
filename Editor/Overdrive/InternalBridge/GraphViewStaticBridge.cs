using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;
using UnityEngine.Yoga;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    public static class GraphViewStaticBridge
    {
#if !UNITY_2020_1_OR_NEWER
        public static VisualElement Instantiate(this VisualTreeAsset vta)
        {
            return vta.CloneTree();
        }

#endif

        public static Color EditorPlayModeTint => UIElementsUtility.editorPlayModeTintColor;

        public static void ShowColorPicker(Action<Color> callback, Color initialColor, bool withAlpha)
        {
            ColorPicker.Show(callback, initialColor, withAlpha);
        }

        public static string SearchField(Rect position, string text)
        {
            return EditorGUI.SearchField(position, text);
        }

        public static float RoundToPixelGrid(float v)
        {
            return GUIUtility.RoundToPixelGrid(v);
        }

        public static Vector2 RoundToPixelGrid(Vector2 v)
        {
            return new Vector2(GUIUtility.RoundToPixelGrid(v.x), GUIUtility.RoundToPixelGrid(v.y));
        }

        public static Rect RoundToPixelGrid(Rect r)
        {
            var min = RoundToPixelGrid(r.min);
            var max = RoundToPixelGrid(r.max);
            return new Rect(min, max - min);
        }

        public static float PixelPerPoint => GUIUtility.pixelsPerPoint;

        public static void ApplyWireMaterial()
        {
            HandleUtility.ApplyWireMaterial();
        }

        /* For tests */
        public static Texture2D LoadIconRequired(string path)
        {
            return EditorGUIUtility.LoadIconRequired(path);
        }

        /* For tests */
        public static void SetDisableInputEvents(this EditorWindow window, bool value)
        {
            //window.disableInputEvents = value;
        }

        /* For tests */
        public static void RepaintImmediately(this EditorWindow window)
        {
            window.RepaintImmediately();
        }

        /* For tests */
        public static void ClearPersistentViewData(this EditorWindow window)
        {
            window.ClearPersistentViewData();
        }

        /* For tests */
        public static void DisableViewDataPersistence(this EditorWindow window)
        {
            window.DisableViewDataPersistence();
        }

        /* For tests */
        public static bool HasGUIView(this VisualElement ve)
        {
            GUIView guiView = ve.elementPanel.ownerObject as GUIView;
            return guiView != null;
        }

        /* For tests */
        public static void SetTimeSinceStartupCB(Func<long> cb)
        {
            if (cb == null)
                Panel.TimeSinceStartup = null;
            else
                Panel.TimeSinceStartup = () => cb();
        }

        /* For tests */
        public static void SetDisableThrottling(bool disable)
        {
            DataWatchService.sharedInstance.disableThrottling = disable;
        }

        /* For tests */
        public static bool GetDisableThrottling()
        {
            return DataWatchService.sharedInstance.disableThrottling;
        }

        public static List<EditorWindow> ShowGraphViewWindowWithTools(Type blackboardType, Type minimapType, Type graphViewType)
        {
            const float width = 1200;
            const float height = 800;

            const float toolsWidth = 200;

            var mainSplitView = ScriptableObject.CreateInstance<SplitView>();

            var sideSplitView = ScriptableObject.CreateInstance<SplitView>();
            sideSplitView.vertical = true;
            sideSplitView.position = new Rect(0, 0, toolsWidth, height);
            var dockArea = ScriptableObject.CreateInstance<DockArea>();
            dockArea.position = new Rect(0, 0, toolsWidth, height - toolsWidth);
            var blackboardWindow = ScriptableObject.CreateInstance(blackboardType) as EditorWindow;
            dockArea.AddTab(blackboardWindow);
            sideSplitView.AddChild(dockArea);

            dockArea = ScriptableObject.CreateInstance<DockArea>();
            dockArea.position = new Rect(0, 0, toolsWidth, toolsWidth);
            var minimapWindow = ScriptableObject.CreateInstance(minimapType) as EditorWindow;
            dockArea.AddTab(minimapWindow);
            sideSplitView.AddChild(dockArea);

            mainSplitView.AddChild(sideSplitView);
            dockArea = ScriptableObject.CreateInstance<DockArea>();
            var graphViewWindow = ScriptableObject.CreateInstance(graphViewType) as EditorWindow;
            dockArea.AddTab(graphViewWindow);
            dockArea.position = new Rect(0, 0, width - toolsWidth, height);
            mainSplitView.AddChild(dockArea);

            var containerWindow = ScriptableObject.CreateInstance<ContainerWindow>();
            containerWindow.m_DontSaveToLayout = false;
            containerWindow.position = new Rect(100, 100, width, height);
            containerWindow.rootView = mainSplitView;
            containerWindow.rootView.position = new Rect(0, 0, mainSplitView.position.width, mainSplitView.position.height);

            containerWindow.Show(ShowMode.NormalWindow, false, true, setFocus: true);

            return new List<EditorWindow> { graphViewWindow, blackboardWindow, minimapWindow };
        }

        public static IEnumerable<T> GetGraphViewWindows<T>(Type typeFilter) where T : EditorWindow
        {
            var guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);

            // Get all GraphViews used by existing tool windows of our type
            using (var it = UIElementsUtility.GetPanelsIterator())
            {
                while (it.MoveNext())
                {
                    var dockArea = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key) as DockArea;
                    if (dockArea == null)
                        continue;

                    if (typeFilter == null)
                    {
                        foreach (var window in dockArea.m_Panes.OfType<T>())
                        {
                            yield return window;
                        }
                    }
                    else
                    {
                        foreach (var window in dockArea.m_Panes.Where(p => p.GetType() == typeFilter).Cast<T>())
                        {
                            yield return window;
                        }
                    }
                }
            }
        }

        public static void UpdateScheduledEvents(this VisualElement ve)
        {
            var scheduler = (TimerEventScheduler)((BaseVisualElementPanel)ve.panel).scheduler;
            scheduler.UpdateScheduledEvents();
        }

        public static bool IsLayoutManual(this VisualElement ve)
        {
            return ve.isLayoutManual;
        }

        // Do not use this function in new code. It is here to support old code.
        // Set element dimensions using styles, with position: absolute.
        public static void SetLayout(this VisualElement ve, Rect layout)
        {
            ve.layout = layout;
        }

        public static Rect GetRect(this VisualElement ve)
        {
            return new Rect(0.0f, 0.0f, ve.layout.width, ve.layout.height);
        }

        public static void SetCheckedPseudoState(this VisualElement ve, bool set)
        {
            if (set)
            {
                ve.pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                ve.pseudoStates &= ~PseudoStates.Checked;
            }
        }

        public static void SetDisabledPseudoState(this VisualElement ve, bool set)
        {
            if (set)
            {
                ve.pseudoStates |= PseudoStates.Disabled;
            }
            else
            {
                ve.pseudoStates &= ~PseudoStates.Disabled;
            }
        }

        public static bool GetDisabledPseudoState(this VisualElement ve)
        {
            return (ve.pseudoStates & PseudoStates.Disabled) == PseudoStates.Disabled;
        }

        public static object GetProperty(this VisualElement ve, PropertyName key)
        {
            return ve.GetProperty(key);
        }

        public static void SetProperty(this VisualElement ve, PropertyName key, object value)
        {
            ve.SetProperty(key, value);
        }

        public static void ResetPositionProperties(this VisualElement ve)
        {
            ve.ResetPositionProperties();
        }

        public static Matrix4x4 WorldTransformInverse(this VisualElement ve)
        {
            return ve.worldTransformInverse;
        }

        public static void DrawImmediate(MeshGenerationContext mgc, Action callback)
        {
#if UNITY_2020_1_OR_NEWER
            mgc.painter.DrawImmediate(callback, true);
#else
            mgc.painter.DrawImmediate(callback);
#endif
        }

        public static void SolidRectangle(MeshGenerationContext mgc, Rect rectParams, Color color, ContextType context)
        {
            mgc.Rectangle(MeshGenerationContextUtils.RectangleParams.MakeSolid(rectParams, color, context));
        }

        public static MeshWriteData AllocateMeshWriteData(MeshGenerationContext mgc, int vertexCount, int indexCount)
        {
            return mgc.Allocate(vertexCount, indexCount, null, null, MeshGenerationContext.MeshFlags.UVisDisplacement);
        }

        public static void SetNextVertex(this MeshWriteData md, Vector3 pos, Vector2 uv, Color32 tint)
        {
            Color32 flags = new Color32(0, 0, 0, (byte)VertexFlags.LastType);
            md.SetNextVertex(new Vertex() { position = pos, uv = uv, tint = tint, idsFlags = flags });
        }

        static void MarkYogaNodeSeen(YogaNode node)
        {
            node.MarkLayoutSeen();

            for (int i = 0; i < node.Count; i++)
            {
                MarkYogaNodeSeen(node[i]);
            }
        }

        public static void MarkYogaNodeSeen(this VisualElement ve)
        {
            MarkYogaNodeSeen(ve.yogaNode);
        }

        public static void MarkYogaNodeDirty(this VisualElement ve)
        {
            ve.yogaNode.MarkDirty();
        }

        public static void ForceComputeYogaNodeLayout(this VisualElement ve)
        {
            ve.yogaNode.CalculateLayout();
        }

        public static Vector2 DoMeasure(this VisualElement ve, float desiredWidth, VisualElement.MeasureMode widthMode, float desiredHeight, VisualElement.MeasureMode heightMode)
        {
            return ve.DoMeasure(desiredWidth, widthMode, desiredHeight, heightMode);
        }

        public static bool IsBoundingBoxDirty(this VisualElement ve)
        {
            return ve.isBoundingBoxDirty;
        }

        public static void SetBoundingBoxDirty(this VisualElement ve)
        {
            ve.isBoundingBoxDirty = true;
        }

        public static void SetRequireMeasureFunction(this VisualElement ve)
        {
            ve.requireMeasureFunction = true;
        }

        public static StyleLength GetComputedStyleWidth(this VisualElement ve)
        {
            return ve.computedStyle.width;
        }

        public static void SetRenderHintsForGraphView(this VisualElement ve)
        {
            ve.renderHints = RenderHints.ClipWithScissors;
        }

        public static Vector2 GetWindowScreenPoint(this VisualElement ve)
        {
            GUIView guiView = ve.elementPanel.ownerObject as GUIView;
            if (guiView == null)
                return Vector2.zero;

            return guiView.screenPosition.position;
        }

        public static T MandatoryQ<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            return UQueryExtensions.MandatoryQ<T>(e, name, className);
        }

        public static VisualElement MandatoryQ(this VisualElement e, string name = null, string className = null)
        {
            return UQueryExtensions.MandatoryQ(e, name, className);
        }
    }

    public abstract class VisualElementBridge : VisualElement
    {
        protected virtual void OnGraphElementDataReady() {}

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            OnGraphElementDataReady();
        }

        protected new string GetFullHierarchicalViewDataKey()
        {
            return base.GetFullHierarchicalViewDataKey();
        }

        protected new T GetOrCreateViewData<T>(object existing, string key) where T : class, new()
        {
            return base.GetOrCreateViewData<T>(existing, key);
        }

        protected new void SaveViewData()
        {
            base.SaveViewData();
        }

        public new uint controlid => base.controlid;

        protected void SetIsCompositeRoot()
        {
            isCompositeRoot = true;
        }

        public static void ChangeMouseCursorTo(VisualElement ve, int internalCursorId)
        {
            var cursor = new UnityEngine.UIElements.Cursor();
            cursor.defaultCursorId = internalCursorId;

            ve.elementPanel.cursorManager.SetCursor(cursor);
        }
    }

    public abstract class GraphViewBridge : VisualElementBridge
    {
        protected static class EventCommandNames
        {
            public const string Cut = UnityEngine.EventCommandNames.Cut;
            public const string Copy = UnityEngine.EventCommandNames.Copy;
            public const string Paste = UnityEngine.EventCommandNames.Paste;
            public const string Duplicate = UnityEngine.EventCommandNames.Duplicate;
            public const string Delete = UnityEngine.EventCommandNames.Delete;
            public const string SoftDelete = UnityEngine.EventCommandNames.SoftDelete;
            public const string FrameSelected = UnityEngine.EventCommandNames.FrameSelected;
        }

        static readonly int s_EditorPixelsPerPointId = Shader.PropertyToID("_EditorPixelsPerPoint");
        static readonly int s_GraphViewScaleId = Shader.PropertyToID("_GraphViewScale");

        public VisualElement contentViewContainer { get; protected set; }

        public ITransform viewTransform => contentViewContainer.transform;

        float scale => viewTransform.scale.x;

        // BE AWARE: This is just a stopgap measure to get the minimap notified and should not be used outside of it.
        // This should also get ripped once the minimap is re-written.
        public Action redrawn { get; set; }

#if UNITY_2020_1_OR_NEWER
        void OnBeforeDrawChain(RenderChain renderChain)
#else
        void OnBeforeDrawChain(UIRenderDevice renderChain)
#endif
        {
            Material mat = renderChain.GetStandardMaterial();

            // Set global graph view shader properties (used by UIR)
            mat.SetFloat(s_EditorPixelsPerPointId, EditorGUIUtility.pixelsPerPoint);
            mat.SetFloat(s_GraphViewScaleId, scale);
            redrawn?.Invoke();
        }

        static Shader graphViewShader;

        protected void OnEnterPanel()
        {
            if (panel is BaseVisualElementPanel p)
            {
                if (graphViewShader == null)
#if UNITY_2020_1_OR_NEWER
                    graphViewShader = Shader.Find("Hidden/GraphView/GraphViewUIE");
#else
                    graphViewShader = EditorGUIUtility.LoadRequired("GraphView/GraphViewUIE.shader") as Shader;
#endif

                p.standardShader = graphViewShader;
                HostView ownerView = p.ownerObject as HostView;
                if (ownerView != null && ownerView.actualView != null)
                    ownerView.actualView.antiAliasing = 4;

#if UNITY_2020_1_OR_NEWER
                p.updateMaterial += OnUpdateMaterial;
                p.beforeUpdate += OnBeforeUpdate;
#else
                // Changing the updaters is assumed not to be a normal use case, except maybe for Unity debugging
                // purposes. For that reason, we don't track updater changes.
                Panel.BeforeUpdaterChange += OnBeforeUpdaterChange;
                Panel.AfterUpdaterChange += OnAfterUpdaterChange;
                UpdateDrawChainRegistration(true);
#endif
            }

            // Force DefaultCommonDark.uss since GraphView only has a dark style at the moment
            UIElementsEditorUtility.ForceDarkStyleSheet(this);
        }

        protected void OnLeavePanel()
        {
#if UNITY_2020_1_OR_NEWER
            if (panel is BaseVisualElementPanel p)
            {
                p.beforeUpdate -= OnBeforeUpdate;
                p.updateMaterial -= OnUpdateMaterial;
            }
#else
            // ReSharper disable once DelegateSubtraction
            Panel.BeforeUpdaterChange -= OnBeforeUpdaterChange;

            // ReSharper disable once DelegateSubtraction
            Panel.AfterUpdaterChange -= OnAfterUpdaterChange;
            UpdateDrawChainRegistration(false);
#endif
        }

#if UNITY_2020_1_OR_NEWER
        void OnBeforeUpdate(IPanel panel)
        {
            redrawn?.Invoke();
        }

        void OnUpdateMaterial(Material mat)
        {
            // Set global graph view shader properties (used by UIR)
            mat.SetFloat(s_EditorPixelsPerPointId, EditorGUIUtility.pixelsPerPoint);
            mat.SetFloat(s_GraphViewScaleId, scale);
        }

#else
        void OnBeforeUpdaterChange()
        {
            UpdateDrawChainRegistration(false);
        }

        void OnAfterUpdaterChange()
        {
            UpdateDrawChainRegistration(true);
        }

        void UpdateDrawChainRegistration(bool register)
        {
            var p = panel as BaseVisualElementPanel;
            UIRRepaintUpdater updater = p?.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
            if (updater != null)
            {
                if (register)
                    updater.BeforeDrawChain += OnBeforeDrawChain;
                else updater.BeforeDrawChain -= OnBeforeDrawChain;
            }
        }

#endif
    }

    public abstract class GraphViewToolWindowBridge : EditorWindow
    {
        public abstract void SelectGraphViewFromWindow(GraphViewEditorWindowBridge window, GraphViewBridge graphView, int graphViewIndexInWindow = 0);
    }

    public abstract class GraphViewEditorWindowBridge : EditorWindow {}
}
