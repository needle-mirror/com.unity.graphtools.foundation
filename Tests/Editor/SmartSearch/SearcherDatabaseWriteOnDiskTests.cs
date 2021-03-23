using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.SmartSearch
{
    class SearcherDatabaseWriteOnDiskTests : BaseFixture
    {
        protected override bool WriteOnDisk => true;
        protected override bool CreateGraphOnStartup => true;

        [TestCase(typeof(int), typeof(GraphNodeModelSearcherItem))]
        [TestCase(typeof(void), typeof(StackNodeModelSearcherItem))]
        public void TestGraphElement_GetMethodsInGraphAssets(Type returnType, Type itemType)
        {
            var graphMethod = GraphModel.CreateFunction("FunctionInOtherGraph", Vector2.zero);
            graphMethod.ReturnType = GraphModel.Stencil.GenerateTypeHandle(returnType);

            var db = new GraphElementSearcherDatabase(Stencil)
                .AddGraphsMethods()
                .Build();

            var result = db.Search("FunctionInOtherGraph", out _);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf(itemType, result[0]);

            var item = (ISearcherItemDataProvider)result[0];
            var data = (FunctionRefSearcherItemData)item.Data;
            Assert.AreEqual(data.FunctionModel, graphMethod);
            Assert.AreEqual(data.GraphModel, GraphModel);
            Assert.AreEqual(result[0].Parent.Name, GraphModel.AssetModel.Name);
            Assert.AreEqual(result[0].Parent.Parent.Name, "Graphs");
        }
    }
}
