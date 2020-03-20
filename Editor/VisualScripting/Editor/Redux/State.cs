using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Editor.Plugins;
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

        public IEditorDataModel EditorDataModel { get; private set; }
        public ICompilationResultModel CompilationResultModel { get; private set; }

        /// <summary>
        /// Stores the list of steps for the current graph, frame and target tuple
        /// </summary>
        public List<TracingStep> DebuggingData { get; set; }

        public int CurrentTracingTarget = -1;
        public int CurrentTracingFrame;
        public int CurrentTracingStep;
        public int MaxTracingStep;

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
            CurrentTracingStep = -1;
        }

        public void Dispose()
        {
            UnloadCurrentGraphAsset();
            CompilationResultModel = null;
            DebuggingData = null;
            EditorDataModel = null;
        }

        public void UnloadCurrentGraphAsset()
        {
            AssetModel?.Dispose();
            AssetModel = null;
            if (EditorDataModel != null)
            {
                //TODO: should not be needed ?
                EditorDataModel.PluginRepository?.UnregisterPlugins();
                EditorDataModel.BoundObject = null;
            }
        }

        public void RegisterReducers(Store store, Action clearRegistrations)
        {
            clearRegistrations();
            store.RegisterReducers();
            CurrentGraphModel?.Stencil?.RegisterReducers(store);
        }
    }
}
