using System;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateGraphAssetAction : IAction
    {
        public readonly Type StencilType;
        public readonly Type GraphType;
        public readonly Type AssetType;
        public readonly string Name;
        public readonly string AssetPath;
        public readonly GameObject Instance;
        public readonly bool WriteOnDisk;
        public readonly IGraphTemplate GraphTemplate;

        public CreateGraphAssetAction(Type stencilType, string name = "", string assetPath = "", GameObject instance = null, bool writeOnDisk = true, IGraphTemplate graphTemplate = null)
            : this(stencilType, typeof(VSGraphModel), typeof(VSGraphAssetModel), name, assetPath, instance, writeOnDisk, graphTemplate)
        {
        }

        public CreateGraphAssetAction(Type stencilType, Type graphType, Type assetType, string name = "", string assetPath = "", GameObject instance = null, bool writeOnDisk = true, IGraphTemplate graphTemplate = null)
        {
            StencilType = stencilType;
            GraphType = graphType;
            AssetType = assetType;
            Name = name;
            AssetPath = assetPath;
            Instance = instance;
            WriteOnDisk = writeOnDisk;
            GraphTemplate = graphTemplate;
        }
    }

    public class LoadGraphAssetAction : IAction
    {
        public enum Type
        {
            Replace,
            PushOnStack,
            KeepHistory
        }

        public readonly string AssetPath;
        public readonly Object BoundObject;
        public readonly Type LoadType;

        public readonly bool AlignAfterLoad;

        public LoadGraphAssetAction(string assetPath, Object boundObject = null, bool alignAfterLoad = false,
                                    Type loadType = Type.Replace)
        {
            AssetPath = assetPath;
            BoundObject = boundObject;
            LoadType = loadType;
            AlignAfterLoad = alignAfterLoad;
        }
    }

    public class UnloadGraphAssetAction : IAction {}
}
