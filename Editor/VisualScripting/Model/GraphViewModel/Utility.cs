using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public static class Utility
    {
        // TODO rename/remove
        public static void SaveAssetIntoObject(IGraphElementModel asset, Object masterAsset)
        {
            EditorUtility.SetDirty(masterAsset);
        }

        public static void SaveAssetIntoObject(Object childAsset, Object masterAsset)
        {
            if (masterAsset == null || childAsset == null)
                return;

            EditorUtility.SetDirty(masterAsset);

            if (AssetDatabase.GetAssetPath(childAsset).Equals(AssetDatabase.GetAssetPath(masterAsset)))
                return;

            if (!EditorUtility.IsPersistent(masterAsset))
                return;

            if ((masterAsset.hideFlags & HideFlags.DontSave) != 0)
            {
                childAsset.hideFlags |= HideFlags.DontSave;
            }
            else
            {
                childAsset.hideFlags |= HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(childAsset, masterAsset);
            }
        }
    }
}
