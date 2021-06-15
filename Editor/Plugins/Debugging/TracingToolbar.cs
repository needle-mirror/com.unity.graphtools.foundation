using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    // PF FIXME use commands for all user interactions
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
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetHelper.AssetPath + "Plugins/Debugging/TracingToolbar.uxml").CloneTree(this);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.AssetPath + "Plugins/Debugging/Tracing.uss"));

            m_PickTargetButton = this.SafeQ<Button>("pickTargetButton");
            m_PickTargetButton.tooltip = "Pick a target";
            m_PickTargetButton.clickable.clickedWithEventInfo += OnPickTargetButton;
            m_PickTargetLabel = m_PickTargetButton.SafeQ<Label>("pickTargetLabel");
            m_PickTargetLabel.tooltip = "Choose an entity trace to display";
            m_PickTargetIcon = m_PickTargetButton.SafeQ(null, "icon");

            m_FirstFrameTracingButton = this.SafeQ<Button>("firstFrameTracingButton");
            m_FirstFrameTracingButton.tooltip = "Go To First Frame";
            m_FirstFrameTracingButton.clickable.clicked += OnFirstFrameTracingButton;

            m_PreviousFrameTracingButton = this.SafeQ<Button>("previousFrameTracingButton");
            m_PreviousFrameTracingButton.tooltip = "Go To Previous Frame";
            m_PreviousFrameTracingButton.clickable.clicked += OnPreviousFrameTracingButton;

            m_PreviousStepTracingButton = this.SafeQ<Button>("previousStepTracingButton");
            m_PreviousStepTracingButton.tooltip = "Go To Previous Step";
            m_PreviousStepTracingButton.clickable.clicked += OnPreviousStepTracingButton;

            m_NextStepTracingButton = this.SafeQ<Button>("nextStepTracingButton");
            m_NextStepTracingButton.tooltip = "Go To Next Step";
            m_NextStepTracingButton.clickable.clicked += OnNextStepTracingButton;

            m_NextFrameTracingButton = this.SafeQ<Button>("nextFrameTracingButton");
            m_NextFrameTracingButton.tooltip = "Go To Next Frame";
            m_NextFrameTracingButton.clickable.clicked += OnNextFrameTracingButton;

            m_LastFrameTracingButton = this.SafeQ<Button>("lastFrameTracingButton");
            m_LastFrameTracingButton.tooltip = "Go To Last Frame";
            m_LastFrameTracingButton.clickable.clicked += OnLastFrameTracingButton;

            m_CurrentFrameTextField = this.SafeQ<IntegerField>("currentFrameTextField");
            m_CurrentFrameTextField.AddToClassList("frameCounterLabel");
            m_CurrentFrameTextField.RegisterCallback<KeyDownEvent>(OnFrameCounterKeyDown);
            m_TotalFrameLabel = this.SafeQ<Label>("totalFrameLabel");
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
                var timeRect = new Rect(0, 0, m_GraphView.Window.rootVisualElement.layout.width - (m_PickTargetButton?.layout.xMax ?? 0), 18 /*imguiContainer.layout.height*/);
                m_TracingTimeline.OnGUI(timeRect);
            });

            m_TracingTimeline = new TracingTimeline(m_GraphView, m_CommandDispatcher.State);
            Add(imguiContainer);
        }

        public void SyncVisible()
        {
            var tracingDataModel = m_CommandDispatcher.State.TracingStatusState;
            if (style.display.value == DisplayStyle.Flex != tracingDataModel.TracingEnabled)
                style.display = tracingDataModel.TracingEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // PF FIXME use command
        void OnPickTargetButton(EventBase eventBase)
        {
            var state = m_CommandDispatcher.State;
            IDebugger debugger = ((Stencil)state.WindowState.GraphModel.Stencil).Debugger;
            var targetIndices = debugger.GetDebuggingTargets(state.WindowState.GraphModel);
            var items = targetIndices == null ? null : targetIndices.Select(x =>
                (SearcherItem)new TargetSearcherItem(x, debugger.GetTargetLabel(state.WindowState.GraphModel, x))).ToList();
            if (items == null || !items.Any())
                items = new List<SearcherItem> { new SearcherItem("<No Object found>") };

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, items, "Entities", i =>
            {
                if (i == null || !(i is TargetSearcherItem targetSearcherItem))
                    return true;

                using (var updater = state.TracingControlState.UpdateScope)
                {
                    updater.CurrentTracingTarget = targetSearcherItem.Target;
                    UpdateTracingMenu(updater);
                }

                return true;
            }, eventBase.originalMousePosition);
            eventBase.StopPropagation();
            eventBase.PreventDefault();
        }

        // PF FIXME use command
        void OnFrameCounterKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                var tracingDataModel = m_CommandDispatcher.State.TracingControlState;

                int frame = m_CurrentFrameTextField.value;

                frame = Math.Max(0, Math.Min(frame, Time.frameCount));

                using (var updater = tracingDataModel.UpdateScope)
                {
                    updater.CurrentTracingFrame = frame;
                    updater.CurrentTracingStep = -1;
                    UpdateTracingMenu(updater);
                }
            }
        }

        // PF FIXME should probably be an observer
        public void UpdateTracingMenu(TracingControlStateComponent.StateUpdater updater, bool force = true)
        {
            var state = m_CommandDispatcher.State;
            var tracingStatusState = state.TracingStatusState;
            var tracingControlState = state.TracingControlState;
            var tracingDataState = state.TracingDataState;

            if (EditorApplication.isPlaying && tracingStatusState.TracingEnabled)
            {
                m_PickTargetLabel.text = ((Stencil)state.WindowState.GraphModel?.Stencil)?.Debugger?.GetTargetLabel(state.WindowState.GraphModel, tracingControlState.CurrentTracingTarget);
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
                    updater.CurrentTracingFrame = Time.frameCount;
                    updater.CurrentTracingStep = -1;
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
                    m_CurrentFrameTextField.value = tracingControlState.CurrentTracingFrame;
                    m_TotalFrameLabel.text = $"/{Time.frameCount.ToString()}";
                    var currentTracingStep = tracingControlState.CurrentTracingStep;
                    if (currentTracingStep >= 0 && currentTracingStep < tracingDataState.MaxTracingStep)
                    {
                        m_TotalFrameLabel.text += $" [{currentTracingStep}/{tracingDataState.MaxTracingStep}]";
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

        // PF FIXME use command
        void OnFirstFrameTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.State.TracingControlState;
            using (var updater = tracingDataModel.UpdateScope)
            {
                updater.CurrentTracingFrame = 0;
                updater.CurrentTracingStep = -1;
                UpdateTracingMenu(updater);
            }
        }

        // PF FIXME use command
        void OnPreviousFrameTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.State.TracingControlState;

            if (tracingDataModel.CurrentTracingFrame > 0)
            {
                using (var updater = tracingDataModel.UpdateScope)
                {
                    updater.CurrentTracingFrame--;
                    updater.CurrentTracingStep = -1;
                    UpdateTracingMenu(updater);
                }
            }
        }

        // PF FIXME use command
        void OnPreviousStepTracingButton()
        {
            var tracingControlState = m_CommandDispatcher.State.TracingControlState;
            var tracingDataState = m_CommandDispatcher.State.TracingDataState;
            using (var updater = tracingControlState.UpdateScope)
            {
                if (tracingControlState.CurrentTracingStep > 0)
                {
                    updater.CurrentTracingStep--;
                }
                else
                {
                    if (tracingControlState.CurrentTracingStep == -1)
                    {
                        updater.CurrentTracingStep = tracingDataState.MaxTracingStep;
                    }
                    else
                    {
                        if (tracingControlState.CurrentTracingFrame > 0)
                        {
                            updater.CurrentTracingFrame--;
                            updater.CurrentTracingStep = tracingDataState.MaxTracingStep;
                        }
                    }
                }

                UpdateTracingMenu(updater);
            }
        }

        // PF FIXME use command
        void OnNextStepTracingButton()
        {
            var tracingControlState = m_CommandDispatcher.State.TracingControlState;
            var tracingDataState = m_CommandDispatcher.State.TracingDataState;

            using (var updater = tracingControlState.UpdateScope)
            {
                if (tracingControlState.CurrentTracingStep < tracingDataState.MaxTracingStep && tracingControlState.CurrentTracingStep >= 0)
                {
                    updater.CurrentTracingStep++;
                }
                else
                {
                    if (tracingControlState.CurrentTracingStep == -1 && (tracingControlState.CurrentTracingFrame < Time.frameCount))
                    {
                        updater.CurrentTracingStep = 0;
                    }
                    else
                    {
                        if (tracingControlState.CurrentTracingFrame < Time.frameCount)
                        {
                            updater.CurrentTracingFrame++;
                            updater.CurrentTracingStep = 0;
                        }
                    }
                }

                UpdateTracingMenu(updater);
            }
        }

        // PF FIXME use command
        void OnNextFrameTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.State.TracingControlState;
            if (tracingDataModel.CurrentTracingFrame < Time.frameCount)
            {
                using (var updater = tracingDataModel.UpdateScope)
                {
                    updater.CurrentTracingFrame++;
                    updater.CurrentTracingStep = -1;
                    UpdateTracingMenu(updater);
                }
            }
        }

        // PF FIXME use command
        void OnLastFrameTracingButton()
        {
            var tracingDataModel = m_CommandDispatcher.State.TracingControlState;

            using (var updater = tracingDataModel.UpdateScope)
            {
                updater.CurrentTracingFrame = Time.frameCount;
                updater.CurrentTracingStep = -1;
                UpdateTracingMenu(updater);
            }
        }
    }
}
