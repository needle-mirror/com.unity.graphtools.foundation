using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    public class InputConstant : Constant<InputName>, IStringWrapperConstantModel
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
            get => Value.name;
            set => Value.name = value;
        }

        public string Label => "Input";
    }
}
