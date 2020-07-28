using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class DragAndDropHelper
    {
        public static List<(IGTFVariableDeclarationModel, SerializableGUID, Vector2)> ExtractVariablesFromDroppedElements(
            IReadOnlyCollection<GraphElement> dropElements,
            GraphView graphView,
            Vector2 initialPosition)
        {
            var elementOffset = Vector2.zero;
            var variablesToCreate = new List<(IGTFVariableDeclarationModel, SerializableGUID, Vector2)>();

            foreach (var dropElement in dropElements)
            {
                if (dropElement is TokenDeclaration tokenDeclaration)
                {
                    Vector2 pos = dropElement.GetPosition().position;

                    if (!variablesToCreate.Any(x => ReferenceEquals(x.Item1, tokenDeclaration.Declaration)))
                        variablesToCreate.Add((tokenDeclaration.Declaration, GUID.Generate(), pos));
                    tokenDeclaration.RemoveFromHierarchy();
                }
                else if (dropElement is IVisualScriptingField visualScriptingField)
                {
                    Vector2 pos = graphView.contentViewContainer.WorldToLocal(initialPosition) + elementOffset;
                    elementOffset.y += ((GraphElement)visualScriptingField).layout.height + VseGraphView.DragDropSpacer;

                    if (!variablesToCreate.Any(x => ReferenceEquals(x.Item1, visualScriptingField.Model)))
                        variablesToCreate.Add((visualScriptingField.Model as IGTFVariableDeclarationModel, GUID.Generate(), pos));
                }
            }
            return variablesToCreate;
        }
    }
}
