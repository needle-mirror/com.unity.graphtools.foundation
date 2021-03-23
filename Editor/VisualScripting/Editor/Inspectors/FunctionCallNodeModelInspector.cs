using System;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    //    [CustomEditor(typeof(NodeAsset<FunctionCallNodeModel>))]
    class FunctionCallNodeModelInspector : NodeModelInspector
    {
        protected override bool DoDefaultInspector => false;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
            //            if (!(target is NodeAsset<FunctionCallNodeModel> asset))
            //                return;
            //            var decl = asset.Node;
            //            var index = 0;
            //            if (decl.TypeArguments != null)
            //            {
            //                var graph = asset.Node.GraphModel;
            //                if (graph != null)
            //                {
            //                    foreach (var typeArgument in decl.TypeArguments)
            //                    {
            //                        var closureIndex = index;
            //                        graph.Stencil.TypeEditor(typeArgument,
            //                            (theType, i) =>
            //                            {
            //                                decl.TypeArguments[closureIndex] = theType;
            //                                decl.OnConnection(null, null);
            //                                refreshUI();
            //                            });
            //                        index++;
            //                    }
            //                }
            //            }

            //            DisplayPorts(decl);
        }
    }
}
