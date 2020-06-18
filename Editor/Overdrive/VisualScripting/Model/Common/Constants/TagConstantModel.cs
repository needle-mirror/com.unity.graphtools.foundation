using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class TagConstantModel : ConstantNodeModel<TagName>, IStringWrapperConstantModel
    {
        public string Label => "Tag";

        public List<string> GetAllInputNames(IEditorDataModel editorDataModel)
        {
            return InternalEditorUtility.tags.ToList();
        }

        public string StringValue
        {
            get => value.name;
            set => this.value.name = value;
        }
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public struct TagName
    {
        public string name;

        public override string ToString()
        {
            return name ?? "";
        }
    }
}
