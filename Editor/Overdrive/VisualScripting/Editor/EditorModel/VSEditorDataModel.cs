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
        static VSPreferences s_EditorPrefs;

        public Action<RequestCompilationOptions> OnCompilationRequest;

        public bool TracingEnabled
        {
            get => m_Win.TracingEnabled;
            set => m_Win.TracingEnabled = value;
        }

        public bool CompilationPending { get; set; }

        List<string> BlackboardExpandedRowStates => m_Win?.BlackboardExpandedRowStates;
        List<string> ElementModelsToSelectUponCreation => m_Win?.ElementModelsToSelectUponCreation;

        // IEditorDataModel
        public UpdateFlags UpdateFlags { get; private set; }
        List<IGTFGraphElementModel> m_ModelsToUpdate = new List<IGTFGraphElementModel>();
        public IEnumerable<IGTFGraphElementModel> ModelsToUpdate => m_ModelsToUpdate;
        public IGTFGraphElementModel ElementModelToRename { get; set; }
        public int CurrentGraphIndex => 0;
        public Preferences Preferences => s_EditorPrefs;

        public VSEditorDataModel(VseWindow win)
        {
            m_Win = win;
        }

        static VSEditorDataModel()
        {
            s_EditorPrefs = VSPreferences.CreatePreferences();
        }

        // We actually serialize this object in VseWindow, so going through this interface should as well
        public GameObject BoundObject
        {
            get => m_Win.BoundObject;
            set => m_Win.SetBoundObject(value);
        }

        public List<OpenedGraph> PreviousGraphModels => m_Win.PreviousGraphModels;

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

        public bool ShouldSelectElementUponCreation(IGTFGraphElementModel model)
        {
            return ElementModelsToSelectUponCreation.Contains(model.Guid.ToString());
        }

        public void SelectElementsUponCreation(IEnumerable<IGTFGraphElementModel> graphElementModels, bool select)
        {
            if (select)
            {
                ElementModelsToSelectUponCreation.AddRange(graphElementModels.Select(x => x.Guid.ToString()));
            }
            else
            {
                foreach (var graphElementModel in graphElementModels)
                    ElementModelsToSelectUponCreation.Remove(graphElementModel.Guid.ToString());
            }
        }

        public void ClearElementsToSelectUponCreation()
        {
            ElementModelsToSelectUponCreation.Clear();
        }
    }
}
