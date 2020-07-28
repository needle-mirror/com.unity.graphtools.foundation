using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class StoreHelper
    {
        public static void RegisterReducers(Store store)
        {
            GraphElements.StoreHelper.RegisterReducers(store);

            UIReducers.Register(store);
            GraphAssetReducers.Register(store);
            GraphReducers.Register(store);
            NodeReducers.Register(store);
            PlacematReducers.Register(store);
            PortalReducers.Register(store);
            EdgeReducers.Register(store);
            VariableReducers.Register(store);
            StickyNoteReducers.Register(store);
        }
    }
}
