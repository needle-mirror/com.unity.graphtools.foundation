using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public struct TargetInsertionInfo
    {
        public Vector2 Delta;
        public string OperationName;
    }

    public class PasteSerializedDataAction : IAction
    {
        public readonly IGTFGraphModel Graph;
        public readonly TargetInsertionInfo Info;
        public readonly IGTFEditorDataModel EditorDataModel;
        public readonly VseGraphView.CopyPasteData Data;

        public PasteSerializedDataAction(IGTFGraphModel graph, TargetInsertionInfo info, IGTFEditorDataModel editorDataModel, VseGraphView.CopyPasteData data)
        {
            Graph = graph;
            Info = info;
            EditorDataModel = editorDataModel;
            Data = data;
        }
    }
}
