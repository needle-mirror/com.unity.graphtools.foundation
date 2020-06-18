using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class InputConstantModel : ConstantNodeModel<InputName>, IStringWrapperConstantModel
    {
        public List<string> GetAllInputNames(IEditorDataModel editorDataModel)
        {
            List<string> list = new List<string>();
            Object inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];

            SerializedObject obj = new SerializedObject(inputManager);

            SerializedProperty axisArray = obj.FindProperty("m_Axes");

            if (axisArray.arraySize == 0)
                Debug.Log("No Axes");

            for (int i = 0; i < axisArray.arraySize; ++i)
            {
                SerializedProperty axis = axisArray.GetArrayElementAtIndex(i);

                string newName = axis.FindPropertyRelative("m_Name").stringValue;

                if (!list.Contains(newName))
                    list.Add(newName);
            }

            return list;
        }

        public string StringValue
        {
            get => value.name;
            set => this.value.name = value;
        }

        public string Label => "Input";
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public struct InputName
    {
        public string name;

        public override string ToString()
        {
            return String.IsNullOrEmpty(name) ? "<No Input>" : name;
        }
    }
}
