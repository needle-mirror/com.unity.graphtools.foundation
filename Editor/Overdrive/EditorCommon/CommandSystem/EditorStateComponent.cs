using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds a part of a <see cref="PersistedEditorState"/>.
    /// </summary>
    [Serializable]
    // Part of the st olds the
    public abstract class EditorStateComponent
    {
    }

    /// <summary>
    /// Holds the view part of a <see cref="PersistedEditorState"/>.
    /// </summary>
    [Serializable]
    public abstract class ViewStateComponent : EditorStateComponent
    {
        [SerializeField]
        SerializableGUID m_ViewGUID;

        /// <summary>
        /// The unique ID of the referenced view.
        /// </summary>
        public SerializableGUID ViewGUID
        {
            get => m_ViewGUID;
            set => m_ViewGUID = value;
        }
    }

    /// <summary>
    /// Holds the asset view part of a <see cref="PersistedEditorState"/>.
    /// </summary>
    [Serializable]
    public abstract class AssetViewStateComponent : EditorStateComponent
    {
        [SerializeField]
        SerializableGUID m_ViewGUID;

        /// <summary>
        /// The unique ID of the referenced view.
        /// </summary>
        public SerializableGUID ViewGUID
        {
            get => m_ViewGUID;
            set => m_ViewGUID = value;
        }
    }
}
