using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public abstract class EdgePortalModel : NodeModel, IEdgePortalModel, IRenamable, ICloneable
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

        public EdgePortalModel()
        {
            InternalInitCapabilities();
        }

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            (DeclarationModel as IRenamable)?.Rename(newName);
        }

        public IGraphElementModel Clone()
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

        protected override void InitCapabilities()
        {
            base.InitCapabilities();
            InternalInitCapabilities();
        }

        void InternalInitCapabilities()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, true);
        }
    }
}
