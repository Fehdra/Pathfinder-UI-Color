// DarkParchmentUI/UMMMenu.cs
// C# 7.3 compatible

using UnityEngine;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    internal static class UMMMenu
    {
        // track last values so we only apply visibility once when toggles change
        private static bool _lastHideLeftAndActionBar;
        private static bool _lastHidePortraitAndRightButtons;
        private static bool _lastHideDialoguePanel;

        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            var s = Main.Settings;
            if (s == null) return;

            GUILayout.Label("DarkParchmentUI - Global UI Tint", GUILayout.Height(20));

            // ===================== Background Tint =====================

            s.Enabled = GUILayout.Toggle(s.Enabled, "Enable background tint");

            GUILayout.Space(6f);
            GUILayout.Label($"Strength: {s.Strength:0.00}");
            s.Strength = GUILayout.HorizontalSlider(s.Strength, 0f, 1f);

            GUILayout.Space(6f);
            GUILayout.Label("Tint Color (warm parchment):");
            s.TintR = Slider01("R", s.TintR);
            s.TintG = Slider01("G", s.TintG);
            s.TintB = Slider01("B", s.TintB);

            GUILayout.Space(6f);
            GUILayout.Label($"Alpha multiply: {s.AlphaMul:0.00} (usually 1.00)");
            s.AlphaMul = GUILayout.HorizontalSlider(s.AlphaMul, 0f, 1f);

            GUILayout.Space(10f);
            s.ExcludeChatLike = GUILayout.Toggle(
                s.ExcludeChatLike,
                "Exclude chat/log panels (recommended)"
            );

            GUILayout.Space(4f);
            GUILayout.Label("Extra exclude tokens (comma separated):");
            s.ExtraExcludeTokens = GUILayout.TextField(s.ExtraExcludeTokens ?? "");

            GUILayout.Space(10f);
            s.SkipSmallImages = GUILayout.Toggle(
                s.SkipSmallImages,
                "Skip small images (icons) to reduce muddy UI"
            );

            if (s.SkipSmallImages)
            {
                GUILayout.Label($"Small image max size: {s.SmallImageMaxSize:0}");
                s.SmallImageMaxSize = GUILayout.HorizontalSlider(s.SmallImageMaxSize, 10f, 120f);
            }

            GUILayout.Space(10f);
            GUILayout.Label("Chat / Info Window Tint", UnityModManager.UI.h2);

            s.EnableChatTint = GUILayout.Toggle(s.EnableChatTint, "Enable chat/info tint");

            if (s.EnableChatTint)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"Chat Strength: {s.ChatStrength:0.00}");
                s.ChatStrength = GUILayout.HorizontalSlider(s.ChatStrength, 0f, 1f);

                GUILayout.Space(6f);
                GUILayout.Label("Chat Tint Color:");
                s.ChatTintR = Slider01("R", s.ChatTintR);
                s.ChatTintG = Slider01("G", s.ChatTintG);
                s.ChatTintB = Slider01("B", s.ChatTintB);
            }

            // ===================== HUD HIDING =====================

            GUILayout.Space(10f);
            GUILayout.Label("HUD Hiding", UnityModManager.UI.h2);

            s.HideLeftAndActionBar = GUILayout.Toggle(
                s.HideLeftAndActionBar,
                "Hide left cast bars + action bar (1 + 2)"
            );

            s.HidePortraitAndRightButtons = GUILayout.Toggle(
                s.HidePortraitAndRightButtons,
                "Hide portrait + right HUD buttons (3 + 4)"
            );

            s.HideDialoguePanel = GUILayout.Toggle(
                s.HideDialoguePanel,
                "Hide dialogue / chat panel (5)"
            );

            // if any changed -> apply now
            if (_lastHideLeftAndActionBar != s.HideLeftAndActionBar ||
                _lastHidePortraitAndRightButtons != s.HidePortraitAndRightButtons ||
                _lastHideDialoguePanel != s.HideDialoguePanel)
            {
                _lastHideLeftAndActionBar = s.HideLeftAndActionBar;
                _lastHidePortraitAndRightButtons = s.HidePortraitAndRightButtons;
                _lastHideDialoguePanel = s.HideDialoguePanel;

                ThemeApplier.ApplyHudArtVisibilityNow();
            }

            // ===================== Text Tint =====================

            GUILayout.Space(16f);
            GUILayout.Label("Text Tint (TMP)");

            s.EnableTextTint = GUILayout.Toggle(s.EnableTextTint, "Enable text tint");

            if (s.EnableTextTint)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"Text Strength: {s.TextStrength:0.00}");
                s.TextStrength = GUILayout.HorizontalSlider(s.TextStrength, 0f, 1f);

                GUILayout.Space(6f);
                GUILayout.Label("Text Tint Color (warm ink):");
                s.TextR = Slider01("R", s.TextR);
                s.TextG = Slider01("G", s.TextG);
                s.TextB = Slider01("B", s.TextB);

                GUILayout.Space(6f);
                s.SkipRichTextColoredSegments = GUILayout.Toggle(
                    s.SkipRichTextColoredSegments,
                    "Skip colored/emphasis text (recommended)"
                );
            }

            // ===================== Actions =====================

            GUILayout.Space(16f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reapply Tint Now"))
            {
                ThemeApplier.ReapplyAll();
                ThemeApplier.ApplyTextToAll();
            }

            if (GUILayout.Button("Restore Originals"))
            {
                ThemeApplier.RestoreAll();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label(
                "Tip: If something shouldn't tint, add an extra exclude token\n" +
                "like 'chat', 'log', or a parent object name you see in the UI."
            );
        }

        private static float Slider01(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(18));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.EndHorizontal();
            return value;
        }
    }
}
