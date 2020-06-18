using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class LayerConstantModel : ConstantNodeModel<LayerName>, IStringWrapperConstantModel
    {
        const int k_MaxLayers = 32;
        public string Label => "Layer";

        public List<string> GetAllInputNames(IEditorDataModel editorDataModel)
        {
            List<string> list = new List<string>(k_MaxLayers);
            for (int i = 0; i < k_MaxLayers; i++)
            {
                string s = LayerMask.LayerToName(i);
                if (!String.IsNullOrEmpty(s))
                    list.Add(s);
            }

            return list;
        }

        public string StringValue
        {
            get => value.name;
            set => this.value.name = value;
        }
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public struct LayerName
    {
        public string name;

        public override string ToString()
        {
            return name ?? "";
        }
    }
}
