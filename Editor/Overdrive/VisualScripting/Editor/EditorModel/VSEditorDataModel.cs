using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
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

        public bool CompilationPending { get; set; }

        List<string> BlackboardExpandedRowStates => m_Win?.BlackboardExpandedRowStates;
        List<string> ElementModelsToSelectUponCreation => m_Win?.ElementModelsToSelectUponCreation;
        List<string> ElementModelsToExpandUponCreation => m_Win?.ElementModelsToExpandUponCreation;

        // IEditorDataModel
        public UpdateFlags UpdateFlags { get; private set; }
        List<IGTFGraphElementModel> m_ModelsToUpdate = new List<IGTFGraphElementModel>();
        public IEnumerable<IGTFGraphElementModel> ModelsToUpdate => m_ModelsToUpdate;
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
        public GameObject BoundObject
        {
            get => m_Win.BoundObject;
            set => m_Win.SetBoundObject(value);
        }

        public List<OpenedGraph> PreviousGraphModels => m_Win.PreviousGraphModels;

        public int UpdateCounter { get; set; }

        public IPluginRepository PluginRepository { get; set; }

        public void SetUpdateFlag(UpdateFlags flag)
        {
            if (flag == UpdateFlags.None)
            {
                m_ModelsToUpdate.Clear();
            }
            UpdateFlags = flag;
        }

        public void AddModelToUpdate(IGTFGraphElementModel controller)
        {
            m_ModelsToUpdate.Add(controller);
        }

        public void ClearModelsToUpdate()
        {
            m_ModelsToUpdate.Clear();
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
