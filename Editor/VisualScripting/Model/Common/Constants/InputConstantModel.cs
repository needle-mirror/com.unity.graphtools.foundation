using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class InputConstantModel : ConstantNodeModel<InputName>, IStringWrapperConstantModel
    {
        public List<string> GetAllInputNames()
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
    public struct InputName
    {
        public string name;

        public override string ToString()
        {
            return String.IsNullOrEmpty(name) ? "<No Input>" : name;
        }
    }
}
