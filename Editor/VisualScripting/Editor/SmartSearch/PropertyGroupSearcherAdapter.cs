using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    class PropertyGroupSearcherAdapter : SimpleSearcherAdapter
    {
        readonly Store m_Store;
        readonly VisualTreeAsset m_ItemTemplate;

        readonly PropertyGroupBaseNodeModel m_PropertyGroupModel;

        public PropertyGroupSearcherAdapter(Store store, PropertyGroupBaseNodeModel model) : base("Get/Set properties")
        {
            m_Store = store;
            m_PropertyGroupModel = model;
            m_ItemTemplate =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    UICreationHelper.templatePath + "PropertyGroupSearcherItem.uxml");
        }

        public override VisualElement MakeItem()
        {
            // create a visual element hierarchy for this search result
            PropertyElement item = new PropertyElement(m_ItemTemplate.name);
            m_ItemTemplate.CloneTree(item);
            item.Toggle = item.MandatoryQ<Toggle>("selectionToggle");
            item.Toggle.RegisterValueChangedCallback(e => OnSelectionToggle(item));

            return item;
        }

        void OnSelectionToggle(PropertyElement element)
        {
            PropertySearcherItem boundSearchItem = element.Item;
            EditModel(element.Toggle.value, boundSearchItem);
        }

        public void EditModel(bool isToggled, SearcherItem boundSearchItem)
        {
            var propertySearcherItem = (PropertySearcherItem)boundSearchItem;

            var action = isToggled
                ? EditPropertyGroupNodeAction.EditType.Add
                : EditPropertyGroupNodeAction.EditType.Remove;
            propertySearcherItem.Enabled = isToggled;

            var memberReference = new TypeMember(propertySearcherItem.MemberInfo.UnderlyingType,
                BuildMemberPath(propertySearcherItem));

            m_Store.Dispatch(new EditPropertyGroupNodeAction(action, m_PropertyGroupModel, memberReference));
        }

        static List<string> BuildMemberPath(PropertySearcherItem propertySearcherItem)
        {
            List<string> memberPath;

            if (propertySearcherItem.Parent != null)
                memberPath = BuildMemberPath(propertySearcherItem.Parent as PropertySearcherItem);
            else
                memberPath = new List<string>();

            memberPath.Add(propertySearcherItem.Name);

            return memberPath;
        }

        public override VisualElement Bind(VisualElement element, SearcherItem item, ItemExpanderState expanderState, string query)
        {
            var propItem = (PropertySearcherItem)item;
            VisualElement expander = base.Bind(element, item, expanderState, query);
            PropertyElement propElt = (PropertyElement)element;
            propElt.Item = propItem;
            propItem.Element = propElt;
            Toggle toggle = propElt.Toggle;

            // SetValueWithoutNotify sets the value, but not the pseudostate
            toggle.SetValueWithoutNotify(propItem.Enabled);
            // .value= will set the pseudo state, but skip the notify and the markDirtyRepaint, as newValue == oldValue
            toggle.value = (propItem.Enabled);
            // still needed
            toggle.MarkDirtyRepaint();

            return expander;
        }

        internal static IEnumerable<SearcherItem> GetPropertySearcherItems(PropertyGroupBaseNodeModel model, int maxDepth)
        {
            var stencil = model.GraphModel.Stencil;
            TypeHandle instanceTypeRef = model.GetConnectedInstanceType();

            if (instanceTypeRef == TypeHandle.ThisType)
            {
                instanceTypeRef = stencil.GetThisType();
            }

            if (!instanceTypeRef.IsValid)
                return Enumerable.Empty<SearcherItem>();

            var existingMembers = new HashSet<int>(model.Members.Select(m => m.GetHashCode()));

            var propertySearcherItemsBuilder =
                new PropertySearcherItemsBuilder(maxDepth, instanceTypeRef, stencil.GraphContext.TypeMetadataResolver, existingMembers);

            return propertySearcherItemsBuilder.Build();
        }
    }
}
