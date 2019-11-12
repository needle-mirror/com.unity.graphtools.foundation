using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Translators;

namespace UnityEditor.VisualScriptingTests
{
    [Serializable]
    class Type0FakeNodeModel : NodeModel, IFakeNode
    {
        public IPortModel Input0 { get; private set; }
        public IPortModel Input1 { get; private set; }
        public IPortModel Input2 { get; private set; }
        public IPortModel Output0 { get; private set; }
        public IPortModel Output1 { get; private set; }
        public IPortModel Output2 { get; private set; }

        protected override void OnDefineNode()
        {
            Input0 = AddDataInput<int>("input0");
            Input1 = AddDataInput<int>("input1");
            Input2 = AddDataInput<int>("input2");
            Output0 = AddDataOutputPort<int>("output0");
            Output1 = AddDataOutputPort<int>("output1");
            Output2 = AddDataOutputPort<int>("output2");
        }
    }

    interface IFakeNode : INodeModel {}

    [GraphtoolsExtensionMethods]
    static class Type0FakeNodeModelExt
    {
        public static IEnumerable<SyntaxNode> BuildGetComponent(this RoslynTranslator translator, IFakeNode model, IPortModel portModel)
        {
            yield return SyntaxFactory.EmptyStatement().WithTrailingTrivia(SyntaxFactory.Comment($"/* {((NodeModel)model).Title} */"));
        }
    }
}
