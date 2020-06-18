using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.Searcher;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    class TracingToolbar : VisualElement
    {
        class TargetSearcherItem : SearcherItem
        {
            public int Target;

            public TargetSearcherItem(int target, string name, string help = "", List<SearcherItem> children = null) : base(name, help, children)
            {
                Target = target;
            }
        }

        const int k_UpdateIntervalMs = 500;

        private readonly GraphView m_GraphView;
        private readonly Store m_Store;
        private TracingTimeline m_TracingTimeline;

        Button m_PickTargetButton;
        Label m_PickTargetLabel;
        VisualElement m_PickTargetIcon;
        Button m_FirstFrameTracingButton;
        Button m_PreviousStepTracingButton;
        Button m_PreviousFrameTracingButton;
        Button m_NextStepTracingButton;
        Button m_NextFrameTracingButton;
        Button m_LastFrameTracingButton;
        IntegerField m_CurrentFrameTextField;
        Label m_TotalFrameLabel;
        Stopwatch m_LastUpdate = Stopwatch.StartNew();

        public TracingToolbar(GraphView graphView, Store store)
        {
            m_GraphView = graphView;
            m_Store = store;

            name = "tracingToolbar";
            AddToClassList("gtf-toolbar");
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Plugins/Debugging/TracingToolbar.uxml").CloneTree(this);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Menu/VseMenu.uss"));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Plugins/Debugging/Tracing.uss"));

            m_PickTargetButton = this.Q<Button>("pickTargetButton");
            m_PickTargetButton.tooltip = "Pick a target";
            m_PickTargetButton.clickable.clickedWithEventInfo += OnPickTargetButton;
            m_PickTargetLabel = m_PickTargetButton.Q<Label>("pickTargetLabel");
            m_PickTargetLabel.tooltip = "Choose an entity trace to display";
            m_PickTargetIcon = m_PickTargetButton.Q(null, "icon");

            m_FirstFrameTracingButton = this.Q<Button>("firstFrameTracingButton");
            m_FirstFrameTracingButton.tooltip = "Go To First Frame";
            m_FirstFrameTracingButton.clickable.clicked += OnFirstFrameTracingButton;

            m_PreviousFrameTracingButton = this.Q<Button>("previousFrameTracingButton");
            m_PreviousFrameTracingButton.tooltip = "Go To Previous Frame";
            m_PreviousFrameTracingButton.clickable.clicked += OnPreviousFrameTracingButton;

            m_PreviousStepTracingButton = this.Q<Button>("previousStepTracingButton");
            m_PreviousStepTracingButton.tooltip = "Go To Previous Step";
            m_PreviousStepTracingButton.clickable.clicked += OnPreviousStepTracingButton;

            m_NextStepTracingButton = this.Q<Button>("nextStepTracingButton");
            m_NextStepTracingButton.tooltip = "Go To Next Step";
            m_NextStepTracingButton.clickable.clicked += OnNextStepTracingButton;

            m_NextFrameTracingButton = this.Q<Button>("nextFrameTracingButton");
            m_NextFrameTracingButton.tooltip = "Go To Next Frame";
            m_NextFrameTracingButton.clickable.clicked += OnNextFrameTracingButton;

            m_LastFrameTracingButton = this.Q<Button>("lastFrameTracingButton");
            m_LastFrameTracingButton.tooltip = "Go To Last Frame";
            m_LastFrameTracingButton.clickable.clicked += OnLastFrameTracingButton;

            m_CurrentFrameTextField = this.Q<IntegerField>("currentFrameTextField");
            m_CurrentFrameTextField.AddToClassList("frameCounterLabel");
            m_CurrentFrameTextField.RegisterCallback<KeyDownEvent>(OnFrameCounterKeyDown);
            m_TotalFrameLabel = this.Q<Label>("totalFrameLabel");
            m_TotalFrameLabel.AddToClassList("frameCounterLabel");

            AddTracingTimeline();
        }

        protected void AddTracingTimeline()
        {
            IMGUIContainer imguiContainer = null;
            imguiContainer = new IMGUIContainer(() =>
            {
                // weird bug if this starts rendering too early
                if (style.display.value == DisplayStyle.None)
                    return;
                var timeRect = new Rect(0, 0, (m_GraphView as VseGraphView).window.rootVisualElement.layout.width - (m_PickTargetButton?.layout.xMax ?? 0),  18 /*imguiContainer.layout.height*/);
                m_TracingTimeline.OnGUI(timeRect);
            });

            m_TracingTimeline = new TracingTimeline((VseGraphView)m_GraphView, m_Store.GetState(), imguiContainer);
            Add(imguiContainer);
        }

        public void SyncVisible()
        {
            if (style.display.value == DisplayStyle.Flex != m_Store.GetState().EditorDataModel.TracingEnabled)
                style.display = m_Store.GetState().EditorDataModel.TracingEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnPickTargetButton(EventBase eventBase)
        {
            State state = m_Store.GetState();
            IDebugger debugger = state.CurrentGraphModel.Stencil.Debugger;
            var targetIndices = debugger.GetDebuggingTargets(state.CurrentGraphModel);
            var items = targetIndices == null ? null : targetIndices.Select(x =>
                (SearcherItem) new TargetSearcherItem(x, debugger.GetTargetLabel(state.CurrentGraphModel, x))).ToList();
            if (items == null || !items.Any())
                items = new List<SearcherItem> {new SearcherItem("<No Object found>")};

            SearcherWindow.Show(EditorWindow.focusedWindow, items, "Entities", i =>
            {
                if (i == null || !(i is TargetSearcherItem targetSearcherItem))
                    return true;
                state.CurrentTracingTarget = targetSearcherItem.Target;
                UpdateTracingMenu();
                return true;
            }, eventBase.originalMousePosition);
            eventBase.StopPropagation();
            eventBase.PreventDefault();
        }

        void OnFrameCounterKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                int frame = m_CurrentFrameTextField.value;

                frame = Math.Max(0, Math.Min(frame, Time.frameCount));
                m_Store.GetState().CurrentTracingFrame = frame;
                m_Store.GetState().CurrentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        public void UpdateTracingMenu(bool force = true)
        {
            var state = m_Store.GetState();

            if (EditorApplication.isPlaying && state.EditorDataModel.TracingEnabled)
            {
                m_PickTargetLabel.text = state.CurrentGraphModel?.Stencil?.Debugger?.GetTargetLabel(state.CurrentGraphModel, state.CurrentTracingTarget);
                m_PickTargetIcon.style.visibility = Visibility.Hidden;
                m_PickTargetButton.SetEnabled(true);
                if (EditorApplication.isPaused || !EditorApplication.isPlaying)
                {
                    m_FirstFrameTracingButton.SetEnabled(true);
                    m_PreviousFrameTracingButton.SetEnabled(true);
                    m_PreviousStepTracingButton.SetEnabled(true);
                    m_NextStepTracingButton.SetEnabled(true);
                    m_NextFrameTracingButton.SetEnabled(true);
                    m_LastFrameTracingButton.SetEnabled(true);
                    m_CurrentFrameTextField.SetEnabled(true);
                    m_TotalFrameLabel.SetEnabled(true);
                }
                else
                {
                    state.CurrentTracingFrame = Time.frameCount;
                    state.CurrentTracingStep = -1;
                    m_FirstFrameTracingButton.SetEnabled(false);
                    m_PreviousFrameTracingButton.SetEnabled(false);
                    m_PreviousStepTracingButton.SetEnabled(false);
                    m_NextStepTracingButton.SetEnabled(false);
                    m_NextFrameTracingButton.SetEnabled(false);
                    m_LastFrameTracingButton.SetEnabled(false);
                    m_CurrentFrameTextField.SetEnabled(false);
                    m_TotalFrameLabel.SetEnabled(false);
                }

                if (!m_LastUpdate.IsRunning)
                    m_LastUpdate.Start();
                if (force || EditorApplication.isPaused || m_LastUpdate.ElapsedMilliseconds > k_UpdateIntervalMs)
                {
                    m_CurrentFrameTextField.value = state.CurrentTracingFrame;
                    m_TotalFrameLabel.text = $"/{Time.frameCount.ToString()}";
                    if (state.CurrentTracingStep != -1)
                    {
                        m_TotalFrameLabel.text += $" [{state.CurrentTracingStep}/{state.MaxTracingStep}]";
                    }

                    m_LastUpdate.Restart();
                }
            }
            else
            {
                m_LastUpdate.Stop();
                m_PickTargetLabel.text = "";
                m_PickTargetIcon.style.visibility = StyleKeyword.Null;
                m_CurrentFrameTextField.value = 0;
                m_PickTargetButton.SetEnabled(false);
                m_CurrentFrameTextField.SetEnabled(false);
                m_TotalFrameLabel.text = "/0";
                m_TotalFrameLabel.SetEnabled(false);
                m_FirstFrameTracingButton.SetEnabled(false);
                m_PreviousFrameTracingButton.SetEnabled(false);
                m_PreviousStepTracingButton.SetEnabled(false);
                m_NextStepTracingButton.SetEnabled(false);
                m_NextFrameTracingButton.SetEnabled(false);
                m_LastFrameTracingButton.SetEnabled(false);
            }
        }

        void OnFirstFrameTracingButton()
        {
            m_Store.GetState().CurrentTracingFrame = 0;
            m_Store.GetState().CurrentTracingStep = -1;
            UpdateTracingMenu();
        }

        void OnPreviousFrameTracingButton()
        {
            if (m_Store.GetState().CurrentTracingFrame > 0)
            {
                m_Store.GetState().CurrentTracingFrame--;
                m_Store.GetState().CurrentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnPreviousStepTracingButton()
        {
            if (m_Store.GetState().CurrentTracingStep > 0)
            {
                m_Store.GetState().CurrentTracingStep--;
            }
            else
            {
                if (m_Store.GetState().CurrentTracingStep == -1)
                {
                    m_Store.GetState().CurrentTracingStep = m_Store.GetState().MaxTracingStep;
                }
                else
                {
                    if (m_Store.GetState().CurrentTracingFrame > 0)
                    {
                        m_Store.GetState().CurrentTracingFrame--;
                        m_Store.GetState().CurrentTracingStep = m_Store.GetState().MaxTracingStep;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextStepTracingButton()
        {
            if (m_Store.GetState().CurrentTracingStep < m_Store.GetState().MaxTracingStep && m_Store.GetState().CurrentTracingStep >= 0)
            {
                m_Store.GetState().CurrentTracingStep++;
            }
            else
            {
                if (m_Store.GetState().CurrentTracingStep == -1 && (m_Store.GetState().CurrentTracingFrame < Time.frameCount))
                {
                    m_Store.GetState().CurrentTracingStep = 0;
                }
                else
                {
                    if (m_Store.GetState().CurrentTracingFrame < Time.frameCount)
                    {
                        m_Store.GetState().CurrentTracingFrame++;
                        m_Store.GetState().CurrentTracingStep = 0;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextFrameTracingButton()
        {
            if (m_Store.GetState().CurrentTracingFrame < Time.frameCount)
            {
                m_Store.GetState().CurrentTracingFrame++;
                m_Store.GetState().CurrentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnLastFrameTracingButton()
        {
            m_Store.GetState().CurrentTracingFrame = Time.frameCount;
            m_Store.GetState().CurrentTracingStep = -1;
            UpdateTracingMenu();
        }
    }
}
