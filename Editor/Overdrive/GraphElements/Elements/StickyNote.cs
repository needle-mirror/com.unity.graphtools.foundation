using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
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

    public class StickyNote : GraphElement, IResizableGraphElement
    {
        public new class UxmlFactory : UxmlFactory<StickyNote> {}

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public new static readonly string ussClassName = "ge-sticky-note";
        static readonly string themeClassNamePrefix = ussClassName.WithUssModifier("theme-");
        static readonly string sizeClassNamePrefix = ussClassName.WithUssModifier("size-");

        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string disabledOverlayElementName = "disabled-overlay";
        public static readonly string titleContainerPartName = "title-container";
        public static readonly string contentContainerPartName = "text-container";
        public static readonly string resizerPartName = "resizer";

        VisualElement m_ContentContainer;

        public override VisualElement contentContainer => m_ContentContainer ?? this;

        public IStickyNoteModel StickyNoteModel => Model as IStickyNoteModel;

        public StickyNote()
        {
            Layer = -100;
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName, true));
            PartList.AppendPart(StickyNoteContentPart.Create(contentContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            var disabledOverlay = new VisualElement { name = disabledOverlayElementName, pickingMode = PickingMode.Ignore };
            hierarchy.Add(disabledOverlay);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
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

            this.PrefixRemoveFromClassList(themeClassNamePrefix);
            AddToClassList(themeClassNamePrefix + StickyNoteModel.Theme.ToKebabCase());

            this.PrefixRemoveFromClassList(sizeClassNamePrefix);
            AddToClassList(sizeClassNamePrefix + StickyNoteModel.TextSize.ToKebabCase());
        }

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
            var titleField = this.Q(titleContainerPartName).Q<Label>();
            var contentField = this.Q(contentContainerPartName).Q<Label>();

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
                Store.Dispatch(new ChangeStickyNoteLayoutAction(StickyNoteModel, newRect, resizeWhat));
            }
        }

        static Vector2 AllExtraSpace(VisualElement element)
        {
            return new Vector2(
                element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight + element.resolvedStyle.paddingLeft + element.resolvedStyle.paddingRight + element.resolvedStyle.borderRightWidth + element.resolvedStyle.borderLeftWidth,
                element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom + element.resolvedStyle.paddingTop + element.resolvedStyle.paddingBottom + element.resolvedStyle.borderBottomWidth + element.resolvedStyle.borderTopWidth
            );
        }
    }
}
