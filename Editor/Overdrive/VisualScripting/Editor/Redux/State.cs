using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class State : Overdrive.State
    {
        public new IGraphModel CurrentGraphModel => base.CurrentGraphModel as IGraphModel;
        public new IEditorDataModel EditorDataModel => base.EditorDataModel as IEditorDataModel;

        public State(IGTFEditorDataModel editorDataModel) : base(editorDataModel)
        {
        }

        ~State()
        {
            Dispose(false);
        }

        public void RegisterReducers(Store store, Action clearRegistrations)
        {
            clearRegistrations();
            store.RegisterReducers();
            CurrentGraphModel?.Stencil?.RegisterReducers(store);
        }
    }
}
