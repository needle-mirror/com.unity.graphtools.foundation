using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public enum StickyNoteTheme
    {
        Classic,
        Black,
        Dark,
        Orange,
        Green,
        Blue,
        Red,
        Purple,
        Teal
    }

    public enum StickyNoteFontSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    public class StickyNote : GraphElement, IResizableGraphElement, IMovableGraphElement
    {
        public new class UxmlFactory : UxmlFactory<StickyNote> {}

        public IGTFStickyNoteModel StickyNoteModel => Model as IGTFStickyNoteModel;

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public new static readonly string k_UssClassName = "ge-sticky-note";
        static readonly string k_ThemeClassNamePrefix = k_UssClassName.WithUssModifier("theme-");
        static readonly string k_SizeClassNamePrefix = k_UssClassName.WithUssModifier("size-");

        public static readonly string k_SelectionBorderElementName = "selection-border";
        public static readonly string k_DisabledOverlayElementName = "disabled-overlay";
        public static readonly string k_TitleContainerPartName = "title-container";
        public static readonly string k_ContentContainerPartName = "text-container";
        public static readonly string k_ResizerPartName = "resizer";

        VisualElement m_ContentContainer;
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        public StickyNote()
        {
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            layer = -100;
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(k_TitleContainerPartName, Model, this, k_UssClassName, true));
            PartList.AppendPart(StickyNoteContentPart.Create(k_ContentContainerPartName, Model, this, k_UssClassName));
            PartList.AppendPart(FourWayResizerPart.Create(k_ResizerPartName, Model, this, k_UssClassName));
        }

        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = k_SelectionBorderElementName };
            selectionBorder.AddToClassList(k_UssClassName.WithUssElement(k_SelectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            var disabledOverlay = new VisualElement { name = k_DisabledOverlayElementName, pickingMode = PickingMode.Ignore };
            hierarchy.Add(disabledOverlay);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(k_UssClassName);
            this.AddStylesheet("StickyNote.uss");
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var newPos = StickyNoteModel.PositionAndSize;
            style.left = newPos.x;
            style.top = newPos.y;
            style.width = newPos.width;
            style.height = newPos.height;

            this.PrefixRemoveFromClassList(k_ThemeClassNamePrefix);
            AddToClassList(k_ThemeClassNamePrefix + StickyNoteModel.Theme.ToKebabCase());

            this.PrefixRemoveFromClassList(k_SizeClassNamePrefix);
            AddToClassList(k_SizeClassNamePrefix + StickyNoteModel.TextSize.ToKebabCase());
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) {}

        public static IEnumerable<string> GetThemes()
        {
            foreach (var s in Enum.GetNames(typeof(StickyNoteTheme)))
            {
                yield return s;
            }
        }

        public static IEnumerable<string> GetSizes()
        {
            foreach (var s in Enum.GetNames(typeof(StickyNoteFontSize)))
            {
                yield return s;
            }
        }

        void OnFitToText(DropdownMenuAction a)
        {
            FitText(false);
        }

        void FitText(bool onlyIfSmaller)
        {
            var titleField = this.Q(k_TitleContainerPartName).Q<Label>();
            var contentField = this.Q(k_ContentContainerPartName).Q<Label>();

            Vector2 preferredTitleSize = Vector2.zero;
            if (!string.IsNullOrEmpty(StickyNoteModel.DisplayTitle))
                preferredTitleSize = titleField.MeasureTextSize(StickyNoteModel.DisplayTitle, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined); // This is the size of the string with the current title font and such

            preferredTitleSize += AllExtraSpace(titleField);
            preferredTitleSize.x += titleField.ChangeCoordinatesTo(this, Vector2.zero).x + resolvedStyle.width - titleField.ChangeCoordinatesTo(this, new Vector2(titleField.layout.width, 0)).x;

            Vector2 preferredContentsSizeOneLine = contentField.MeasureTextSize(StickyNoteModel.Contents, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);

            Vector2 contentExtraSpace = AllExtraSpace(contentField);
            preferredContentsSizeOneLine += contentExtraSpace;

            Vector2 extraSpace = new Vector2(resolvedStyle.width, resolvedStyle.height) - contentField.ChangeCoordinatesTo(this, new Vector2(contentField.layout.width, contentField.layout.height));
            extraSpace += titleField.ChangeCoordinatesTo(this, Vector2.zero);
            preferredContentsSizeOneLine += extraSpace;

            float width = 0;
            float height = 0;
            // The content in one line is smaller than the current width.
            // Set the width to fit both title and content.
            // Set the height to have only one line in the content
            if (preferredContentsSizeOneLine.x < Mathf.Max(preferredTitleSize.x, resolvedStyle.width))
            {
                width = Mathf.Max(preferredContentsSizeOneLine.x, preferredTitleSize.x);
                height = preferredContentsSizeOneLine.y + preferredTitleSize.y;
            }
            else // The width is not enough for the content: keep the width or use the title width if bigger.
            {
                width = Mathf.Max(preferredTitleSize.x + extraSpace.x, resolvedStyle.width);
                float contextWidth = width - extraSpace.x - contentExtraSpace.x;
                Vector2 preferredContentsSize = contentField.MeasureTextSize(StickyNoteModel.Contents, contextWidth, MeasureMode.Exactly, 0, MeasureMode.Undefined);

                preferredContentsSize += contentExtraSpace;

                height = preferredTitleSize.y + preferredContentsSize.y + extraSpace.y;
            }

            ResizeFlags resizeWhat = ResizeFlags.None;
            if (!onlyIfSmaller || resolvedStyle.width < width)
            {
                resizeWhat |= ResizeFlags.Width;
                style.width = width;
            }

            if (!onlyIfSmaller || resolvedStyle.height < height)
            {
                resizeWhat |= ResizeFlags.Height;
                style.height = height;
            }

            if (this is IResizableGraphElement && resizeWhat != ResizeFlags.None)
            {
                Rect newRect = new Rect(0, 0, width, height);
                (this as IResizableGraphElement).OnResized(newRect, resizeWhat);
            }
        }

        public virtual void OnResized(Rect newRect, ResizeFlags resizeWhat)
        {
            if (resizeWhat != ResizeFlags.None)
            {
                Store.Dispatch(new ResizeStickyNoteAction(StickyNoteModel, newRect, resizeWhat));
            }
        }

        static Vector2 AllExtraSpace(VisualElement element)
        {
            return new Vector2(
                element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight + element.resolvedStyle.paddingLeft + element.resolvedStyle.paddingRight + element.resolvedStyle.borderRightWidth + element.resolvedStyle.borderLeftWidth,
                element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom + element.resolvedStyle.paddingTop + element.resolvedStyle.paddingBottom + element.resolvedStyle.borderBottomWidth + element.resolvedStyle.borderTopWidth
            );
        }

        public bool IsMovable => true;
    }
}
