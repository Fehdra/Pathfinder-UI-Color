// DarkParchmentUI/UIElementPatches.cs


using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DarkParchmentUI
{
    [HarmonyPatch]
    internal static class UIElementPatches
    {
        // Many games drive UI visibility by setting color/alpha repeatedly.
        // Patch Graphic.color, but only act on Image/RawImage to avoid intercepting Text/etc.

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Graphic), "set_color")]
        private static void Graphic_set_color_Prefix(Graphic __instance, ref Color value)
        {
            if (__instance == null) return;
            if (ThemeApplier.SuppressPatches) return;

            // Image inherits Graphic but doesn't override the color setter, so we patch Graphic.set_color
            // and then ignore everything except Image/RawImage.
            if (!(__instance is Image) && !(__instance is RawImage)) return;

            var s = Main.Settings;
            if (s == null || !s.Enabled) return;
            if (s.Strength <= 0.0001f) return;

            // Respect excludes (HUD groups, chat/log, and user tokens)
            if (ThemeApplier.IsExcludedForPatch(__instance.transform))
                return;

            // Optional: skip tiny images (icons) to reduce muddy UI and per-frame patch work
            if (s.SkipSmallImages)
            {
                var rt = __instance.rectTransform;
                if (rt != null)
                {
                    var rect = rt.rect;
                    if (rect.width <= s.SmallImageMaxSize && rect.height <= s.SmallImageMaxSize)
                        return;
                }
            }

            // Preserve original alpha so things don't "disappear"
            value = ThemeApplier.TintIncomingStable(__instance, value);

        }

    }
}
