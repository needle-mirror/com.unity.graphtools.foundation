using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public static class DragAndDropHelper
    {
        public static List<Tuple<IVariableDeclarationModel, Vector2>> ExtractVariablesFromDroppedElements(
            IReadOnlyCollection<GraphElement> dropElements,
            VseGraphView graphView,
            Vector2 initialPosition)
        {
            var elementOffset = Vector2.zero;
            var variablesToCreate = new List<Tuple<IVariableDeclarationModel, Vector2>>();

            foreach (var dropElement in dropElements)
            {
                if (dropElement is TokenDeclaration tokenDeclaration)
                {
                    Vector2 pos = dropElement.GetPosition().position;

                    if (!variablesToCreate.Any(x => ReferenceEquals(x.Item1, tokenDeclaration.Declaration)))
                        variablesToCreate.Add(new Tuple<IVariableDeclarationModel, Vector2>(tokenDeclaration.Declaration, pos));
                    tokenDeclaration.RemoveFromHierarchy();
                }
                else if (dropElement is IVisualScriptingField visualScriptingField)
                {
                    Vector2 pos = graphView.contentViewContainer.WorldToLocal(initialPosition) + elementOffset;
                    elementOffset.y += ((GraphElement)visualScriptingField).layout.height + VseGraphView.DragDropSpacer;

                    if (!variablesToCreate.Any(x => ReferenceEquals(x.Item1, visualScriptingField.GraphElementModel)))
                        variablesToCreate.Add(new Tuple<IVariableDeclarationModel, Vector2>(visualScriptingField.GraphElementModel as IVariableDeclarationModel, pos));
                }
            }
            return variablesToCreate;
        }
    }
}
