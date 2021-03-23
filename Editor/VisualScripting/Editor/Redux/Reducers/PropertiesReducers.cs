using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    static class PropertiesReducers
    {
        public static void Register(Store store)
        {
            store.Register<EditPropertyGroupNodeAction>(EditPropertyGroupNode);
        }

        static State EditPropertyGroupNode(State previousState, EditPropertyGroupNodeAction action)
        {
            var propertyGroupBase = action.nodeModel as PropertyGroupBaseNodeModel;
            if (propertyGroupBase == null)
                return previousState;

            Undo.RegisterCompleteObjectUndo(propertyGroupBase.SerializableAsset, "Remove Members");
            switch (action.editType)
            {
                case EditPropertyGroupNodeAction.EditType.Add:
                    propertyGroupBase.AddMember(action.member);
                    EditorUtility.SetDirty(propertyGroupBase.SerializableAsset);
                    break;

                case EditPropertyGroupNodeAction.EditType.Remove:
                    propertyGroupBase.RemoveMember(action.member);
                    EditorUtility.SetDirty(propertyGroupBase.SerializableAsset);
                    break;
            }

            return previousState;
        }
    }
}
