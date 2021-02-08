using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Searcher;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    class TracingToolbar : Toolbar
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

        TracingTimeline m_TracingTimeline;

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

        public TracingToolbar(GraphView graphView, CommandDispatcher commandDispatcher) : base(commandDispatcher, graphView)
        {
            name = "tracingToolbar";
            AddToClassList(ussClassName);
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetHelper.AssetPath + "VisualScripting/Editor/Plugins/Debugging/TracingToolbar.uxml").CloneTree(this);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.AssetPath + "VisualScripting/Editor/Plugins/Debugging/Tracing.uss"));

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
            IMGUIContainer imguiContainer;
            imguiContainer = new IMGUIContainer(() =>
            {
                // weird bug if this starts rendering too early
                if (style.display.value == DisplayStyle.None)
                    return;
                var timeRect = new Rect(0, 0, m_GraphView.Window.rootVisualElement.layout.width - (m_PickTargetButton?.layout.xMax ?? 0),  18 /*imguiContainer.layout.height*/);
                m_TracingTimeline.OnGUI(timeRect);
            });

            m_TracingTimeline = new TracingTimeline(m_GraphView, m_CommandDispatcher.GraphToolState);
            Add(imguiContainer);
        }

        public void SyncVisible()
        {
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;
            if (style.display.value == DisplayStyle.Flex != tracingDataModel.TracingEnabled)
                style.display = tracingDataModel.TracingEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnPickTargetButton(EventBase eventBase)
        {
            var state = m_CommandDispatcher.GraphToolState;
            IDebugger debugger = state.GraphModel.Stencil.Debugger;
            var targetIndices = debugger.GetDebuggingTargets(state.GraphModel);
            var items = targetIndices == null ? null : targetIndices.Select(x =>
                (SearcherItem) new TargetSearcherItem(x, debugger.GetTargetLabel(state.GraphModel, x))).ToList();
            if (items == null || !items.Any())
                items = new List<SearcherItem> {new SearcherItem("<No Object found>")};

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, items, "Entities", i =>
            {
                if (i == null || !(i is TargetSearcherItem targetSearcherItem))
                    return true;
                state.TracingState.CurrentTracingTarget = targetSearcherItem.Target;
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
                var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

                int frame = m_CurrentFrameTextField.value;

                frame = Math.Max(0, Math.Min(frame, Time.frameCount));
                tracingDataModel.CurrentTracingFrame = frame;
                tracingDataModel.CurrentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        public void UpdateTracingMenu(bool force = true)
        {
            var state = m_CommandDispatcher.GraphToolState;
            var tracingDataModel = state.TracingState;

            if (EditorApplication.isPlaying && tracingDataModel.TracingEnabled)
            {
                m_PickTargetLabel.text = state.GraphModel?.Stencil?.Debugger?.GetTargetLabel(state.GraphModel, tracingDataModel.CurrentTracingTarget);
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
                    tracingDataModel.CurrentTracingFrame = Time.frameCount;
                    tracingDataModel.CurrentTracingStep = -1;
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
                    m_CurrentFrameTextField.value = tracingDataModel.CurrentTracingFrame;
                    m_TotalFrameLabel.text = $"/{Time.frameCount.ToString()}";
                    if (tracingDataModel.CurrentTracingStep != -1)
                    {
                        m_TotalFrameLabel.text += $" [{tracingDataModel.CurrentTracingStep}/{tracingDataModel.MaxTracingStep}]";
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
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

            tracingDataModel.CurrentTracingFrame = 0;
            tracingDataModel.CurrentTracingStep = -1;
            UpdateTracingMenu();
        }

        void OnPreviousFrameTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

            if (tracingDataModel.CurrentTracingFrame > 0)
            {
                tracingDataModel.CurrentTracingFrame--;
                tracingDataModel.CurrentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnPreviousStepTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

            if (tracingDataModel.CurrentTracingStep > 0)
            {
                tracingDataModel.CurrentTracingStep--;
            }
            else
            {
                if (tracingDataModel.CurrentTracingStep == -1)
                {
                    tracingDataModel.CurrentTracingStep = tracingDataModel.MaxTracingStep;
                }
                else
                {
                    if (tracingDataModel.CurrentTracingFrame > 0)
                    {
                        tracingDataModel.CurrentTracingFrame--;
                        tracingDataModel.CurrentTracingStep = tracingDataModel.MaxTracingStep;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextStepTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

            if (tracingDataModel.CurrentTracingStep < tracingDataModel.MaxTracingStep && tracingDataModel.CurrentTracingStep >= 0)
            {
                tracingDataModel.CurrentTracingStep++;
            }
            else
            {
                if (tracingDataModel.CurrentTracingStep == -1 && (tracingDataModel.CurrentTracingFrame < Time.frameCount))
                {
                    tracingDataModel.CurrentTracingStep = 0;
                }
                else
                {
                    if (tracingDataModel.CurrentTracingFrame < Time.frameCount)
                    {
                        tracingDataModel.CurrentTracingFrame++;
                        tracingDataModel.CurrentTracingStep = 0;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextFrameTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

            if (tracingDataModel.CurrentTracingFrame < Time.frameCount)
            {
                tracingDataModel.CurrentTracingFrame++;
                tracingDataModel.CurrentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnLastFrameTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

            tracingDataModel.CurrentTracingFrame = Time.frameCount;
            tracingDataModel.CurrentTracingStep = -1;
            UpdateTracingMenu();
        }
    }
}
