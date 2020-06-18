#if DISABLE_SIMPLE_MATH_TESTS
using System;
using UnityEditor;
using Unity.GraphElements;
using UnityEngine;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleStickyNote : StickyNote
    {
        MathStickyNote m_Model;

        public MathStickyNote model
        {
            get => m_Model;
            set
            {
                if (m_Model == value)
                    return;

                m_Model = value;
                OnModelChange();
            }
        }

        public SimpleStickyNote() : base(Vector2.zero)
        {
            RegisterCallback<StickyNoteChangeEvent>(OnStickyNoteChange);
        }

        void OnStickyNoteChange(StickyNoteChangeEvent evt)
        {
            if (model == null)
                return;

            switch (evt.change)
            {
                case StickyNoteChange.Contents:
                    model.contents = contents;
                    break;
                case StickyNoteChange.Title:
                    model.title = title;
                    break;
                case StickyNoteChange.FontSize:
                    model.textSize = fontSize.ToString();
                    break;
                case StickyNoteChange.Theme:
                    model.theme = theme.ToString();
                    break;
                case StickyNoteChange.Position:
                    model.position = GetPosition();
                    break;
            }

            EditorUtility.SetDirty(model);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (model != null)
                model.position = newPos;
        }

        void OnModelChange()
        {
            if (model == null)
                return;

            title = model.title;
            contents = model.contents;

            if (!Enum.TryParse(model.theme, out StickyNoteTheme parsedTheme))
            {
                parsedTheme = StickyNoteTheme.Classic;
                model.theme = parsedTheme.ToString();
            }
            theme = parsedTheme;

            if (!Enum.TryParse(model.textSize, out StickyNoteFontSize parsedFontSize))
            {
                parsedFontSize = StickyNoteFontSize.Small;
                model.textSize = parsedFontSize.ToString();
            }
            fontSize = parsedFontSize;

            SetPosition(model.position);

            EditorUtility.SetDirty(model);
        }
    }
}
#endif
