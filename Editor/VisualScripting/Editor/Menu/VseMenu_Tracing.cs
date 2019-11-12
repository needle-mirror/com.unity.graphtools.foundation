using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Searcher;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    partial class VseMenu
    {
        public Action<ChangeEvent<bool>> OnToggleTracing;

        const int k_UpdateIntervalMs = 500;

        ToolbarToggle m_EnableTracingButton;
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

        void CreateTracingMenu()
        {
            m_EnableTracingButton = this.MandatoryQ<ToolbarToggle>("enableTracingButton");
            m_EnableTracingButton.tooltip = "Toggle Tracing For Current Instance";
            m_EnableTracingButton.SetValueWithoutNotify(m_Store.GetState().EditorDataModel.TracingEnabled);
            m_EnableTracingButton.RegisterValueChangedCallback(e => OnToggleTracing?.Invoke(e));

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
        }

        class EntitySearcherItem : SearcherItem
        {
            public readonly DebuggerTracer.FrameData.EntityDescriptor Entity;
            public EntitySearcherItem(DebuggerTracer.FrameData.EntityDescriptor e)
                : base($"{e.EntityReference.EntityIndex}: {e.EntityName}")
            {
                Entity = e;
            }
        }

        void OnPickTargetButton(EventBase eventBase)
        {
            var items = DebuggerTracer.GetTargets(m_Store.GetState().currentTracingFrame, (m_Store.GetState().CurrentGraphModel?.AssetModel as Object)?.GetInstanceID() ?? -1)
                .Select(x => (SearcherItem) new EntitySearcherItem(x)).ToList();
            SearcherWindow.Show(EditorWindow.focusedWindow, items, "Entities", i =>
            {
                if (i == null)
                    return true;
                var t = ((EntitySearcherItem)i).Entity;
                m_Store.GetState().currentTracingTarget = t.EntityReference.EntityIndex;
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
                m_Store.GetState().currentTracingFrame = frame;
                m_Store.GetState().currentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void UpdateTracingMenu(bool force = true)
        {
            var state = m_Store.GetState();

            if (EditorApplication.isPlaying && state.EditorDataModel.TracingEnabled && DebuggerTracer.AllGraphs != null)
            {
                m_PickTargetLabel.text = state.currentTracingTarget.ToString();
                m_PickTargetIcon.visible = false;
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
                    state.currentTracingFrame = Time.frameCount;
                    state.currentTracingStep = -1;
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
                    m_CurrentFrameTextField.value = state.currentTracingFrame;
                    m_TotalFrameLabel.text = $"/{Time.frameCount.ToString()}";
                    if (state.currentTracingStep != -1)
                    {
                        m_TotalFrameLabel.text += $" [{state.currentTracingStep}/{state.maxTracingStep}]";
                    }

                    m_LastUpdate.Restart();
                }
            }
            else
            {
                m_LastUpdate.Stop();
                state.currentTracingFrame = Time.frameCount;
                state.currentTracingStep = -1;
                m_PickTargetLabel.text = "";
                m_PickTargetIcon.visible = true;
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
            m_Store.GetState().currentTracingFrame = 0;
            m_Store.GetState().currentTracingStep = -1;
            UpdateTracingMenu();
        }

        void OnPreviousFrameTracingButton()
        {
            if (m_Store.GetState().currentTracingFrame > 0)
            {
                m_Store.GetState().currentTracingFrame--;
                m_Store.GetState().currentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnPreviousStepTracingButton()
        {
            if (m_Store.GetState().currentTracingStep > 0)
            {
                m_Store.GetState().currentTracingStep--;
            }
            else
            {
                if (m_Store.GetState().currentTracingStep == -1)
                {
                    m_Store.GetState().currentTracingStep = m_Store.GetState().maxTracingStep;
                }
                else
                {
                    if (m_Store.GetState().currentTracingFrame > 0)
                    {
                        m_Store.GetState().currentTracingFrame--;
                        m_Store.GetState().currentTracingStep = m_Store.GetState().maxTracingStep;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextStepTracingButton()
        {
            if (m_Store.GetState().currentTracingStep < m_Store.GetState().maxTracingStep && m_Store.GetState().currentTracingStep >= 0)
            {
                m_Store.GetState().currentTracingStep++;
            }
            else
            {
                if (m_Store.GetState().currentTracingStep == -1 && (m_Store.GetState().currentTracingFrame < Time.frameCount))
                {
                    m_Store.GetState().currentTracingStep = 0;
                }
                else
                {
                    if (m_Store.GetState().currentTracingFrame < Time.frameCount)
                    {
                        m_Store.GetState().currentTracingFrame++;
                        m_Store.GetState().currentTracingStep = 0;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextFrameTracingButton()
        {
            if (m_Store.GetState().currentTracingFrame < Time.frameCount)
            {
                m_Store.GetState().currentTracingFrame++;
                m_Store.GetState().currentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnLastFrameTracingButton()
        {
            m_Store.GetState().currentTracingFrame = Time.frameCount;
            m_Store.GetState().currentTracingStep = -1;
            UpdateTracingMenu();
        }
    }
}
