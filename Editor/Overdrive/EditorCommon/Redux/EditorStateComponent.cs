using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    public abstract class EditorStateComponent
    {
    }

    [Serializable]
    public abstract class ViewStateComponent : EditorStateComponent
    {
        [SerializeField]
        SerializableGUID m_ViewGUID;

        public GUID ViewGUID
        {
            get => m_ViewGUID;
            set => m_ViewGUID = value;
        }
    }

    [Serializable]
    public abstract class AssetViewStateComponent : EditorStateComponent
    {
        [SerializeField]
        SerializableGUID m_ViewGUID;

        public GUID ViewGUID
        {
            get => m_ViewGUID;
            set => m_ViewGUID = value;
        }
    }
}
