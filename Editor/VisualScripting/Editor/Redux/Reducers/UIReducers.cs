using System;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    static class UIReducers
    {
        public static void Register(Store store)
        {
            store.Register<RefreshUIAction>(RefreshUI);
            store.Register<OpenDocumentationAction>(OpenDocumentation);
        }

        static State RefreshUI(State previousState, RefreshUIAction action)
        {
            previousState.MarkForUpdate(action.UpdateFlags);
            if (action.ChangedModels != null)
                ((VSGraphModel)previousState.CurrentGraphModel).LastChanges.ChangedElements.AddRange(action.ChangedModels);
            return previousState;
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
