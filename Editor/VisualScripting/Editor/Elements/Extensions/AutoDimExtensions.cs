using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    namespace AutoDimOpacity
    {
        // AutoDimOpacity is an extension on VisualElement that adds automatic dimming of the element's opacity
        // on MouseEnter/Leave events.  Parameters are defined in AutoDimExtension.uss
        //
        // To use this feature on a VisualElement-based MyVisualElement class:
        //
        // this.AutoDimOpacity();

        [PublicAPI]
        public static class VisualElementExtensions
        {
            public enum StartingOpacity
            {
                Min,
                Max,
            }

            const float k_DefaultMinOpacity = 0.5f;
            const float k_DefaultMaxOpacity = 1.0f;
            const float k_DefaultStepOpacity = 0.05f;
            const int k_DefaultDelayMs = 850;

            class OpacityProperties
            {
                public Overflow OriginalOverflow;
                public float OriginalOpacity;
                public bool Augmenting;
                public readonly CustomStyleProperty<float> MinOpacity = new CustomStyleProperty<float>("--unity-min-opacity");
                public readonly CustomStyleProperty<float> MaxOpacity = new CustomStyleProperty<float>("--unity-max-opacity");
                public readonly CustomStyleProperty<float> Step = new CustomStyleProperty<float>("--unity-opacity-step");
                public readonly CustomStyleProperty<int> DelayMs = new CustomStyleProperty<int>("--unity-opacity-delay");
            }

            static readonly Dictionary<VisualElement, OpacityProperties> k_OpacityProperties =
                new Dictionary<VisualElement, OpacityProperties>();

            static readonly StyleSheet k_StyleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "AutoDimExtension.uss");

            public static void AutoDimOpacity(this VisualElement ele)
            {
                ele.RegisterCallback<AttachToPanelEvent>(e => ele.EnableAutoDimOpacity(StartingOpacity.Min));
                ele.RegisterCallback<DetachFromPanelEvent>(e => ele.DisableAutoDimOpacity());
            }

            public static void ToggleAutoDimOpacity(this VisualElement ele, StartingOpacity startingOpacity)
            {
                if (ele.IsAutoDimOpacityEnabled())
                {
                    ele.DisableAutoDimOpacity();
                }
                else
                {
                    ele.EnableAutoDimOpacity(startingOpacity);
                }
            }

            public static void EnableAutoDimOpacity(this VisualElement ele, StartingOpacity startingOpacity)
            {
                if (ele.IsAutoDimOpacityEnabled())
                    return;

                ele.styleSheets.Add(k_StyleSheet);
                ele.AddToClassList("autoDim");

                k_OpacityProperties[ele] = new OpacityProperties
                {
                    OriginalOpacity = 1f, // TODO: ele.style.opacity,
                    OriginalOverflow = ele.style.overflow.value
                };

                ele.style.overflow = Overflow.Visible;

                ele.SetAutoDimOpacity(startingOpacity);

                ele.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
                ele.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
                ele.RegisterCallback<DragExitedEvent>(OnDragExited);
                ele.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            }

            public static void DisableAutoDimOpacity(this VisualElement ele)
            {
                if (!ele.IsAutoDimOpacityEnabled())
                    return;

                ele.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
                ele.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
                ele.UnregisterCallback<DragExitedEvent>(OnDragExited);
                ele.UnregisterCallback<DragLeaveEvent>(OnDragLeave);

                ele.style.overflow = k_OpacityProperties[ele].OriginalOverflow;
                ele.SetOpacityRecursive(ele, k_OpacityProperties[ele].OriginalOpacity);
                k_OpacityProperties.Remove(ele);

                ele.RemoveFromClassList("autoDim");
                ele.styleSheets.Remove(k_StyleSheet);
            }

            public static void UpdateAutoDimOpacity(this VisualElement ele)
            {
                ele.SetOpacityRecursive(ele, ele.style.opacity.value);
            }

            static void SetAutoDimOpacity(this VisualElement ele, StartingOpacity startingOpacity)
            {
                switch (startingOpacity)
                {
                    case StartingOpacity.Min:
                        ele.style.opacity = ele.GetMinOpacity();
                        break;
                    case StartingOpacity.Max:
                        ele.style.opacity = ele.GetMaxOpacity();
                        break;
                }
                ele.UpdateAutoDimOpacity();
            }

            public static bool IsAutoDimOpacityEnabled(this VisualElement ele)
            {
                return ele.ClassListContains("autoDim");
            }

            static float GetMinOpacity(this VisualElement ele)
            {
                return ele.customStyle.TryGetValue(k_OpacityProperties[ele].MinOpacity, out var minOpacity) ? minOpacity : k_DefaultMinOpacity;
            }

            static float GetMaxOpacity(this VisualElement ele)
            {
                return ele.customStyle.TryGetValue(k_OpacityProperties[ele].MaxOpacity, out var maxOpacity) ? maxOpacity : k_DefaultMaxOpacity;
            }

            static float GetStepOpacity(this VisualElement ele)
            {
                return ele.customStyle.TryGetValue(k_OpacityProperties[ele].Step, out var step) ? step : k_DefaultStepOpacity;
            }

            static int GetOpacityDelayMs(this VisualElement ele)
            {
                return ele.customStyle.TryGetValue(k_OpacityProperties[ele].DelayMs, out var delayMs) ? delayMs : k_DefaultDelayMs;
            }

            static void OnMouseEnter(MouseEnterEvent evt)
            {
                var ele = (VisualElement)evt.target;
                var opacityProperties = k_OpacityProperties[ele];
                opacityProperties.Augmenting = true;
                k_OpacityProperties[ele] = opacityProperties;
                ele.schedule.Execute(ele.AugmentOpacity).StartingIn(0).Every(5)
                    .Until(() => !ele.ClassListContains("autoDim") || ele.style.opacity.value >= ele.GetMaxOpacity());
            }

            static void OnDragExited(DragExitedEvent evt)
            {
                MouseLeave(evt);
            }

            static void OnDragLeave(DragLeaveEvent evt)
            {
                MouseLeave(evt);
            }

            static void OnMouseLeave(MouseLeaveEvent evt)
            {
                MouseLeave(evt);
            }

            static void MouseLeave(EventBase evt)
            {
                var ele = (VisualElement)evt.target;
                var opacityProperties = k_OpacityProperties[ele];
                opacityProperties.Augmenting = false;
                k_OpacityProperties[ele] = opacityProperties;
                ele.schedule.Execute(ele.ReduceOpacity).StartingIn(ele.GetOpacityDelayMs()).Every(5)
                    .Until(() => !ele.ClassListContains("autoDim") || ele.style.opacity.value <= ele.GetMinOpacity());
            }

            static void AugmentOpacity(this VisualElement ele)
            {
                if (!ele.ClassListContains("autoDim") || !k_OpacityProperties[ele].Augmenting)
                    return;
                ele.SetOpacityRecursive(ele, Mathf.Min(ele.GetMaxOpacity(), ele.style.opacity.value + ele.GetStepOpacity()));
            }

            static void ReduceOpacity(this VisualElement ele)
            {
                if (!ele.ClassListContains("autoDim") || k_OpacityProperties[ele].Augmenting)
                    return;
                ele.SetOpacityRecursive(ele, Mathf.Max(ele.GetMinOpacity(), ele.style.opacity.value - ele.GetStepOpacity()));
            }

            static void SetOpacityRecursive(this VisualElement ele, VisualElement element, float opacity)
            {
                element.style.opacity = opacity;
                foreach (var child in element.hierarchy.Children())
                    ele.SetOpacityRecursive(child, opacity);
            }
        }
    }
}
