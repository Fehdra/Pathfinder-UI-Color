// DarkParchmentUI/Main.cs
//
// Goals:
// - Keep older settings (text presets/custom RGB) + add separate popup/tooltip/chat background controls
// - Avoid nameplate/worldspace flicker (skip overhead/nameplates; allow large worldspace "paper" tooltips)
// - Avoid lag: NO periodic full ApplyTheme() scans
//   * Full apply only on Apply/settings change
//   * Discovery caches canvases and rebuilds caches when transform count changes (cheap-ish, throttled)
// - Popups/tooltips/chat can be tinted differently from main pages
// - Debug option logs which UI elements are classified MAIN / POPUP / CHAT on Apply

using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    public enum BackgroundPreset { Warm, Neutral, Cold, Custom }
    public enum TextPreset { Neutral, Warm, Cool, Custom }

    // ================= SETTINGS =================
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("===== MAIN UI =====")]
        public bool _mainHeader;

        [Draw("DEBUG: Log tinted backgrounds on Apply")]
        public bool debugLogOnApply = false;

        [Draw("Enable background tint")]
        public bool enableBackground = true;

        [Draw("Darkness (0 = default, 1 = darkest)", Min = 0f, Max = 1f)]
        public float darkness = 0.35f;

        [Draw("Background preset")]
        public BackgroundPreset preset = BackgroundPreset.Warm;

        [Draw("Custom target R (0..1)", Min = 0f, Max = 1f)]
        public float customR = 0.12f;

        [Draw("Custom target G (0..1)", Min = 0f, Max = 1f)]
        public float customG = 0.10f;

        [Draw("Custom target B (0..1)", Min = 0f, Max = 1f)]
        public float customB = 0.09f;

        [Draw("Warmth (-1 = cooler, +1 = warmer)", Min = -1f, Max = 1f)]
        public float warmth = 0.0f;

        // ===== Text options =====
        [Draw("===== TEXT =====")]
        public bool _textHeader;

        [Draw("Enable text boost (brightness)")]
        public bool enableTextBoost = true;

        [Draw("Text boost (0 = off, 1 = max)", Min = 0f, Max = 1f)]
        public float textBoost = 0.30f;

        [Draw("Enable text tint (color shift)")]
        public bool enableTextTint = true;

        [Draw("Text color preset")]
        public TextPreset textPreset = TextPreset.Neutral;

        [Draw("Text tint strength (0 = off, 1 = strong)", Min = 0f, Max = 1f)]
        public float textTint = 0.20f;

        [Draw("Tint ALL text (may affect rarity colors)")]
        public bool tintAllText = false;

        [Draw("Custom text R (0..1)", Min = 0f, Max = 1f)]
        public float textR = 0.95f;

        [Draw("Custom text G (0..1)", Min = 0f, Max = 1f)]
        public float textG = 0.95f;

        [Draw("Custom text B (0..1)", Min = 0f, Max = 1f)]
        public float textB = 0.98f;

        // ===== Popup/Tooltip/Chat options =====
        [Draw("===== POPUPS / TOOLTIPS / CHAT =====")]
        public bool _popupHeader;

        [Draw("Enable popup/tooltip/chat tint")]
        public bool enablePopupBackground = true;

        [Draw("Popup/tooltip/chat darkness", Min = 0f, Max = 1f)]
        public float popupDarkness = 0.55f;

        [Draw("Popup/tooltip/chat background preset")]
        public BackgroundPreset popupPreset = BackgroundPreset.Neutral;

        [Draw("Popup/tooltip/chat warmth", Min = -1f, Max = 1f)]
        public float popupWarmth = 0.0f;

        [Draw("Popup custom R", Min = 0f, Max = 1f)]
        public float popupCustomR = 0.18f;

        [Draw("Popup custom G", Min = 0f, Max = 1f)]
        public float popupCustomG = 0.16f;

        [Draw("Popup custom B", Min = 0f, Max = 1f)]
        public float popupCustomB = 0.14f;

        // ===== Performance =====
        [Draw("===== PERFORMANCE =====")]
        public bool _perfHeader;

        [Draw("Force TMP redraw on apply")]
        public bool forceTMPRefresh = true;

        [Draw("Discovery interval (seconds)", Min = 0.5f, Max = 10f)]
        public float discoveryInterval = 2.5f;

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
        public void OnChange() => Main.RequestApply(clearCaches: true);
    }

    // ================= MAIN =================
    public static class Main
    {
        public static UnityModManager.ModEntry Mod;
        public static Settings Settings;

        static Harmony _harmony;
        static bool _applyRequested;
        static bool _forceRedrawThisPass;
        static bool _logApplyThisPass;
        // recursion guard for TMP patch
        static bool IsInternalChanging;

        static bool HasTooltipishComponent(GameObject go)
        {
            if (go == null) return false;

            // Walk up parents and inspect MonoBehaviours by type name
            Transform t = go.transform;
            for (int depth = 0; depth < 10 && t != null; depth++)
            {
                var mbs = t.GetComponents<MonoBehaviour>();
                for (int i = 0; i < mbs.Length; i++)
                {
                    var mb = mbs[i];
                    if (mb == null) continue;

                    string tn = mb.GetType().Name;
                    if (tn.IndexOf("Tooltip", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (tn.IndexOf("Hint", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (tn.IndexOf("Inspect", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (tn.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }

                t = t.parent;
            }

            return false;
        }


        // ================= DEBUG HELPERS =================
        static string GetPath(Transform t)
        {
            if (t == null) return "<null>";
            var parts = new List<string>(12);
            while (t != null && parts.Count < 12)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        static void DebugLogBg(UnityModManager.ModEntry.ModLogger logger, string kind, GameObject go, bool isPopup, bool isChat)
        {
            if (logger == null || go == null) return;
            string path = GetPath(go.transform);
            logger.Log($"[DPUI] {kind} => {(isChat ? "CHAT" : (isPopup ? "POPUP" : "MAIN"))} :: {path}");
        }

        // ================= HINTS =================
        static readonly string[] BackgroundHints =
        {
            "background","bg","back","paper","parchment","sheet",
            "panel","window","frame","backdrop","overlay","plate",
            "scroll","viewport","content","list"
        };

        // Keep flicker offenders excluded.
        // NOTE: don't include "tooltip/hint/bark/chat" here; we WANT to tint those.
        static readonly string[] ExcludedHierarchyHints =
        {
            "portrait", "doll", "ragdoll", "character",
            "console",
            "overhead", "nameplate", "unitname", "unit_name",
            "map", "globalmap", "citybuilder",
            "cursor", "drag",
            "minimap"
        };

        // Popup/tooltips (spell sheets, item tooltips, hover popups)
        static readonly string[] PopupHints =
        {
            "tooltip", "itemtooltip", "spelltooltip",
            "hint", "hover",
            "context", "popup", "floating",
            "inspection", "inspect", "description"
        };

        // Chat/combat log/dialog text box (bottom-right)
        static readonly string[] ChatHints =
        {
            "chat", "combat", "combatlog", "log",
            "dialog", "dialogue", "conversation",
            "bark", "subtitle", "message"
        };

        static bool IsPopupLike(GameObject go)
        {
            if (go == null) return false;

            // NEW: component-based detection catches Owlcat tooltip views even if names are generic
            if (HasTooltipishComponent(go)) return true;

            Transform t = go.transform;
            for (int depth = 0; depth < 10 && t != null; depth++)
            {
                string n = t.name ?? "";
                for (int i = 0; i < PopupHints.Length; i++)
                    if (n.IndexOf(PopupHints[i], StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                t = t.parent;
            }
            return false;
        }
        static bool IsChatLike(GameObject go)
        {
            if (go == null) return false;

            Transform t = go.transform;
            for (int depth = 0; depth < 12 && t != null; depth++)
            {
                string n = t.name ?? "";
                for (int i = 0; i < ChatHints.Length; i++)
                    if (n.IndexOf(ChatHints[i], StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                t = t.parent;
            }
            return false;
        }


        // ================= CACHES =================
        static readonly Dictionary<int, Color> OriginalImageColors = new Dictionary<int, Color>();
        static readonly Dictionary<int, Color> OriginalRawImageColors = new Dictionary<int, Color>();
        static readonly Dictionary<int, Color> OriginalTMPColors = new Dictionary<int, Color>();
        static readonly Dictionary<int, Color> GameBaseTMPColor = new Dictionary<int, Color>();

        class CanvasCache
        {
            public readonly List<Image> bgImages = new List<Image>(256);
            public readonly List<RawImage> bgRawImages = new List<RawImage>(64);
            public readonly List<TMP_Text> tmpTexts = new List<TMP_Text>(512);
            public int lastNodeCount; // total transforms (catches nested tooltip spawns)
        }

        static readonly Dictionary<int, CanvasCache> CanvasCaches = new Dictionary<int, CanvasCache>();
        static readonly List<Canvas> CanvasBuf = new List<Canvas>(64);

        static float _nextDiscoveryTime;

        // ================= LOAD =================
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            modEntry.OnGUI = (_) =>
            {
                Settings.Draw(_);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply"))
                    RequestApply(clearCaches: false);

                if (GUILayout.Button("Clear caches + Apply"))
                    RequestApply(clearCaches: true);
                GUILayout.EndHorizontal();
            };

            modEntry.OnSaveGUI = (_) => Settings.Save(modEntry);

            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll();

            Mod.Logger.Log("DarkParchmentUI loaded.");
            return true;
        }

        static void ClearAllCaches()
        {
            OriginalImageColors.Clear();
            OriginalRawImageColors.Clear();
            OriginalTMPColors.Clear();
            GameBaseTMPColor.Clear();
            CanvasCaches.Clear();
        }

        public static void RequestApply(bool clearCaches)
        {
            if (clearCaches) ClearAllCaches();
            _applyRequested = true;
        }

        // ================= APPLY LOOP =================
        [HarmonyPatch(typeof(Canvas), "SendWillRenderCanvases")]
        static class Patch_SendWillRenderCanvases
        {
            static void Prefix()
            {
                if (Settings == null) return;

                if (_applyRequested)
                {
                    _applyRequested = false;
                    _forceRedrawThisPass = Settings.forceTMPRefresh;
                    _logApplyThisPass = Settings.debugLogOnApply;

                    DiscoverCanvases(force: true);
                    ApplyAllCached(includeText: true);

                    _forceRedrawThisPass = false;
                    _logApplyThisPass = false;
                    return;
                }

                // cheap discovery only
                DiscoverCanvases(force: false);
            }
        }

        // Reactive TMP patch: reduces hover flash without forcing heavy UI scans
        [HarmonyPatch(typeof(TMP_Text), "color", MethodType.Setter)]
        static class Patch_TMP_Text_SetColor
        {
            static void Postfix(TMP_Text __instance)
            {
                if (Settings == null || IsInternalChanging) return;
                if (__instance == null || !__instance.isActiveAndEnabled) return;
                if (ShouldNeverTouch(__instance.gameObject)) return;
                if (!Settings.enableTextBoost && !Settings.enableTextTint) return;

                var canvas = __instance.canvas;
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && !IsSafeWorldspaceCanvas(canvas))
                    return;

                ApplyToSingleTMP(__instance, isReactiveUpdate: true);
            }
        }

        // Allow SOME worldspace canvases (paper tooltips) but not overhead/nameplates
        static bool IsSafeWorldspaceCanvas(Canvas c)
        {
            if (c == null) return false;

            // If it looks like popup/tooltip by name chain, allow.
            if (IsPopupLike(c.gameObject) || IsChatLike(c.gameObject)) return true;

            var rt = c.GetComponent<RectTransform>();
            if (rt != null)
            {
                float w = Mathf.Abs(rt.rect.width);
                float h = Mathf.Abs(rt.rect.height);

                // original large "sheet" allowance
                if (w > 700f && h > 450f) return true;

                // NEW: allow moderate popup-like worldspace (kept tied to popup detection above)
                if (w > 260f && h > 160f && IsPopupLike(c.gameObject)) return true;
            }

            return false;
        }

        static void DiscoverCanvases(bool force)
        {
            float interval = Mathf.Clamp(Settings.discoveryInterval, 0.5f, 10f);
            if (!force && Time.unscaledTime < _nextDiscoveryTime) return;
            _nextDiscoveryTime = Time.unscaledTime + interval;

            CanvasBuf.Clear();
            CanvasBuf.AddRange(UnityEngine.Object.FindObjectsOfType<Canvas>());

            for (int i = 0; i < CanvasBuf.Count; i++)
            {
                var c = CanvasBuf[i];
                if (c == null) continue;
                if (ShouldNeverTouch(c.gameObject)) continue;

                // worldspace: allow only safe ones
                if (c.renderMode == RenderMode.WorldSpace && !IsSafeWorldspaceCanvas(c))
                    continue;

                int id = c.GetInstanceID();

                if (!CanvasCaches.TryGetValue(id, out var cache))
                {
                    cache = new CanvasCache();
                    BuildCanvasCache(c, cache);
                    CanvasCaches[id] = cache;
                    continue;
                }

                int nodeCount = c.GetComponentsInChildren<Transform>(true).Length;
                if (nodeCount != cache.lastNodeCount)
                    BuildCanvasCache(c, cache);
            }
        }

        static void BuildCanvasCache(Canvas c, CanvasCache cache)
        {
            cache.bgImages.Clear();
            cache.bgRawImages.Clear();
            cache.tmpTexts.Clear();
            cache.lastNodeCount = c.GetComponentsInChildren<Transform>(true).Length;

            var root = c.gameObject;

            // ===== Images =====
            var imgs = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < imgs.Length; i++)
            {
                var img = imgs[i];
                if (img == null) continue;
                if (ShouldNeverTouch(img.gameObject)) continue;
                if (!LooksLikeBackground(img)) continue;

                cache.bgImages.Add(img);

                int iid = img.GetInstanceID();
                if (!OriginalImageColors.ContainsKey(iid))
                    OriginalImageColors[iid] = img.color;
            }

            // ===== RawImages =====
            var raws = root.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < raws.Length; i++)
            {
                var raw = raws[i];
                if (raw == null) continue;
                if (ShouldNeverTouch(raw.gameObject)) continue;

                var rt = raw.rectTransform;
                float w = Mathf.Abs(rt.rect.width);
                float h = Mathf.Abs(rt.rect.height);

                // NEW: allow popup/chat raw images regardless of size
                bool isPopupOrChat = IsPopupLike(raw.gameObject) || IsChatLike(raw.gameObject);
                if (!isPopupOrChat && (w < 360f && h < 220f)) continue;

                cache.bgRawImages.Add(raw);

                int rid = raw.GetInstanceID();
                if (!OriginalRawImageColors.ContainsKey(rid))
                    OriginalRawImageColors[rid] = raw.color;
            }

            // ===== TMP Text =====
            var tmps = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                var tmp = tmps[i];
                if (tmp == null) continue;
                if (ShouldNeverTouch(tmp.gameObject)) continue;

                // NEW: allow smaller fonts for popup/chat
                bool popupOrChatText = IsPopupLike(tmp.gameObject) || IsChatLike(tmp.gameObject);
                if (!popupOrChatText && tmp.fontSize < 12) continue;
                if (popupOrChatText && tmp.fontSize < 9) continue;

                var cv = tmp.canvas;
                if (cv != null && cv.renderMode == RenderMode.WorldSpace && !IsSafeWorldspaceCanvas(cv))
                    continue;

                cache.tmpTexts.Add(tmp);

                int tid = tmp.GetInstanceID();
                if (!OriginalTMPColors.ContainsKey(tid))
                    OriginalTMPColors[tid] = tmp.color;
            }
        }

        static void ApplyAllCached(bool includeText)
        {
            float mainT = Mathf.Clamp01(Settings.darkness);
            float popupT = Mathf.Clamp01(Settings.popupDarkness);

            Color mainBg = GetTargetBackgroundColor();
            Color popupBg = GetTargetPopupBackgroundColor();

            foreach (var kv in CanvasCaches)
            {
                var cache = kv.Value;

                if (Settings.enableBackground || Settings.enablePopupBackground || Settings.debugLogOnApply)
                {
                    // ========== Images ==========
                    for (int i = 0; i < cache.bgImages.Count; i++)
                    {
                        var img = cache.bgImages[i];
                        if (img == null) continue;

                        bool isPopup = IsPopupLike(img.gameObject);
                        bool isChat = IsChatLike(img.gameObject);
                        bool treatAsPopup = isPopup || isChat;

                        if (_logApplyThisPass && Settings.debugLogOnApply)
                            DebugLogBg(Mod.Logger, "ImageBG", img.gameObject, isPopup, isChat);


                        if (treatAsPopup && !Settings.enablePopupBackground) continue;
                        if (!treatAsPopup && !Settings.enableBackground) continue;

                        float t = treatAsPopup ? popupT : mainT;
                        Color targetBg = treatAsPopup ? popupBg : mainBg;

                        int id = img.GetInstanceID();
                        if (!OriginalImageColors.TryGetValue(id, out var orig)) continue;

                        Color baseColor = Desaturate(orig, 0.25f);
                        Color target = new Color(targetBg.r, targetBg.g, targetBg.b, baseColor.a);

                        img.color = Color.Lerp(baseColor, target, t);
                    }

                    // ========== RawImages ==========
                    for (int i = 0; i < cache.bgRawImages.Count; i++)
                    {
                        var raw = cache.bgRawImages[i];
                        if (raw == null) continue;

                        bool isPopup = IsPopupLike(raw.gameObject);
                        bool isChat = IsChatLike(raw.gameObject);
                        bool treatAsPopup = isPopup || isChat;

                        if (_logApplyThisPass && Settings.debugLogOnApply)
                            DebugLogBg(Mod.Logger, "RawBG", raw.gameObject, isPopup, isChat);


                        if (treatAsPopup && !Settings.enablePopupBackground) continue;
                        if (!treatAsPopup && !Settings.enableBackground) continue;

                        float t = treatAsPopup ? popupT : mainT;
                        Color targetBg = treatAsPopup ? popupBg : mainBg;

                        int id = raw.GetInstanceID();
                        if (!OriginalRawImageColors.TryGetValue(id, out var orig)) continue;

                        Color baseColor = Desaturate(orig, 0.25f);
                        Color target = new Color(targetBg.r, targetBg.g, targetBg.b, baseColor.a);

                        raw.color = Color.Lerp(baseColor, target, t);
                    }
                }

                if (!includeText) continue;

                // ========== Text ==========
                for (int i = 0; i < cache.tmpTexts.Count; i++)
                {
                    var tmp = cache.tmpTexts[i];
                    if (tmp == null) continue;

                    ApplyToSingleTMP(tmp, isReactiveUpdate: false);

                    if (_forceRedrawThisPass)
                    {
                        tmp.havePropertiesChanged = true;
                        tmp.SetVerticesDirty();
                    }
                }
            }
        }

        static void ApplyToSingleTMP(TMP_Text tmp, bool isReactiveUpdate)
        {
            if (IsInternalChanging) return;

            if (tmp == null || Settings == null) return;
            if (!tmp.isActiveAndEnabled) return;
            if (ShouldNeverTouch(tmp.gameObject)) return;

            // NEW: allow smaller fonts for popup/chat
            bool popupOrChatText = IsPopupLike(tmp.gameObject) || IsChatLike(tmp.gameObject);
            if (!popupOrChatText && tmp.fontSize < 12) return;
            if (popupOrChatText && tmp.fontSize < 9) return;

            var canvas = tmp.canvas;
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && !IsSafeWorldspaceCanvas(canvas))
                return;

            float tb = (Settings.enableTextBoost ? Mathf.Clamp01(Settings.textBoost) : 0f);
            float tt = (Settings.enableTextTint ? Mathf.Clamp01(Settings.textTint) : 0f);
            if (tb <= 0.001f && tt <= 0.001f) return;

            int id = tmp.GetInstanceID();
            Color current = tmp.color;

            if (!OriginalTMPColors.ContainsKey(id))
                OriginalTMPColors[id] = current;

            // Ignore hover-white overwrites when updating base
            if (isReactiveUpdate)
            {
                if (!LooksLikeHighlightWhite(current))
                    GameBaseTMPColor[id] = current;
            }
            else
            {
                if (!GameBaseTMPColor.ContainsKey(id))
                    GameBaseTMPColor[id] = OriginalTMPColors[id];
            }

            Color baseText = GameBaseTMPColor.TryGetValue(id, out var bc) ? bc : OriginalTMPColors[id];
            Color c = baseText;

            if (tb > 0.001f)
                c = BoostText(c, tb);

            if (tt > 0.001f)
            {
                Color tint = GetTargetTextTint();
                bool popupOrChat = popupOrChatText;

                // NEW: popup/chat always tint (even warm-colored text)
                if (Settings.tintAllText || popupOrChat || LooksLikeCoolText(baseText) || LooksLikeStandardText(baseText))
                    c = TintText(c, tint, tt);
            }

            try
            {
                IsInternalChanging = true;
                if (!Approximately(tmp.color, c))
                    tmp.color = c;
            }
            finally
            {
                IsInternalChanging = false;
            }
        }

        // ================= DETECTORS =================
        static bool ShouldNeverTouch(GameObject go)
        {
            if (go == null) return false;

            // self
            string n = go.name ?? "";
            for (int i = 0; i < ExcludedHierarchyHints.Length; i++)
                if (n.IndexOf(ExcludedHierarchyHints[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            // parents
            Transform t = go.transform.parent;
            for (int depth = 0; depth < 6 && t != null; depth++)
            {
                n = t.name ?? "";
                for (int i = 0; i < ExcludedHierarchyHints.Length; i++)
                    if (n.IndexOf(ExcludedHierarchyHints[i], StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                t = t.parent;
            }

            return false;
        }

        static bool LooksLikeBackground(Image img)
        {
            if (img == null) return false;

            // NEW: if it is popup/chat, always treat as background even if small
            if (IsPopupLike(img.gameObject) || IsChatLike(img.gameObject))
                return true;

            var rt = img.rectTransform;
            float w = Mathf.Abs(rt.rect.width);
            float h = Mathf.Abs(rt.rect.height);

            if (w < 360f && h < 220f) return false;

            string n = img.gameObject.name ?? "";
            string sn = (img.sprite != null ? img.sprite.name : "") ?? "";

            for (int i = 0; i < BackgroundHints.Length; i++)
            {
                var hint = BackgroundHints[i];
                if (n.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sn.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            if (w > 900f || h > 650f) return true;
            if ((w > 420f && h > 260f) && img.color.a > 0.12f) return true;

            return false;
        }

        static bool LooksLikeHighlightWhite(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float sat = max - min;
            return c.a > 0.8f && max > 0.92f && sat < 0.06f;
        }

        static bool LooksLikeStandardText(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float sat = max - min;
            return sat < 0.12f;
        }

        static bool LooksLikeCoolText(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float sat = max - min;
            if (sat < 0.06f) return false;
            return (c.b > c.g + 0.03f) && (c.b > 0.20f);
        }

        // ================= COLORS =================
        static Color GetTargetBackgroundColor()
        {
            Color presetColor;
            switch (Settings.preset)
            {
                case BackgroundPreset.Warm: presetColor = new Color(0.12f, 0.10f, 0.09f, 1f); break;
                case BackgroundPreset.Neutral: presetColor = new Color(0.11f, 0.11f, 0.11f, 1f); break;
                case BackgroundPreset.Cold: presetColor = new Color(0.09f, 0.10f, 0.12f, 1f); break;
                default:
                    presetColor = new Color(
                        Mathf.Clamp01(Settings.customR),
                        Mathf.Clamp01(Settings.customG),
                        Mathf.Clamp01(Settings.customB),
                        1f
                    );
                    break;
            }

            float w = Mathf.Clamp(Settings.warmth, -1f, 1f);
            float shift = 0.05f * w;

            return new Color(
                Mathf.Clamp01(presetColor.r + shift),
                Mathf.Clamp01(presetColor.g + shift * 0.25f),
                Mathf.Clamp01(presetColor.b - shift),
                1f
            );
        }

        static Color GetTargetPopupBackgroundColor()
        {
            Color presetColor;
            switch (Settings.popupPreset)
            {
                case BackgroundPreset.Warm: presetColor = new Color(0.12f, 0.10f, 0.09f, 1f); break;
                case BackgroundPreset.Neutral: presetColor = new Color(0.11f, 0.11f, 0.11f, 1f); break;
                case BackgroundPreset.Cold: presetColor = new Color(0.09f, 0.10f, 0.12f, 1f); break;
                default:
                    presetColor = new Color(
                        Mathf.Clamp01(Settings.popupCustomR),
                        Mathf.Clamp01(Settings.popupCustomG),
                        Mathf.Clamp01(Settings.popupCustomB),
                        1f
                    );
                    break;
            }

            float w = Mathf.Clamp(Settings.popupWarmth, -1f, 1f);
            float shift = 0.05f * w;

            return new Color(
                Mathf.Clamp01(presetColor.r + shift),
                Mathf.Clamp01(presetColor.g + shift * 0.25f),
                Mathf.Clamp01(presetColor.b - shift),
                1f
            );
        }

        static Color GetTargetTextTint()
        {
            switch (Settings.textPreset)
            {
                case TextPreset.Warm:
                    return new Color(1.00f, 0.92f, 0.82f, 1f); // ivory
                case TextPreset.Cool:
                    return new Color(0.86f, 0.92f, 1.00f, 1f); // ice
                case TextPreset.Custom:
                    return new Color(
                        Mathf.Clamp01(Settings.textR),
                        Mathf.Clamp01(Settings.textG),
                        Mathf.Clamp01(Settings.textB),
                        1f
                    );
                case TextPreset.Neutral:
                default:
                    return new Color(0.95f, 0.95f, 0.95f, 1f); // soft white
            }
        }

        static Color Desaturate(Color c, float amount)
        {
            float gray = (c.r + c.g + c.b) / 3f;
            return new Color(
                Mathf.Lerp(c.r, gray, amount),
                Mathf.Lerp(c.g, gray, amount),
                Mathf.Lerp(c.b, gray, amount),
                c.a
            );
        }

        static Color BoostText(Color c, float boost)
        {
            float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            if (lum > 0.80f) return c;

            return new Color(
                Mathf.Lerp(c.r, 1f, boost),
                Mathf.Lerp(c.g, 1f, boost),
                Mathf.Lerp(c.b, 1f, boost),
                c.a
            );
        }

        static Color TintText(Color original, Color tint, float strength)
        {
            Color mixed = Color.Lerp(original, new Color(tint.r, tint.g, tint.b, original.a), strength);
            return new Color(mixed.r, mixed.g, mixed.b, original.a);
        }

        static bool Approximately(Color a, Color b)
        {
            const float e = 0.002f;
            return Mathf.Abs(a.r - b.r) < e
                && Mathf.Abs(a.g - b.g) < e
                && Mathf.Abs(a.b - b.b) < e
                && Mathf.Abs(a.a - b.a) < e;
        }
    }
}
