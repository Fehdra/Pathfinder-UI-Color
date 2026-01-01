// DarkParchmentUI/Settings.cs


using UnityEngine;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    public class Settings : UnityModManager.ModSettings
    {
        // ===================== Global Tint =====================
        public bool Enabled = true;

        // Tint strength 0..1 (0 = no change, 1 = full multiply)
        public float Strength = 0.35f;

        // Parchment-ish default tint target (warm, slightly darker)
        public float TintR = 0.92f;
        public float TintG = 0.86f;
        public float TintB = 0.75f;

        // Multiply alpha (usually keep 1)
        public float AlphaMul = 1.0f;

        // Optional extra exclude tokens (comma separated): e.g. "journal, encyclopedia"
        public string ExtraExcludeTokens = "";

        // Optional: skip tiny images (icons) to reduce “muddy UI”
        public bool SkipSmallImages = false;
        public float SmallImageMaxSize = 40f;

        public Color TintColor
            => new Color(Clamp01(TintR), Clamp01(TintG), Clamp01(TintB), Clamp01(AlphaMul));

        // ===================== Chat / Info Window Tint =====================
        public bool EnableChatTint = false;
        public float ChatStrength = 0.35f;

        public float ChatTintR = 0.92f;
        public float ChatTintG = 0.86f;
        public float ChatTintB = 0.75f;

        public Color ChatTintColor
            => new Color(Clamp01(ChatTintR), Clamp01(ChatTintG), Clamp01(ChatTintB), 1f);

        // ===================== Text Tint (TMP) =====================
        public bool EnableTextTint = false;
        public float TextStrength = 0.35f;

        [Draw("===== SPELL NAME TEXT =====")]
        public bool _spellTextHeader;

        [Draw("Enable spell name tint")]
        public bool EnableSpellNameTint = false;

        [Draw("Spell name strength (0..1)", Min = 0f, Max = 1f)]
        public float SpellNameStrength = 1.0f;

        [Draw("Spell name tint color")]
        public Color SpellNameTintColor = new Color(0.85f, 0.72f, 0.35f, 1f);

        [Draw("===== JOURNAL / ENCYCLOPEDIA TEXT =====")]
        public bool _loreTextHeader;

        [Draw("Enable journal/encyclopedia tint")]
        public bool EnableLoreTextTint = false;

        [Draw("Journal/encyclopedia strength (0..1)", Min = 0f, Max = 1f)]
        public float LoreTextStrength = 1.0f;

        [Draw("Journal/encyclopedia tint color")]
        public Color LoreTextTintColor = new Color(0.75f, 0.70f, 0.60f, 1f);


        // “Warm ink” default
        public float TextR = 0.95f;
        public float TextG = 0.93f;
        public float TextB = 0.88f;

        // Optional: don’t recolor already-colored text
        public bool SkipRichTextColoredSegments = true;

        public Color TextTintColor
            => new Color(Clamp01(TextR), Clamp01(TextG), Clamp01(TextB), 1f);

        // ===================== HUD HIDING =====================
        public bool HideLeftAndActionBar = false;          // (1 + 2)
        public bool HideCenterAndRightButtons = false;     // (3 + 4)  (NOT portrait)
        public bool HideDialoguePanel = false;             // (5)
        public bool HideJournalBackground = false;        // Journal / Encyclopedia parchment

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }
}
