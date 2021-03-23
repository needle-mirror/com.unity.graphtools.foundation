using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public static class BadgeContainerExtensions
    {
        const string k_HasErrorBadge = "hasErrorIconBadge";
        const string k_HasValueBadge = "hasValueIconBadge";

        public static void ShowErrorBadge(
            this IBadgeContainer self,
            VisualElement parent,
            SpriteAlignment alignment,
            string description,
            Store store,
            CompilerQuickFix errorQuickFix
        )
        {
            Assert.IsNotNull(parent);
            Assert.IsTrue(self is VisualElement);

            var target = (VisualElement)self;

            if (self.ErrorBadge?.parent == null)
            {
                var message = description;
                if (errorQuickFix != null)
                    message += $"\r\nQuick fix: {errorQuickFix.description}";

                self.ErrorBadge = IconBadge.CreateError(message);
                parent.Add(self.ErrorBadge);
                self.ErrorBadge.AttachTo(target, alignment);

                target.EnableInClassList(k_HasErrorBadge, true);

                if (errorQuickFix != null)
                    self.ErrorBadge.RegisterCallback<MouseDownEvent>(e => errorQuickFix.quickFix(store));

                return;
            }

            self.ErrorBadge.AttachTo(target, alignment);
        }

        public static void HideErrorBadge(this IBadgeContainer self)
        {
            Assert.IsTrue(self is VisualElement);

            ((VisualElement)self).RemoveFromClassList(k_HasErrorBadge);

            self.ErrorBadge?.RemoveFromHierarchy();
            self.ErrorBadge = null;
        }

        public static void ShowValueBadge(this IBadgeContainer self,
            VisualElement parent,
            VisualElement target,
            SpriteAlignment alignment,
            string description, Color badgeColor)
        {
            Assert.IsNotNull(parent);
            Assert.IsTrue(self is VisualElement);

            if (self.ValueBadge == null)
            {
                self.ValueBadge = new ValueBadge();
                self.ValueBadge.AddToClassList("valueBadge");
                parent.Add(self.ValueBadge);
                self.ValueBadge.AttachTo(target, alignment);
            }
            else
                self.ValueBadge.style.visibility = Visibility.Visible;

            self.ValueBadge.BadgeColor = badgeColor;
            self.ValueBadge.Text = description;

            target.EnableInClassList(k_HasValueBadge, true);
        }

        public static void HideValueBadge(this IBadgeContainer self)
        {
            Assert.IsTrue(self is VisualElement);

            ((VisualElement)self).RemoveFromClassList(k_HasValueBadge);
            if (self.ValueBadge != null)
                self.ValueBadge.style.visibility = Visibility.Hidden;
        }
    }
}
