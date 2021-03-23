using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a declaration (e.g. a variable) in a graph.
    /// </summary>
    [Serializable]
    public class DeclarationModel : GraphElementModel, IDeclarationModel, IRenamable
    {
        [FormerlySerializedAs("name")]
        [SerializeField]
        string m_Name;

        public string Title
        {
            get => m_Name;
            set => m_Name = value;
        }

        public virtual string DisplayTitle => Title.Nicify();

        public DeclarationModel()
        {
            InternalInitCapabilities();
        }

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            SetNameFromUserName(newName);
        }

        /// <inheritdoc />
        protected override void InitCapabilities()
        {
            InternalInitCapabilities();
        }

        void InternalInitCapabilities()
        {
            m_Capabilities = new List<Capabilities>
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Droppable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Renamable
            };
        }

        void SetNameFromUserName(string userName)
        {
            string newName = userName.ToUnityNameFormat();
            if (string.IsNullOrWhiteSpace(newName))
                return;

            Title = newName;
        }
    }
}
