using System;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    public class Node : CollapsibleInOutNode, IDroppable, ICustomSearcherHandler
    {
        public static readonly string k_TitleIconContainerPartName = "title-icon-container";

        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                var titleComponent = PartList.GetPart(k_TitleIconContainerPartName) as IconTitleProgressPart;
                if (titleComponent?.CoroutineProgressBar != null)
                {
                    titleComponent.CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.ReplacePart(k_TitleContainerPartName, IconTitleProgressPart.Create(k_TitleIconContainerPartName, Model, this, k_UssClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Node.uss"));
        }

        public Func<Node, Overdrive.Store, Vector2, SearcherFilter, bool> CustomSearcherHandler { get; set; }

        public bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null)
        {
            return CustomSearcherHandler == null || CustomSearcherHandler(this, Store, mousePosition, filter);
        }
    }
}
