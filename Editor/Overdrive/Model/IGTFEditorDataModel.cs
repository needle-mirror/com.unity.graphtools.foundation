using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFEditorDataModel
    {
        UpdateFlags UpdateFlags { get; }
        void SetUpdateFlag(UpdateFlags flag);
        IEnumerable<IGTFGraphElementModel> ModelsToUpdate { get; }
        void AddModelToUpdate(IGTFGraphElementModel controller);
        void ClearModelsToUpdate();
    }
}
