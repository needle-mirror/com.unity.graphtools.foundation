using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using ICloneable = UnityEditor.GraphToolsFoundation.Overdrive.Model.ICloneable;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public abstract class EdgePortalModel : NodeModel, IGTFEdgePortalModel, IRenamable, ICloneable
    {
        [SerializeField]
        int m_EvaluationOrder;

        [SerializeReference]
        IDeclarationModel m_DeclarationModel;

        public IDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = value;
        }

        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        public int EvaluationOrder
        {
            get => m_EvaluationOrder;
            protected set => m_EvaluationOrder = value;
        }

        public bool IsRenamable => true;

        public void Rename(string newName)
        {
            (DeclarationModel as IRenamable)?.Rename(newName);
        }

        public IGTFGraphElementModel Clone()
        {
            var decl = m_DeclarationModel;
            try
            {
                m_DeclarationModel = null;
                var clone = ICloneableExtensions.CloneUsingScriptableObjectInstantiate(this);
                clone.m_DeclarationModel = decl;
                return clone;
            }
            finally
            {
                m_DeclarationModel = decl;
            }
        }

        public virtual bool CanCreateOppositePortal()
        {
            return true;
        }
    }
}
