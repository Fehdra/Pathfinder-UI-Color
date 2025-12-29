// DarkParchmentUI/ThemeApplier.cs
// C# 7.3 compatible

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
        // Track originals so we can restore + reapply safely
        private static readonly Dictionary<int, Color> _originalImageColors = new Dictionary<int, Color>(4096);
        private static readonly Dictionary<int, Color> _originalRawImageColors = new Dictionary<int, Color>(1024);

        // Track original enabled state for HUD hiding
        private static readonly Dictionary<int, bool> _originalImageEnabled = new Dictionary<int, bool>(2048);
        private static readonly Dictionary<int, bool> _originalRawImageEnabled = new Dictionary<int, bool>(512);

        // Track references for reapply (we prune nulls)
        private static readonly List<Image> _images = new List<Image>(4096);
        private static readonly List<RawImage> _rawImages = new List<RawImage>(1024);

        private static string[] _excludeTokensCache = Array.Empty<string>();
        private static string _excludeTokensKey = "";

        internal static bool SuppressPatches;

        // ===================== Tracking =====================

        public static void ClearAllTracking()
        {
            _originalImageColors.Clear();
            _originalRawImageColors.Clear();
            _originalImageEnabled.Clear();
            _originalRawImageEnabled.Clear();
            _images.Clear();
            _rawImages.Clear();
        }

        public static void RestoreAll()
        {
            SuppressPatches = true;
            try
            {
                for (int i = _images.Count - 1; i >= 0; i--)
                {
                    var img = _images[i];
                    if (img == null) { _images.RemoveAt(i); continue; }

                    int id = img.GetInstanceID();
                    if (_originalImageColors.TryGetValue(id, out var c))
                        img.color = c;
                    if (_originalImageEnabled.TryGetValue(id, out var en))
                        img.enabled = en;
                }

                for (int i = _rawImages.Count - 1; i >= 0; i--)
                {
                    var r = _rawImages[i];
                    if (r == null) { _rawImages.RemoveAt(i); continue; }

                    int id = r.GetInstanceID();
                    if (_originalRawImageColors.TryGetValue(id, out var c))
                        r.color = c;
                    if (_originalRawImageEnabled.TryGetValue(id, out var en))
                        r.enabled = en;
                }
            }
            finally
            {
                SuppressPatches = false;
            }
        }

        // ===================== Background Tint =====================

        public static void ApplyToAllCanvases()
        {
            var s = Main.Settings;
            if (s == null) return;

            SuppressPatches = true;
            try
            {
                EnsureExcludeCache();

                // Only tint when enabled
                if (s.Enabled)
                {
                    var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
                    for (int i = 0; i < canvases.Length; i++)
                    {
                        var c = canvases[i];
                        if (c == null) continue;
                        ApplyToRoot(c.gameObject);
                    }
                }

                // Always apply HUD hiding (independent of tint)
                ApplyHudVisibilityInternal();
            }
            finally
            {
                SuppressPatches = false;
            }
        }

        public static void ApplyToRoot(GameObject root)
        {
            if (root == null) return;

            var s = Main.Settings;
            if (s == null || !s.Enabled) return;

            var tintTarget = s.TintColor;
            float strength = Clamp01(s.Strength);

            // -------------------- Images --------------------
            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;

                // Chat tint is special and happens BEFORE exclusions
                if (IsChatPanel(img.transform))
                {
                    if (!s.EnableChatTint) continue;

                    var chatTint = s.ChatTintColor;
                    float chatStrength = Clamp01(s.ChatStrength);

                    int id = img.GetInstanceID();
                    if (!_originalImageColors.ContainsKey(id))
                    {
                        _originalImageColors[id] = img.color;
                        _images.Add(img);
                    }

                    img.color = BlendMultiply(_originalImageColors[id], chatTint, chatStrength);
                    continue;
                }

                // never tint HUD groups or portraits
                if (IsPortraitLike(img.transform)) continue;
                if (IsHudGroup12345(img.transform)) continue;

                if (IsExcluded(img.transform, _excludeTokensCache)) continue;
                if (s.SkipSmallImages && IsSmall(img.rectTransform, s.SmallImageMaxSize)) continue;

                int imgId = img.GetInstanceID();
                if (!_originalImageColors.ContainsKey(imgId))
                {
                    _originalImageColors[imgId] = img.color;
                    _images.Add(img);
                }

                img.color = BlendMultiply(_originalImageColors[imgId], tintTarget, strength);
            }

            // -------------------- RawImages --------------------
            var rawImages = root.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < rawImages.Length; i++)
            {
                var r = rawImages[i];
                if (r == null) continue;

                // Chat tint is special and happens BEFORE exclusions
                if (IsChatPanel(r.transform))
                {
                    if (!s.EnableChatTint) continue;

                    var chatTint = s.ChatTintColor;
                    float chatStrength = Clamp01(s.ChatStrength);

                    int id = r.GetInstanceID();
                    if (!_originalRawImageColors.ContainsKey(id))
                    {
                        _originalRawImageColors[id] = r.color;
                        _rawImages.Add(r);
                    }

                    r.color = BlendMultiply(_originalRawImageColors[id], chatTint, chatStrength);
                    continue;
                }

                if (IsPortraitLike(r.transform)) continue;
                if (IsHudGroup12345(r.transform)) continue;

                if (IsExcluded(r.transform, _excludeTokensCache)) continue;
                if (s.SkipSmallImages && IsSmall(r.rectTransform, s.SmallImageMaxSize)) continue;

                int rawId = r.GetInstanceID();
                if (!_originalRawImageColors.ContainsKey(rawId))
                {
                    _originalRawImageColors[rawId] = r.color;
                    _rawImages.Add(r);
                }

                r.color = BlendMultiply(_originalRawImageColors[rawId], tintTarget, strength);
            }
        }

        public static void ApplyToSingle(Image img)
        {
            if (img == null) return;

            var s = Main.Settings;
            if (s == null || !s.Enabled) return;

            EnsureExcludeCache();

            // Chat special
            if (IsChatPanel(img.transform))
            {
                if (!s.EnableChatTint) return;

                int id = img.GetInstanceID();
                if (!_originalImageColors.ContainsKey(id))
                {
                    _originalImageColors[id] = img.color;
                    _images.Add(img);
                }

                img.color = BlendMultiply(_originalImageColors[id], s.ChatTintColor, Clamp01(s.ChatStrength));
                return;
            }

            if (IsPortraitLike(img.transform)) return;
            if (IsHudGroup12345(img.transform)) return;
            if (IsExcluded(img.transform, _excludeTokensCache)) return;
            if (s.SkipSmallImages && IsSmall(img.rectTransform, s.SmallImageMaxSize)) return;

            int imgId = img.GetInstanceID();
            if (!_originalImageColors.ContainsKey(imgId))
            {
                _originalImageColors[imgId] = img.color;
                _images.Add(img);
            }

            img.color = BlendMultiply(_originalImageColors[imgId], s.TintColor, Clamp01(s.Strength));
        }

        public static void ApplyToSingle(RawImage r)
        {
            if (r == null) return;

            var s = Main.Settings;
            if (s == null || !s.Enabled) return;

            EnsureExcludeCache();

            // Chat special
            if (IsChatPanel(r.transform))
            {
                if (!s.EnableChatTint) return;

                int id = r.GetInstanceID();
                if (!_originalRawImageColors.ContainsKey(id))
                {
                    _originalRawImageColors[id] = r.color;
                    _rawImages.Add(r);
                }

                r.color = BlendMultiply(_originalRawImageColors[id], s.ChatTintColor, Clamp01(s.ChatStrength));
                return;
            }

            if (IsPortraitLike(r.transform)) return;
            if (IsHudGroup12345(r.transform)) return;
            if (IsExcluded(r.transform, _excludeTokensCache)) return;
            if (s.SkipSmallImages && IsSmall(r.rectTransform, s.SmallImageMaxSize)) return;

            int rawId = r.GetInstanceID();
            if (!_originalRawImageColors.ContainsKey(rawId))
            {
                _originalRawImageColors[rawId] = r.color;
                _rawImages.Add(r);
            }

            r.color = BlendMultiply(_originalRawImageColors[rawId], s.TintColor, Clamp01(s.Strength));
        }

        public static void ReapplyAll()
        {
            RestoreAll();
            ApplyToAllCanvases();
        }

        // Used by UIElementPatches (Graphic.set_color). Preserves alpha.
        internal static Color TintIncoming(Color original)
        {
            var s = Main.Settings;
            if (s == null) return original;

            float strength = Clamp01(s.Strength);
            var tint = s.TintColor;

            var mul = new Color(
                original.r * tint.r,
                original.g * tint.g,
                original.b * tint.b,
                original.a
            );

            var res = Color.Lerp(original, mul, strength);
            res.a = original.a;
            return res;
        }

        internal static bool IsExcludedForPatch(Transform t)
        {
            EnsureExcludeCache();

            // Don’t let patches tint HUD groups or portrait
            if (IsPortraitLike(t)) return true;
            if (IsHudGroup12345(t)) return true;

            // Let chat be excluded here (we tint chat via ApplyToRoot/Single path)
            if (IsChatPanel(t)) return true;

            return IsExcluded(t, _excludeTokensCache);
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

                    t.color = TintTextIncoming(t.color);
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
            var ink = s.TextTintColor;
            ink.a = original.a;

            return Color.Lerp(original, ink, strength);
        }

        internal static bool IsExcludedForText(Transform t)
        {
            EnsureExcludeCache();

            if (IsPortraitLike(t)) return true;
            if (IsHudGroup12345(t)) return true;
            if (IsChatPanel(t)) return true;

            return IsExcluded(t, _excludeTokensCache);
        }

        // ===================== HUD HIDING =====================

        // called from UMMMenu when toggles change
        public static void ApplyHudArtVisibilityNow()
        {
            SuppressPatches = true;
            try
            {
                EnsureExcludeCache();
                ApplyHudVisibilityInternal();
            }
            finally
            {
                SuppressPatches = false;
            }
        }

        private static void ApplyHudVisibilityInternal()
        {
            var s = Main.Settings;
            if (s == null) return;

            // -------------------- Images --------------------
            var images = UnityEngine.Object.FindObjectsOfType<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;

                // Never hide portrait via these toggles
                if (IsPortraitLike(img.transform))
                {
                    RestoreEnabledIfTracked(img);
                    continue;
                }

                bool is1 = IsLeftCastBars(img.transform);
                bool is2 = IsActionBarStrip(img.transform);
                bool is3 = IsCenterHudPlate(img.transform);
                bool is4 = IsRightHudButtons(img.transform);
                bool is5 = IsDialoguePanel(img.transform);

                if (!is1 && !is2 && !is3 && !is4 && !is5)
                {
                    RestoreEnabledIfTracked(img);
                    continue;
                }

                bool hide =
                    (s.HideLeftAndActionBar && (is1 || is2)) ||
                    (s.HidePortraitAndRightButtons && (is3 || is4)) ||
                    (s.HideDialoguePanel && is5);

                if (!hide)
                {
                    RestoreEnabledIfTracked(img);
                    continue;
                }

                // For (1+2) hide *art/backplates* only; for others hide full group.
                bool requireArtFilter = (is1 || is2);

                if (requireArtFilter && !IsHudArtCandidate(img.transform, img.rectTransform))
                {
                    RestoreEnabledIfTracked(img);
                    continue;
                }

                TrackEnabled(img);
                img.enabled = false;
            }

            // -------------------- RawImages --------------------
            var raws = UnityEngine.Object.FindObjectsOfType<RawImage>(true);
            for (int i = 0; i < raws.Length; i++)
            {
                var r = raws[i];
                if (r == null) continue;

                if (IsPortraitLike(r.transform))
                {
                    RestoreEnabledIfTracked(r);
                    continue;
                }

                bool is1 = IsLeftCastBars(r.transform);
                bool is2 = IsActionBarStrip(r.transform);
                bool is3 = IsCenterHudPlate(r.transform);
                bool is4 = IsRightHudButtons(r.transform);
                bool is5 = IsDialoguePanel(r.transform);

                if (!is1 && !is2 && !is3 && !is4 && !is5)
                {
                    RestoreEnabledIfTracked(r);
                    continue;
                }

                bool hide =
                    (s.HideLeftAndActionBar && (is1 || is2)) ||
                    (s.HidePortraitAndRightButtons && (is3 || is4)) ||
                    (s.HideDialoguePanel && is5);

                if (!hide)
                {
                    RestoreEnabledIfTracked(r);
                    continue;
                }

                bool requireArtFilter = (is1 || is2);

                if (requireArtFilter && !IsHudArtCandidate(r.transform, r.rectTransform))
                {
                    RestoreEnabledIfTracked(r);
                    continue;
                }

                TrackEnabled(r);
                r.enabled = false;
            }
        }

        private static void TrackEnabled(Image img)
        {
            int id = img.GetInstanceID();
            if (!_originalImageEnabled.ContainsKey(id))
                _originalImageEnabled[id] = img.enabled;

            if (!_originalImageColors.ContainsKey(id))
            {
                _originalImageColors[id] = img.color;
                _images.Add(img);
            }
        }

        private static void TrackEnabled(RawImage r)
        {
            int id = r.GetInstanceID();
            if (!_originalRawImageEnabled.ContainsKey(id))
                _originalRawImageEnabled[id] = r.enabled;

            if (!_originalRawImageColors.ContainsKey(id))
            {
                _originalRawImageColors[id] = r.color;
                _rawImages.Add(r);
            }
        }

        private static void RestoreEnabledIfTracked(Image img)
        {
            int id = img.GetInstanceID();
            if (_originalImageEnabled.TryGetValue(id, out var en))
                img.enabled = en;
        }

        private static void RestoreEnabledIfTracked(RawImage r)
        {
            int id = r.GetInstanceID();
            if (_originalRawImageEnabled.TryGetValue(id, out var en))
                r.enabled = en;
        }

        // Heuristic: hide big “panel art”, not small icons/buttons
        private static bool IsHudArtCandidate(Transform t, RectTransform rt)
        {
            if (rt == null) return false;

            var rect = rt.rect;
            float w = rect.width;
            float h = rect.height;

            if (w <= 90f && h <= 90f) return false;
            if (w <= 8f || h <= 8f) return false;

            string name = t != null ? (t.name ?? "") : "";
            string lower = name.ToLowerInvariant();

            if (lower.Contains("icon") ||
                lower.Contains("button") ||
                lower.Contains("slot") ||
                lower.Contains("ability") ||
                lower.Contains("spell") ||
                lower.Contains("skill"))
                return false;

            if (lower.Contains("bg") ||
                lower.Contains("background") ||
                lower.Contains("back") ||
                lower.Contains("paper") ||
                lower.Contains("parchment") ||
                lower.Contains("frame") ||
                lower.Contains("decor") ||
                lower.Contains("art"))
                return true;

            float area = w * h;
            if (area >= 12000f && (w >= 160f || h >= 160f))
                return true;

            return false;
        }

        // ===================== Group Detection (1..5) =====================

        private static bool IsLeftCastBars(Transform t)
        {
            return NameHasAny(t, 18,
                "groups", "group", "unitgroup", "grouppanel",
                "castbar", "leftbar", "leftpanel");
        }

        private static bool IsActionBarStrip(Transform t)
        {
            return NameHasAny(t, 18,
                "actionbar", "abilitybar", "hotbar", "spellbar",
                "commandbar", "quickbar", "skillbar", "itembar");
        }

        // (3) = the big center HUD plate area near the portrait (NOT the portrait itself)
        private static bool IsCenterHudPlate(Transform t)
        {
            return NameHasAny(t, 18,
                "center", "mainpanel", "centerpanel", "hud", "uipanel",
                "unitpanel", "characterpanel", "partybar", "mainbar");
        }

        private static bool IsRightHudButtons(Transform t)
        {
            return NameHasAny(t, 18,
                "sidebuttons", "rightbuttons", "utilitybuttons",
                "minimapbuttons", "rightpanel", "hudbuttons");
        }

        private static bool IsDialoguePanel(Transform t)
        {
            return NameHasAny(t, 18,
                "dialog", "dialogue", "conversation",
                "chat", "combatlog", "log", "history");
        }

        private static bool IsChatPanel(Transform t)
        {
            // same as dialogue panel on purpose (chat/info window)
            return IsDialoguePanel(t);
        }

        private static bool IsHudGroup12345(Transform t)
        {
            return IsLeftCastBars(t) ||
                   IsActionBarStrip(t) ||
                   IsCenterHudPlate(t) ||
                   IsRightHudButtons(t) ||
                   IsDialoguePanel(t);
        }

        private static bool IsPortraitLike(Transform t)
        {
            if (t == null) return false;

            for (int depth = 0; depth < 18 && t != null; depth++, t = t.parent)
            {
                var n = t.name;
                if (string.IsNullOrEmpty(n)) continue;

                var lower = n.ToLowerInvariant();
                if (lower.Contains("portrait") ||
                    lower.Contains("portraits") ||
                    lower.Contains("charportrait") ||
                    lower.Contains("unitportrait"))
                    return true;
            }

            return false;
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
            var key = (s.ExcludeChatLike ? "1" : "0") + "|" + (s.ExtraExcludeTokens ?? "");
            if (!string.Equals(_excludeTokensKey, key, StringComparison.Ordinal))
            {
                _excludeTokensKey = key;
                _excludeTokensCache = BuildExcludeTokens(s.ExcludeChatLike, s.ExtraExcludeTokens);
            }
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

        private static string[] BuildExcludeTokens(bool excludeChat, string extraTokensCsv)
        {
            var list = new List<string>(16);

            if (excludeChat)
            {
                list.Add("chat");
                list.Add("message");
                list.Add("history");
                list.Add("log");
                list.Add("combatlog");
            }

            if (!string.IsNullOrEmpty(extraTokensCsv))
            {
                var parts = extraTokensCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var tok = parts[i].Trim();
                    if (tok.Length > 0)
                        list.Add(tok.ToLowerInvariant());
                }
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
    }
}
