using System;
using System.Collections.Generic;
using GraphModel = UnityEditor.GraphToolsFoundation.Overdrive.BasicModel.GraphModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphModel : GraphModel, IBlackboardGraphModel
    {
        public override Type DefaultStencilType => typeof(ClassStencil);
        public bool Valid => true;

        public string GetBlackboardTitle()
        {
            return "Blackboard";
        }

        public string GetBlackboardSubTitle()
        {
            return "";
        }

        public IEnumerable<string> SectionNames
        {
            get
            {
                yield return "Variables";
            }
        }

        public IEnumerable<IVariableDeclarationModel> GetSectionRows(string sectionName)
        {
            return VariableDeclarations;
        }

        public void PopulateCreateMenu(string sectionName, GenericMenu menu, Store store)
        {
            throw new NotImplementedException();
        }

        public IGraphModel GraphModel => this;
        public GUID Guid { get; set; }

        public void AssignNewGuid()
        {
            Guid = GUID.Generate();
        }

        public IReadOnlyList<Capabilities> Capabilities => new List<Capabilities>
        {
            Overdrive.Capabilities.NoCapabilities
        };
    }
}
