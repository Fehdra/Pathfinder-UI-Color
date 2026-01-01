// DarkParchmentUI/UMMMenu.cs

using System.Collections;
using UnityEngine;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    internal static class UMMMenu
    {
        // Track last values so we only do expensive reapply when something actually changed
        private static bool _lastEnabled;
        private static float _lastStrength;
        private static float _lastTintR, _lastTintG, _lastTintB;
        private static float _lastAlphaMul;
        private static string _lastExtraExcludeTokens;
        private static bool _lastSkipSmallImages;
        private static float _lastSmallImageMaxSize;

        private static bool _lastEnableChatTint;
        private static float _lastChatStrength;
        private static float _lastChatTintR, _lastChatTintG, _lastChatTintB;

        private static bool _lastEnableTextTint;
        private static float _lastTextStrength;
        private static float _lastTextR, _lastTextG, _lastTextB;
        private static bool _lastSkipRichText;

        // Spell-name + lore text tint tracking
        private static bool _lastEnableSpellNameTint;
        private static float _lastSpellNameStrength;
        private static Color _lastSpellNameTintColor;

        private static bool _lastEnableLoreTextTint;
        private static float _lastLoreTextStrength;
        private static Color _lastLoreTextTintColor;

        // HUD hiding
        private static bool _lastHideLeftAndActionBar;
        private static bool _lastHideCenterAndRightButtons;
        private static bool _lastHideDialoguePanel;

        private static bool _lastHideJournalBackground;
        // Debounce expensive work while dragging sliders
        private static int _bgSeq;
        private static int _textSeq;
        private static int _hudSeq;

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

            GUILayout.Space(8f);
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

            // ===================== Chat / Info Window Tint =====================
            GUILayout.Space(12f);
            GUILayout.Label("Chat - Info Window Tint", UnityModManager.UI.h2);

            s.EnableChatTint = GUILayout.Toggle(s.EnableChatTint, "Enable chat - info tint");

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
            GUILayout.Space(12f);
            GUILayout.Label("HUD Hiding", UnityModManager.UI.h2);

            s.HideLeftAndActionBar = GUILayout.Toggle(
                s.HideLeftAndActionBar,
                "Hide left cast bars + action bar (1 + 2)"
            );

            s.HideCenterAndRightButtons = GUILayout.Toggle(
                s.HideCenterAndRightButtons,
                "Hide center HUD plate + right HUD buttons (3 + 4)"
            );

            s.HideDialoguePanel = GUILayout.Toggle(
                s.HideDialoguePanel,
                "Hide dialogue - chat panel (5)"
            );


            s.HideJournalBackground = GUILayout.Toggle(
                s.HideJournalBackground,
                "Hide Journal - Encyclopedia parchment background"
            );
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

                // ===== Spell names =====
                GUILayout.Space(10f);
                GUILayout.Label("Spell Name Tint (tooltip titles)", UnityModManager.UI.h2);

                s.EnableSpellNameTint = GUILayout.Toggle(s.EnableSpellNameTint, "Enable spell name tint");
                if (s.EnableSpellNameTint)
                {
                    GUILayout.Space(6f);
                    GUILayout.Label($"Spell name strength: {s.SpellNameStrength:0.00}");
                    s.SpellNameStrength = GUILayout.HorizontalSlider(s.SpellNameStrength, 0f, 1f);

                    GUILayout.Space(6f);
                    GUILayout.Label("Spell name tint color:");
                    s.SpellNameTintColor = DrawColorSliders(s.SpellNameTintColor);
                }

                // ===== Journal / Encyclopedia =====
                GUILayout.Space(10f);
                GUILayout.Label("Journal / Encyclopedia Text Tint", UnityModManager.UI.h2);

                s.EnableLoreTextTint = GUILayout.Toggle(s.EnableLoreTextTint, "Enable journal/encyclopedia tint");
                if (s.EnableLoreTextTint)
                {
                    GUILayout.Space(6f);
                    GUILayout.Label($"Journal/encyclopedia strength: {s.LoreTextStrength:0.00}");
                    s.LoreTextStrength = GUILayout.HorizontalSlider(s.LoreTextStrength, 0f, 1f);

                    GUILayout.Space(6f);
                    GUILayout.Label("Journal/encyclopedia tint color:");
                    s.LoreTextTintColor = DrawColorSliders(s.LoreTextTintColor);
                }
            }

            // ===================== Apply-on-change =====================
            bool backgroundChanged =
                _lastEnabled != s.Enabled ||
                !Mathf.Approximately(_lastStrength, s.Strength) ||
                !Mathf.Approximately(_lastTintR, s.TintR) ||
                !Mathf.Approximately(_lastTintG, s.TintG) ||
                !Mathf.Approximately(_lastTintB, s.TintB) ||
                !Mathf.Approximately(_lastAlphaMul, s.AlphaMul) ||
                _lastSkipSmallImages != s.SkipSmallImages ||
                !Mathf.Approximately(_lastSmallImageMaxSize, s.SmallImageMaxSize) ||
                !string.Equals(_lastExtraExcludeTokens ?? "", s.ExtraExcludeTokens ?? "", System.StringComparison.Ordinal);

            bool chatChanged =
                _lastEnableChatTint != s.EnableChatTint ||
                !Mathf.Approximately(_lastChatStrength, s.ChatStrength) ||
                !Mathf.Approximately(_lastChatTintR, s.ChatTintR) ||
                !Mathf.Approximately(_lastChatTintG, s.ChatTintG) ||
                !Mathf.Approximately(_lastChatTintB, s.ChatTintB);

            bool textChanged =
                _lastEnableTextTint != s.EnableTextTint ||
                !Mathf.Approximately(_lastTextStrength, s.TextStrength) ||
                !Mathf.Approximately(_lastTextR, s.TextR) ||
                !Mathf.Approximately(_lastTextG, s.TextG) ||
                !Mathf.Approximately(_lastTextB, s.TextB) ||
                _lastSkipRichText != s.SkipRichTextColoredSegments ||
                _lastEnableSpellNameTint != s.EnableSpellNameTint ||
                !Mathf.Approximately(_lastSpellNameStrength, s.SpellNameStrength) ||
                _lastSpellNameTintColor != s.SpellNameTintColor ||
                _lastEnableLoreTextTint != s.EnableLoreTextTint ||
                !Mathf.Approximately(_lastLoreTextStrength, s.LoreTextStrength) ||
                _lastLoreTextTintColor != s.LoreTextTintColor;

            if (backgroundChanged || chatChanged)
            {
                _lastEnabled = s.Enabled;
                _lastStrength = s.Strength;
                _lastTintR = s.TintR;
                _lastTintG = s.TintG;
                _lastTintB = s.TintB;
                _lastAlphaMul = s.AlphaMul;
                _lastSkipSmallImages = s.SkipSmallImages;
                _lastSmallImageMaxSize = s.SmallImageMaxSize;
                _lastExtraExcludeTokens = s.ExtraExcludeTokens ?? "";

                _lastEnableChatTint = s.EnableChatTint;
                _lastChatStrength = s.ChatStrength;
                _lastChatTintR = s.ChatTintR;
                _lastChatTintG = s.ChatTintG;
                _lastChatTintB = s.ChatTintB;

                QueueBackgroundApply();
            }

            if (textChanged)
            {
                _lastEnableTextTint = s.EnableTextTint;
                _lastTextStrength = s.TextStrength;
                _lastTextR = s.TextR;
                _lastTextG = s.TextG;
                _lastTextB = s.TextB;
                _lastSkipRichText = s.SkipRichTextColoredSegments;

                _lastEnableSpellNameTint = s.EnableSpellNameTint;
                _lastSpellNameStrength = s.SpellNameStrength;
                _lastSpellNameTintColor = s.SpellNameTintColor;

                _lastEnableLoreTextTint = s.EnableLoreTextTint;
                _lastLoreTextStrength = s.LoreTextStrength;
                _lastLoreTextTintColor = s.LoreTextTintColor;

                QueueTextApply();
            }

            if (_lastHideLeftAndActionBar != s.HideLeftAndActionBar ||
                _lastHideCenterAndRightButtons != s.HideCenterAndRightButtons ||
                _lastHideDialoguePanel != s.HideDialoguePanel)
            {
                _lastHideLeftAndActionBar = s.HideLeftAndActionBar;
                _lastHideCenterAndRightButtons = s.HideCenterAndRightButtons;
                _lastHideDialoguePanel = s.HideDialoguePanel;

                _lastHideJournalBackground = s.HideJournalBackground;
                QueueHudApply();

            }

            // ===================== Actions =====================
            GUILayout.Space(16f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reapply Tint Now"))
            {
                ThemeApplier.ReapplyAll();
                ThemeApplier.ApplyHudVisibilityNow();
                if (s.EnableTextTint) ThemeApplier.ApplyTextToAll();
            }

            if (GUILayout.Button("Restore Originals"))
            {
                ThemeApplier.RestoreAll();
            }

            GUILayout.EndHorizontal();
        }

        private static void QueueBackgroundApply()
        {
            Runner.Ensure();
            _bgSeq++;
            int seq = _bgSeq;
            Runner.Instance.Run(DelayedBackgroundApply(seq));
        }


        private static IEnumerator DelayedBackgroundApply(int seq)
        {
            yield return new WaitForSeconds(0.25f);
            if (seq != _bgSeq) yield break;

            ThemeApplier.ReapplyAll();
            ThemeApplier.ApplyHudVisibilityNow();

            if (Main.Settings != null && Main.Settings.EnableTextTint)
                ThemeApplier.ApplyTextToAll();
        }

        private static void QueueTextApply()
        {
            Runner.Ensure();
            _textSeq++;
            int seq = _textSeq;
            Runner.Instance.Run(DelayedTextApply(seq));
        }

        private static IEnumerator DelayedTextApply(int seq)
        {
            yield return new WaitForSeconds(0.25f);
            if (seq != _textSeq) yield break;

            if (Main.Settings != null && Main.Settings.EnableTextTint)
                ThemeApplier.ApplyTextToAll();
        }

        private static void QueueHudApply()
        {
            Runner.Ensure();
            _hudSeq++;
            int seq = _hudSeq;
            Runner.Instance.Run(DelayedHudApply(seq));
        }

        private static IEnumerator DelayedHudApply(int seq)
        {
            yield return new WaitForSeconds(0.10f);
            if (seq != _hudSeq) yield break;

            ThemeApplier.ApplyHudVisibilityNow();
        }

        private static Color DrawColorSliders(Color c)
        {
            c.r = Slider01("R", c.r);
            c.g = Slider01("G", c.g);
            c.b = Slider01("B", c.b);
            return c;
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
