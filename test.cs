// DarkParchmentUI/UMMMenu.cs
// C# 7.3 compatible

using UnityEngine;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    internal static class UMMMenu
    {
        // track last values so we only apply visibility once when toggles change
        private static bool _lastHideActionBarArt;
        private static bool _lastHideLeftQuickbar;
        private static bool _lastHideCenterHudArt;

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

            // ===================== HUD ART HIDING =====================

            GUILayout.Space(10f);
            GUILayout.Label("HUD Art Hiding", UnityModManager.UI.h2);

            s.HideActionBarArt = GUILayout.Toggle(s.HideActionBarArt,
                "Hide action bar background art (keep spell icons)");

            s.HideLeftQuickbar = GUILayout.Toggle(s.HideLeftQuickbar,
                "Hide left-side popout spell bars");

            s.HideCenterHudArt = GUILayout.Toggle(s.HideCenterHudArt,
                "Hide center/bottom HUD parchment panels");

            // if any hide toggle changed, apply immediately
            if (_lastHideActionBarArt != s.HideActionBarArt ||
                _lastHideLeftQuickbar != s.HideLeftQuickbar ||
                _lastHideCenterHudArt != s.HideCenterHudArt)
            {
                _lastHideActionBarArt = s.HideActionBarArt;
                _lastHideLeftQuickbar = s.HideLeftQuickbar;
                _lastHideCenterHudArt = s.HideCenterHudArt;

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
