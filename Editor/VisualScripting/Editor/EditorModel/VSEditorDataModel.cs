using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    class VSEditorDataModel : IEditorDataModel
    {
        readonly VseWindow m_Win;
        static VSEditorPrefs s_EditorPrefs;

        public Action<RequestCompilationOptions> OnCompilationRequest;

        public bool TracingEnabled
        {
            get => m_Win.TracingEnabled;
            set => m_Win.TracingEnabled = value;
        }

        List<string> BlackboardExpandedRowStates => m_Win?.BlackboardExpandedRowStates;
        List<string> ElementModelsToSelectUponCreation => m_Win?.ElementModelsToSelectUponCreation;
        List<string> ElementModelsToExpandUponCreation => m_Win?.ElementModelsToExpandUponCreation;

        // IEditorDataModel
        public UpdateFlags UpdateFlags { get; private set; }
        public IGraphElementModel ElementModelToRename { get; set; }
        public GUID NodeToFrameGuid { get; set; } = default;
        public int CurrentGraphIndex => 0;
        public VSPreferences Preferences => s_EditorPrefs;

        public VSEditorDataModel(VseWindow win)
        {
            m_Win = win;
        }

        static VSEditorDataModel()
        {
            s_EditorPrefs = new VSEditorPrefs();
        }

        // We actually serialize this object in VseWindow, so going through this interface should as well
        public object BoundObject
        {
            get => m_Win.BoundObject;
            set => m_Win.SetBoundObject(value as GameObject);
        }

        public List<GraphModel> PreviousGraphModels => m_Win.PreviousGraphModels;

        public int UpdateCounter { get; set; }

        public IPluginRepository PluginRepository { get; set; }

        public void SetUpdateFlag(UpdateFlags flag)
        {
            UpdateFlags = flag;
        }

        public void RequestCompilation(RequestCompilationOptions options)
        {
            OnCompilationRequest?.Invoke(options);
        }

        public bool ShouldExpandBlackboardRowUponCreation(string rowName)
        {
            return BlackboardExpandedRowStates.Any(x => x == rowName);
        }

        public void ExpandBlackboardRowsUponCreation(IEnumerable<string> rowNames, bool expand)
        {
            if (expand)
            {
                foreach (var rowName in rowNames)
                {
                    if (!BlackboardExpandedRowStates.Any(x => x == rowName))
                        BlackboardExpandedRowStates.Add(rowName);
                }
            }
            else
            {
                foreach (var rowName in rowNames)
                {
                    var foundIndex = BlackboardExpandedRowStates.FindIndex(x => x == rowName);
                    if (foundIndex != -1)
                        BlackboardExpandedRowStates.RemoveAt(foundIndex);
                }
            }
        }

        public bool ShouldSelectElementUponCreation(IHasGraphElementModel hasGraphElementModel)
        {
            return ElementModelsToSelectUponCreation.Contains(hasGraphElementModel?.GraphElementModel?.GetId());
        }

        public void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select)
        {
            if (select)
            {
                ElementModelsToSelectUponCreation.AddRange(graphElementModels.Select(x => x.GetId()));
            }
            else
            {
                foreach (var graphElementModel in graphElementModels)
                    ElementModelsToSelectUponCreation.Remove(graphElementModel.GetId());
            }
        }

        public void ClearElementsToSelectUponCreation()
        {
            ElementModelsToSelectUponCreation.Clear();
        }

        public bool ShouldExpandElementUponCreation(IVisualScriptingField visualScriptingField)
        {
            return ElementModelsToExpandUponCreation?.Contains(visualScriptingField.GraphElementModel.GetId()) ?? false;
        }

        public void ExpandElementsUponCreation(IEnumerable<IVisualScriptingField> visualScriptingFields, bool expand)
        {
            if (expand)
                ElementModelsToExpandUponCreation.AddRange(visualScriptingFields
                    .Select(x => x.ExpandableGraphElementModel.GetId()));
            else
                foreach (var field in visualScriptingFields)
                    ElementModelsToExpandUponCreation.Remove(field.GraphElementModel.GetId());
        }
    }
}
