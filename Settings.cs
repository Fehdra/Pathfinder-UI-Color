// DarkParchmentUI/Settings.cs
// C# 7.3 compatible

using UnityEngine;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool Enabled = true;

        // Tint strength 0..1 (0 = no change, 1 = full multiply)
        public float Strength = 0.35f;

        // Parchment-ish default tint target (warm, slightly darker)
        public float TintR = 0.92f;
        public float TintG = 0.86f;
        public float TintB = 0.75f;

        // Multiply alpha (usually keep 1)
        public float AlphaMul = 1.0f;

        // Exclude chat/log panels
        public bool ExcludeChatLike = true;

        // Optional extra exclude tokens (comma separated): e.g. "journal, encyclopedia"
        public string ExtraExcludeTokens = "";

        // Optional: skip tiny images (icons) to reduce “muddy UI”
        public bool SkipSmallImages = false;
        public float SmallImageMaxSize = 40f;

        public Color TintColor
            => new Color(Clamp01(TintR), Clamp01(TintG), Clamp01(TintB), Clamp01(AlphaMul));

        // ===================== Chat Tint =====================
        public bool EnableChatTint = false;
        public float ChatStrength = 0.35f;

        // chat parchment tint default (tweak later)
        public float ChatTintR = 0.92f;
        public float ChatTintG = 0.86f;
        public float ChatTintB = 0.75f;

        public Color ChatTintColor
            => new Color(Clamp01(ChatTintR), Clamp01(ChatTintG), Clamp01(ChatTintB), Clamp01(AlphaMul));

        // ===================== Text Tint =====================
        public bool EnableTextTint = false;
        public float TextStrength = 0.35f;

        // “Warm ink” default
        public float TextR = 0.95f;
        public float TextG = 0.93f;
        public float TextB = 0.88f;

        // Optional: don’t recolor already-colored text
        public bool SkipRichTextColoredSegments = true;

        public Color TextTintColor => new Color(Clamp01(TextR), Clamp01(TextG), Clamp01(TextB), 1f);

        // ===================== HUD ART HIDING =====================
        public bool HideLeftAndActionBar = false;     // hides 1 + 2
        public bool HidePortraitAndRightButtons = false; // hides 3 + 4
        public bool HideDialoguePanel = false;        // hides 5

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }
}
