using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class State : IDisposable
    {
        public IGraphAssetModel AssetModel { get; set; }
        public IGraphModel CurrentGraphModel => AssetModel?.GraphModel;

        public VSPreferences Preferences => EditorDataModel?.Preferences;

        public IEditorDataModel EditorDataModel { get; }
        public ICompilationResultModel CompilationResultModel { get; private set; }

        public struct DebuggingDataModel
        {
            public INodeModel nodeModel;
            public byte progress;
            public DebuggerTracer.EntityFrameTrace.StepType type;
            public string text;
            public Dictionary<INodeModel, string> values;
        }
        public List<DebuggingDataModel> DebuggingData { get; set; }

        public int currentTracingTarget;
        public int currentTracingFrame;
        public int currentTracingStep;
        public int maxTracingStep;
        public bool requestNodeAlignment;

        public enum UIRebuildType                             // for performance debugging purposes
        {
            None, Partial, Full
        }
        public string LastDispatchedActionName { get; set; }    // ---
        public UIRebuildType lastActionUIRebuildType;           // ---

        public State(IEditorDataModel editorDataModel)
        {
            CompilationResultModel = new CompilationResultModel();
            EditorDataModel = editorDataModel;
            currentTracingStep = -1;
        }

        public void Dispose()
        {
            UnloadCurrentGraphAsset();
            CompilationResultModel = null;
            DebuggingData = null;
        }

        public void UnloadCurrentGraphAsset()
        {
            AssetModel?.Dispose();
            AssetModel = null;
            //TODO: should not be needed ?
            EditorDataModel?.PluginRepository?.UnregisterPlugins();
        }

        public void RegisterReducers(Store store, Action clearRegistrations)
        {
            clearRegistrations();
            store.RegisterReducers();
            if (CurrentGraphModel?.Stencil != null)
                CurrentGraphModel?.Stencil.RegisterReducers(store);
        }
    }
}
