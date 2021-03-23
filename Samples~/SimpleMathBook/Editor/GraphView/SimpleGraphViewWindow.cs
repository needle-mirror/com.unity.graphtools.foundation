using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.UIElements;
using State = UnityEditor.GraphToolsFoundation.Overdrive.GraphToolState;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{
    internal class SimpleGraphViewWindow : GraphViewEditorWindow
    {
        [MenuItem("GTF Samples/MathBook Editor")]
        public static void ShowWindow()
        {
            GetWindow<SimpleGraphViewWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EditorToolName = "Math Book";
        }

        protected override GraphView CreateGraphView()
        {
            return new SimpleGraphView(this, true, CommandDispatcher);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new MathBookOnboardingProvider());

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(GraphAssetModel asset)
        {
            return asset is MathBookAsset;
        }
    }
}
