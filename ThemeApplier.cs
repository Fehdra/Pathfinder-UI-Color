// DarkParchmentUI/ThemeApplier.cs

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkParchmentUI
{
    internal static class ThemeApplier
    {
        // ===================== TINT TRACKING (colors only) =====================
        private static readonly Dictionary<int, Color> _originalTintImageColors = new Dictionary<int, Color>(4096);
        private static readonly Dictionary<int, Color> _originalTintRawImageColors = new Dictionary<int, Color>(1024);

        private static readonly List<Image> _tintedImages = new List<Image>(4096);
        private static readonly List<RawImage> _tintedRawImages = new List<RawImage>(1024);

        // ===================== HUD HIDE TRACKING (enabled/raycast/color) =====================
        private struct HudState
        {
            public bool enabled;
            public bool raycastTarget;
            public Color color;
        }

        private static readonly Dictionary<int, HudState> _hudImageState = new Dictionary<int, HudState>(4096);
        private static readonly Dictionary<int, HudState> _hudRawState = new Dictionary<int, HudState>(1024);

        private static readonly List<Image> _hudTouchedImages = new List<Image>(4096);
        private static readonly List<RawImage> _hudTouchedRawImages = new List<RawImage>(1024);

        // ===================== Exclude cache =====================
        private static string[] _excludeTokensCache = Array.Empty<string>();
        private static string _excludeTokensKey = "";

        private static readonly Dictionary<int, bool> _excludeForPatchCache = new Dictionary<int, bool>(8192);
        private static readonly Dictionary<int, bool> _excludeForTextCache = new Dictionary<int, bool>(8192);

        internal static bool SuppressPatches;

        [ThreadStatic] private static Transform TMP_TextCurrentTransform;

        internal static void SetCurrentTextTransform(Transform t) => TMP_TextCurrentTransform = t;
        internal static void ClearCurrentTextTransform() => TMP_TextCurrentTransform = null;

        // ===================== Public API =====================

        public static void ClearAllTracking()
        {
            _originalTintImageColors.Clear();
            _originalTintRawImageColors.Clear();
            _tintedImages.Clear();
            _tintedRawImages.Clear();

            _hudImageState.Clear();
            _hudRawState.Clear();
            _hudTouchedImages.Clear();
            _hudTouchedRawImages.Clear();

            _excludeForPatchCache.Clear();
            _excludeForTextCache.Clear();
        }

        public static void RestoreAll()
        {
            SuppressPatches = true;
            try
            {
                RestoreTintOnly();
                RestoreHudOnly();
            }
            finally
            {
                SuppressPatches = false;
            }
        }

        public static void ReapplyAll()
        {
            SuppressPatches = true;
            try
            {
                RestoreTintOnly();     // or whatever your tint-restore function is called
                ApplyToAllCanvases();
            }
            finally
            {
                SuppressPatches = false;
            }
        }


        public static void ApplyToAllCanvases()
        {
            var s = Main.Settings;
            if (s == null) return;

            SuppressPatches = true;
            try
            {
                EnsureExcludeCache();

                if (s.Enabled || s.EnableChatTint)
                {
                    var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
                    for (int i = 0; i < canvases.Length; i++)
                    {
                        var c = canvases[i];
                        if (c == null) continue;
                        ApplyToRoot(c.gameObject);
                    }
                }
            }
            finally
            {
                SuppressPatches = false;
            }
        }

        public static void ApplyHudVisibilityNow()
        {
            SuppressPatches = true;
            try
            {
                // Only undo what HUD hiding changed (does NOT undo tint)
                RestoreHudOnly();
                ApplyHudVisibilityInternal();
            }
            finally
            {
                SuppressPatches = false;
            }
        }

        // ===================== Background Tint =====================

        public static void ApplyToRoot(GameObject root)
        {
            if (root == null) return;

            var s = Main.Settings;
            if (s == null) return;

            var uiTint = s.TintColor;
            float uiStrength = Clamp01(s.Strength);

            var chatTint = s.ChatTintColor;
            float chatStrength = Clamp01(s.ChatStrength);

            // Images
            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;

                // Chat tint FIRST (must be before HUD-skip)
                if (IsChatPanel(img.transform))
                {
                    if (!s.EnableChatTint) continue;
                    TintImage(img, chatTint, chatStrength);
                    continue;
                }

                if (!s.Enabled) continue;

                // Skip HUD groups for tint (but DO tint journal/encyclopedia screens)
                if (IsHudGroup12345(img.transform) && !IsJournalLikeScreen(img.transform))
                    continue;

                if (IsExcluded(img.transform, _excludeTokensCache)) continue;
                if (s.SkipSmallImages && IsSmall(img.rectTransform, s.SmallImageMaxSize)) continue;

                TintImage(img, uiTint, uiStrength);
            }

            // RawImages
            var raws = root.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < raws.Length; i++)
            {
                var r = raws[i];
                if (r == null) continue;

                if (IsChatPanel(r.transform))
                {
                    if (!s.EnableChatTint) continue;
                    TintRaw(r, chatTint, chatStrength);
                    continue;
                }

                if (!s.Enabled) continue;

                if (IsHudGroup12345(r.transform) && !IsJournalLikeScreen(r.transform))
                    continue;

                if (IsExcluded(r.transform, _excludeTokensCache)) continue;
                if (s.SkipSmallImages && IsSmall(r.rectTransform, s.SmallImageMaxSize)) continue;

                TintRaw(r, uiTint, uiStrength);
            }
        }

        private static void TintImage(Image img, Color tintTarget, float strength)
        {
            int id = img.GetInstanceID();
            if (!_originalTintImageColors.ContainsKey(id))
            {
                _originalTintImageColors[id] = img.color;
                _tintedImages.Add(img);
            }
            img.color = BlendMultiply(_originalTintImageColors[id], tintTarget, strength);
        }

        private static void TintRaw(RawImage r, Color tintTarget, float strength)
        {
            int id = r.GetInstanceID();
            if (!_originalTintRawImageColors.ContainsKey(id))
            {
                _originalTintRawImageColors[id] = r.color;
                _tintedRawImages.Add(r);
            }
            r.color = BlendMultiply(_originalTintRawImageColors[id], tintTarget, strength);
        }

        private static void RestoreTintOnly()
        {
            for (int i = _tintedImages.Count - 1; i >= 0; i--)
            {
                var img = _tintedImages[i];
                if (img == null) { _tintedImages.RemoveAt(i); continue; }

                int id = img.GetInstanceID();
                if (_originalTintImageColors.TryGetValue(id, out var c))
                    img.color = c;
            }

            for (int i = _tintedRawImages.Count - 1; i >= 0; i--)
            {
                var r = _tintedRawImages[i];
                if (r == null) { _tintedRawImages.RemoveAt(i); continue; }

                int id = r.GetInstanceID();
                if (_originalTintRawImageColors.TryGetValue(id, out var c))
                    r.color = c;
            }
        }

        // Used by UIElementPatches (Graphic.set_color). Preserves alpha.
        internal static Color TintIncoming(Color original)
        {
            var s = Main.Settings;
            if (s == null) return original;

            float strength = Clamp01(s.Strength);
            if (strength <= 0.0001f) return original;

            var tint = s.TintColor;
            var mul = new Color(original.r * tint.r, original.g * tint.g, original.b * tint.b, original.a);

            var res = Color.Lerp(original, mul, strength);
            res.a = original.a;
            return res;
        }

        internal static bool IsExcludedForPatch(Transform t)
        {
            if (t == null) return true;

            int id = t.GetInstanceID();
            if (_excludeForPatchCache.TryGetValue(id, out var cached))
                return cached;

            EnsureExcludeCache();

            bool res = IsHudGroup12345(t) || IsChatPanel(t) || IsExcluded(t, _excludeTokensCache);
            _excludeForPatchCache[id] = res;
            return res;
        }
        // Stable tint for the per-frame Graphic.set_color patch.
        // Prevents "tint stacking" when the game repeatedly sets colors during scene/UI transitions.
        internal static Color TintIncomingStable(Graphic g, Color incoming)
        {
            var s = Main.Settings;
            if (s == null) return incoming;

            float strength = Clamp01(s.Strength);
            if (strength <= 0.0001f) return incoming;

            float a = incoming.a;
            int id = g.GetInstanceID();

            // Use the ORIGINAL (untinted) color for this instance as the base.
            Color baseColor = incoming;

            if (g is Image img)
            {
                if (!_originalTintImageColors.TryGetValue(id, out baseColor))
                {
                    _originalTintImageColors[id] = incoming;
                    _tintedImages.Add(img);
                    baseColor = incoming;
                }
            }
            else if (g is RawImage raw)
            {
                if (!_originalTintRawImageColors.TryGetValue(id, out baseColor))
                {
                    _originalTintRawImageColors[id] = incoming;
                    _tintedRawImages.Add(raw);
                    baseColor = incoming;
                }
            }
            else
            {
                return incoming;
            }

            // Tint from the stable base, not from the incoming (possibly already tinted) value.
            var res = BlendMultiply(baseColor, s.TintColor, strength);
            res.a = a;
            return res;
        }

        // ===================== Text Tint =====================

        public static void ApplyTextToAll()
        {
            var s = Main.Settings;
            if (s == null || !s.EnableTextTint) return;

            SuppressPatches = true;
            try
            {
                EnsureExcludeCache();

                var texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    var t = texts[i];
                    if (t == null) continue;
                    if (IsExcludedForText(t.transform)) continue;

                    TMP_TextCurrentTransform = t.transform;
                    t.color = TintTextIncoming(t.color);
                    TMP_TextCurrentTransform = null;
                }
            }
            finally
            {
                SuppressPatches = false;
            }
        }

        internal static Color TintTextIncoming(Color original)
        {
            var s = Main.Settings;
            if (s == null) return original;

            float strength = Clamp01(s.TextStrength);
            Color ink = s.TextTintColor;

            var current = TMP_TextCurrentTransform;

            if (current != null)
            {
                bool inSpellContext = NameHasAny(current, 18, "spell", "ability", "spellbook", "tooltip", "inspect");

                if (inSpellContext)
                {
                    if (s.EnableSpellNameTint && IsSpellNameText(current))
                    {
                        strength = Clamp01(s.SpellNameStrength);
                        ink = s.SpellNameTintColor;
                    }
                    else
                    {
                        return original;
                    }
                }
                else if (s.EnableLoreTextTint && IsJournalOrEncyclopediaText(current))
                {
                    strength = Clamp01(s.LoreTextStrength);
                    ink = s.LoreTextTintColor;
                }
            }

            ink.a = original.a;
            return Color.Lerp(original, ink, strength);
        }

        internal static bool IsExcludedForText(Transform t)
        {
            if (t == null) return true;

            int id = t.GetInstanceID();
            if (_excludeForTextCache.TryGetValue(id, out var cached))
                return cached;

            EnsureExcludeCache();

            bool res = IsHudGroup12345(t) || IsExcluded(t, _excludeTokensCache);
            _excludeForTextCache[id] = res;
            return res;
        }

        // ===================== HUD HIDING =====================

        private static void ApplyHudVisibilityInternal()
        {
            var s = Main.Settings;
            if (s == null) return;

            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null) continue;

                // NEVER touch main menu / front-end UI
                if (IsFrontEndScreen(c.transform))
                    continue;

                bool isGameplayHud = IsGameplayHudCanvas(c.transform);
                bool isJournal = IsJournalLikeScreen(c.transform);

                // Gameplay HUD → allow HUD hiding
                if (isGameplayHud)
                {
                    ApplyHudVisibilityUnderRoot(
                        c.gameObject,
                        allowHudGroups: true,
                        allowJournalParchment: false
                    );
                    continue;
                }

                // Journal / Encyclopedia ONLY allow parchment hide
                if (s.HideJournalBackground && isJournal)
                {
                    ApplyHudVisibilityUnderRoot(
                        c.gameObject,
                        allowHudGroups: false,
                        allowJournalParchment: true
                    );
                }
            }
        }

        private static void ApplyHudVisibilityUnderRoot(GameObject root, bool allowHudGroups, bool allowJournalParchment)
        {
            var s = Main.Settings;
            if (s == null) return;

            // Images
            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;

                // Journal parchment only
                if (allowJournalParchment && IsJournalLikeScreen(img.transform))
                {
                    if (s.HideJournalBackground && IsJournalParchmentBackgroundCandidate(img.transform, img.rectTransform))
                        HideHudGraphic(img);
                    else
                        RestoreHudGraphic(img);

                    continue;
                }

                if (!allowHudGroups)
                    continue;

                if (IsPortraitLike(img.transform))
                {
                    RestoreHudGraphic(img);
                    continue;
                }

                bool isLeft = IsLeftCastBars(img.transform);
                bool isAction = IsActionBarStrip(img.transform);
                bool isCenter = IsCenterHudPlate(img.transform);
                bool isRight = IsRightHudButtons(img.transform);
                bool isDialog = IsDialoguePanel(img.transform);

                if (!isLeft && !isAction && !isCenter && !isRight && !isDialog)
                {
                    RestoreHudGraphic(img);
                    continue;
                }

                bool hide =
                    (s.HideLeftAndActionBar && (isLeft || isAction)) ||
                    (s.HideCenterAndRightButtons && (isCenter || isRight)) ||
                    (s.HideDialoguePanel && isDialog);

                if (!hide)
                {
                    RestoreHudGraphic(img);
                    continue;
                }

                bool requireArtFilter = (isLeft || isAction);
                if (requireArtFilter && !IsHudArtCandidate(img.transform, img.rectTransform))
                {
                    RestoreHudGraphic(img);
                    continue;
                }

                HideHudGraphic(img);
            }

            // RawImages
            var raws = root.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < raws.Length; i++)
            {
                var r = raws[i];
                if (r == null) continue;

                if (allowJournalParchment && IsJournalLikeScreen(r.transform))
                {
                    if (s.HideJournalBackground && IsJournalParchmentBackgroundCandidate(r.transform, r.rectTransform))
                        HideHudGraphic(r);
                    else
                        RestoreHudGraphic(r);

                    continue;
                }

                if (!allowHudGroups)
                    continue;

                if (IsPortraitLike(r.transform))
                {
                    RestoreHudGraphic(r);
                    continue;
                }

                bool isLeft = IsLeftCastBars(r.transform);
                bool isAction = IsActionBarStrip(r.transform);
                bool isCenter = IsCenterHudPlate(r.transform);
                bool isRight = IsRightHudButtons(r.transform);
                bool isDialog = IsDialoguePanel(r.transform);

                if (!isLeft && !isAction && !isCenter && !isRight && !isDialog)
                {
                    RestoreHudGraphic(r);
                    continue;
                }

                bool hide =
                    (s.HideLeftAndActionBar && (isLeft || isAction)) ||
                    (s.HideCenterAndRightButtons && (isCenter || isRight)) ||
                    (s.HideDialoguePanel && isDialog);

                if (!hide)
                {
                    RestoreHudGraphic(r);
                    continue;
                }

                bool requireArtFilter = (isLeft || isAction);
                if (requireArtFilter && !IsHudArtCandidate(r.transform, r.rectTransform))
                {
                    RestoreHudGraphic(r);
                    continue;
                }

                HideHudGraphic(r);
            }
        }

        private static void HideHudGraphic(Image img)
        {
            if (img == null) return;
            if (IsSelectableTargetGraphic(img)) return;

            int id = img.GetInstanceID();
            if (!_hudImageState.ContainsKey(id))
            {
                _hudImageState[id] = new HudState { enabled = img.enabled, raycastTarget = img.raycastTarget, color = img.color };
                _hudTouchedImages.Add(img);
            }

            // Hide visually + prevent blocking clicks behind it
            img.raycastTarget = false;
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }

        private static void HideHudGraphic(RawImage r)
        {
            if (r == null) return;
            if (IsSelectableTargetGraphic(r)) return;

            int id = r.GetInstanceID();
            if (!_hudRawState.ContainsKey(id))
            {
                _hudRawState[id] = new HudState { enabled = r.enabled, raycastTarget = r.raycastTarget, color = r.color };
                _hudTouchedRawImages.Add(r);
            }

            r.raycastTarget = false;
            var c = r.color;
            c.a = 0f;
            r.color = c;
        }

        private static void RestoreHudGraphic(Image img)
        {
            if (img == null) return;
            int id = img.GetInstanceID();

            if (_hudImageState.TryGetValue(id, out var st))
            {
                img.enabled = st.enabled;
                img.raycastTarget = st.raycastTarget;
                img.color = st.color;
            }
        }

        private static void RestoreHudGraphic(RawImage r)
        {
            if (r == null) return;
            int id = r.GetInstanceID();

            if (_hudRawState.TryGetValue(id, out var st))
            {
                r.enabled = st.enabled;
                r.raycastTarget = st.raycastTarget;
                r.color = st.color;
            }
        }

        private static void RestoreHudOnly()
        {
            for (int i = _hudTouchedImages.Count - 1; i >= 0; i--)
            {
                var img = _hudTouchedImages[i];
                if (img == null) { _hudTouchedImages.RemoveAt(i); continue; }
                RestoreHudGraphic(img);
            }

            for (int i = _hudTouchedRawImages.Count - 1; i >= 0; i--)
            {
                var r = _hudTouchedRawImages[i];
                if (r == null) { _hudTouchedRawImages.RemoveAt(i); continue; }
                RestoreHudGraphic(r);
            }
        }

        private static bool IsSelectableTargetGraphic(Graphic g)
        {
            if (g == null) return false;

            var sel = g.GetComponent<Selectable>();
            if (sel != null && sel.targetGraphic == g) return true;

            var selParent = g.GetComponentInParent<Selectable>();
            if (selParent != null && selParent.targetGraphic == g) return true;

            return false;
        }

        // ===================== Candidates =====================

        private static bool IsHudArtCandidate(Transform t, RectTransform rt)
        {
            if (rt == null) return false;

            float w = rt.rect.width;
            float h = rt.rect.height;

            if (w <= 90f && h <= 90f) return false;
            if (w <= 8f || h <= 8f) return false;

            string lower = (t != null ? (t.name ?? "") : "").ToLowerInvariant();

            if (lower.Contains("bg") ||
                lower.Contains("background") ||
                lower.Contains("back") ||
                lower.Contains("paper") ||
                lower.Contains("parchment") ||
                lower.Contains("decor") ||
                lower.Contains("art"))
                return true;

            if (lower.Contains("icon") ||
                lower.Contains("button") ||
                lower.Contains("slot") ||
                lower.Contains("ability") ||
                lower.Contains("spell") ||
                lower.Contains("skill"))
                return false;

            float area = w * h;
            if (area >= 12000f && (w >= 160f || h >= 160f))
                return true;

            return false;
        }

        // Strict: hide ONLY very large background art with explicit BG-ish names.
        private static bool IsJournalParchmentBackgroundCandidate(Transform t, RectTransform rt)
        {
            if (rt == null) return false;

            float w = rt.rect.width;
            float h = rt.rect.height;

            if (w < 600f || h < 400f) return false;

            string lower = (t != null ? (t.name ?? "") : "").ToLowerInvariant();

            return lower.Contains("background") ||
                   lower.Contains("parchment_bg") ||
                   lower.Contains("paper_bg") ||
                   lower.Contains("journal_bg") ||
                   lower.Contains("encyclopedia_bg") ||
                   (lower.Contains("parchment") && lower.Contains("bg"));
        }

        // ===================== Group Detection (1..5) =====================

        private static bool IsLeftCastBars(Transform t)
        {
            if (IsNonHudScreen(t) || IsFrontEndScreen(t)) return false;
            if (NameHasAny(t, 12, "map", "minimap", "worldmap", "areamap", "localmap")) return false;

            return NameHasAny(t, 18,
                "groups", "group", "unitgroup", "grouppanel",
                "castbar", "leftbar", "leftpanel");
        }

        private static bool IsActionBarStrip(Transform t)
        {
            if (IsNonHudScreen(t) || IsFrontEndScreen(t)) return false;
            if (NameHasAny(t, 12, "map", "minimap", "worldmap", "areamap", "localmap")) return false;

            // IMPORTANT: do NOT include "scroll" or a generic "bar" here (it breaks inventory/stash/journal).
            return NameHasAny(t, 18,
                "actionbar", "abilitybar", "hotbar", "spellbar",
                "commandbar", "quickbar", "skillbar", "itembar");
        }

        private static bool IsCenterHudPlate(Transform t)
        {
            if (IsNonHudScreen(t) || IsFrontEndScreen(t)) return false;
            if (IsPortraitLike(t)) return false;

            return NameHasAny(t, 18,
                "center", "mainpanel", "centerpanel",
                "unitpanel", "partybar", "mainbar",
                "hudplate", "backplate", "uipanel");
        }

        private static bool IsRightHudButtons(Transform t)
        {
            if (IsNonHudScreen(t) || IsFrontEndScreen(t)) return false;

            return NameHasAny(t, 18,
                "sidebuttons", "rightbuttons", "utilitybuttons",
                "rightpanel", "hudright", "hudbuttons");
        }

        private static bool IsDialoguePanel(Transform t)
        {
            if (IsNonHudScreen(t) || IsFrontEndScreen(t)) return false;

            return NameHasAny(t, 18,
                "dialog", "dialogue", "conversation",
                "chat", "combatlog", "log", "history", "textbox");
        }

        private static bool IsHudGroup12345(Transform t)
        {
            return IsLeftCastBars(t) || IsActionBarStrip(t) || IsCenterHudPlate(t) || IsRightHudButtons(t) || IsDialoguePanel(t) || IsPortraitLike(t);
        }

        private static bool IsPortraitLike(Transform t)
        {
            if (IsNonHudScreen(t) || IsFrontEndScreen(t)) return false;

            return NameHasAny(t, 20,
                "portrait", "portraits", "charportrait", "unitportrait",
                "avatar", "charactericon", "partyicon", "unitframe");
        }


        private static bool IsChatPanel(Transform t)
        {
            return NameHasAny(t, 18,
                "dialog", "dialogue", "conversation",
                "chat", "combatlog", "log", "history", "textbox");
        }

        private static bool IsJournalLikeScreen(Transform t)
        {
            return NameHasAny(t, 28,
                "journal", "questjournal",
                "encyclopedia", "codex", "glossary", "lore");
        }

        private static bool IsFrontEndScreen(Transform t)
        {
            return NameHasAny(t, 32,
                "mainmenu", "title", "frontend", "front_end",
                "credits", "license", "newgame", "continue", "loadgame",
                "settings", "options");
        }

        private static bool IsGameplayHudCanvas(Transform t)
        {
            // Only treat canvases that look like in-game HUD as HUD.
            // Anything that smells like inventory/stash/spellbook/journal/etc is NOT HUD.
            if (NameHasAny(t, 40,
                    "inventory", "stash", "sharedstash", "loot", "vendor", "trade",
                    "spellbook", "journal", "encyclopedia", "codex", "glossary",
                    "character", "mythic", "map", "worldmap", "areamap", "minimap", "localmap"))
                return false;

            return NameHasAny(t, 40,
                "hud", "ingame", "in_game", "gameui", "game_ui", "surfacehud", "uimain", "mainui");
        }

        private static bool IsSpellNameText(Transform t)
        {
            bool hasNameToken = NameHasAny(t, 10, "spellname", "abilityname", "name", "title", "header");
            bool inSpellContext = NameHasAny(t, 18, "spell", "ability", "spellbook", "tooltip", "inspect");
            return hasNameToken && inSpellContext && !NameHasAny(t, 8, "description", "desc", "body", "text");
        }

        private static bool IsJournalOrEncyclopediaText(Transform t)
        {
            return NameHasAny(t, 40,
                "journal", "questjournal", "quest",
                "encyclopedia", "codex", "glossary", "lore",
                "commonterms", "terms", "tutorial", "basics",
                "page", "entry", "article", "bookmark", "book",
                "scrollview", "scroll", "viewport", "content", "body", "description");
        }

        private static bool IsNonHudScreen(Transform t)
        {
            // Screens where HUD hiding should never apply.
            return NameHasAny(t, 28,
                "journal", "encyclopedia", "spellbook", "inventory", "character",
                "mythic", "map", "worldmap", "areamap", "minimap", "localmap",
                "stash", "sharedstash", "loot", "vendor", "trade");
        }

        private static bool NameHasAny(Transform t, int maxDepth, params string[] tokens)
        {
            for (int depth = 0; depth < maxDepth && t != null; depth++, t = t.parent)
            {
                var n = t.name;
                if (string.IsNullOrEmpty(n)) continue;

                var lower = n.ToLowerInvariant();
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (lower.Contains(tokens[i]))
                        return true;
                }
            }
            return false;
        }

        // ===================== Exclude Cache + Helpers =====================

        internal static void EnsureExcludeCache()
        {
            var s = Main.Settings;
            if (s == null) return;

            var key = s.ExtraExcludeTokens ?? "";
            if (!string.Equals(_excludeTokensKey, key, StringComparison.Ordinal))
            {
                _excludeTokensKey = key;
                _excludeTokensCache = BuildExcludeTokens(s.ExtraExcludeTokens);
                _excludeForPatchCache.Clear();
                _excludeForTextCache.Clear();
            }
        }

        private static string[] BuildExcludeTokens(string extraTokensCsv)
        {
            if (string.IsNullOrEmpty(extraTokensCsv))
                return Array.Empty<string>();

            var list = new List<string>(16);
            var parts = extraTokensCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var tok = parts[i].Trim();
                if (tok.Length > 0)
                    list.Add(tok.ToLowerInvariant());
            }

            return list.Distinct().ToArray();
        }

        private static bool IsExcluded(Transform t, string[] tokens)
        {
            if (tokens == null || tokens.Length == 0) return false;
            if (t == null) return false;

            for (int depth = 0; depth < 14 && t != null; depth++, t = t.parent)
            {
                var n = t.name;
                if (string.IsNullOrEmpty(n)) continue;

                var lower = n.ToLowerInvariant();
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (lower.Contains(tokens[i]))
                        return true;
                }
            }

            return false;
        }

        private static Color BlendMultiply(Color original, Color tintTarget, float strength)
        {
            var mul = new Color(
                original.r * tintTarget.r,
                original.g * tintTarget.g,
                original.b * tintTarget.b,
                original.a * tintTarget.a
            );
            return Color.Lerp(original, mul, strength);
        }

        private static bool IsSmall(RectTransform rt, float maxSize)
        {
            if (rt == null) return false;
            var rect = rt.rect;
            return rect.width <= maxSize && rect.height <= maxSize;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
