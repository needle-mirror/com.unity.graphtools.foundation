using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Actions
{
    [Category("Sticky Notes")]
    [Category("Action")]
    class StickyNoteActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        static readonly Rect k_StickyNoteRect = new Rect(Vector2.zero, new Vector2(100, 100));
        static readonly Rect k_StickyNote2Rect = new Rect(Vector2.right * 100, new Vector2(50, 50));

        [Test]
        public void Test_CreateStickyNoteAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(0));
                    return new CreateStickyNoteAction(k_StickyNoteRect);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.IsTrue(string.IsNullOrEmpty(GetStickyNote(0).Contents));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(k_StickyNoteRect));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    return new CreateStickyNoteAction(k_StickyNote2Rect);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(2));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(k_StickyNoteRect));
                    Assert.That(GetStickyNote(1).PositionAndSize, Is.EqualTo(k_StickyNote2Rect));
                });
        }

        [Test]
        public void Test_ResizeStickyNoteAction([Values] TestingMode mode)
        {
            var stickyNote = GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    stickyNote = GetStickyNote(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(k_StickyNoteRect));
                    return new ChangeStickyNoteLayoutAction(stickyNote, k_StickyNote2Rect, ResizeFlags.All);
                },
                () =>
                {
                    stickyNote = GetStickyNote(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(k_StickyNote2Rect));
                });
        }

        [Test]
        public void Test_UpdateStickyNoteAction([Values] TestingMode mode)
        {
            var stickyNote = GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(string.IsNullOrEmpty(GetStickyNote(0).Title));
                    Assert.IsTrue(string.IsNullOrEmpty(GetStickyNote(0).Contents));
                    return new UpdateStickyNoteAction(stickyNote, "stickyNote2", "This is a note");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Title, Is.EqualTo("stickyNote2"));
                    Assert.That(GetStickyNote(0).Contents, Is.EqualTo("This is a note"));
                });
        }

        [Test]
        public void Test_UpdateStickyNoteThemeAction([Values] TestingMode mode)
        {
            var stickyNote = GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Theme, Is.EqualTo(StickyNoteColorTheme.Classic.ToString()));
                    return new UpdateStickyNoteThemeAction(new[] { stickyNote }, StickyNoteColorTheme.Teal.ToString());
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Theme, Is.EqualTo(StickyNoteColorTheme.Teal.ToString()));
                });
        }

        [Test]
        public void Test_UpdateStickyNoteTextSizeAction([Values] TestingMode mode)
        {
            var stickyNote = GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).TextSize, Is.EqualTo(StickyNoteTextSize.Small.ToString()));
                    return new UpdateStickyNoteTextSizeAction(new[] { stickyNote }, StickyNoteTextSize.Huge.ToString());
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).TextSize, Is.EqualTo(StickyNoteTextSize.Huge.ToString()));
                });
        }
    }
}
