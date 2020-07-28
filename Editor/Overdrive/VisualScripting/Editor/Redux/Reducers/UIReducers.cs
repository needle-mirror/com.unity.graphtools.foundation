using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class UIReducers
    {
        public static void Register(Store store)
        {
            store.RegisterReducer<State, OpenDocumentationAction>(OpenDocumentation);
        }

        static State OpenDocumentation(State previousState, OpenDocumentationAction action)
        {
            foreach (var nodeModel in action.NodeModels)
            {
                // TODO: Get the right path for the documentation
                Help.BrowseURL("https://docs.unity3d.com/Manual/30_search.html?q=" + nodeModel.GetType().Name);
                break;
            }

            previousState.MarkForUpdate(UpdateFlags.None);
            return previousState;
        }
    }
}
