using System;
using UnityEditor.EditorCommon;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public partial class VseWindow
    {
        [SerializeField]
        LockTracker m_LockTracker = new LockTracker();

        bool Locked
        {
            get => m_Store?.GetState().AssetModel != null && m_LockTracker.IsLocked;
            set => m_LockTracker.IsLocked = value;
        }

        void OnLockStateChanged(bool locked)
        {
            // Make sure that upon unlocking, any selection change is updated
            if (!locked)
                OnGlobalSelectionChange();
        }
    }
}
