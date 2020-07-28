using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class State : Overdrive.State
    {
        public new IEditorDataModel EditorDataModel => base.EditorDataModel as IEditorDataModel;

        public State(IGTFEditorDataModel editorDataModel) : base(editorDataModel)
        {
        }

        ~State()
        {
            Dispose(false);
        }
    }
}
