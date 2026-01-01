// DarkParchmentUI/TextPatches.cs


using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DarkParchmentUI
{
    [HarmonyPatch]
    internal static class TextPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TMP_Text), "set_color")]
        private static void TMPText_set_color_Prefix(TMP_Text __instance, ref Color value)
        {
            if (__instance == null) return;
            if (ThemeApplier.SuppressPatches) return;

            var s = Main.Settings;
            if (s == null || !s.EnableTextTint) return;

            if (ThemeApplier.IsExcludedForText(__instance.transform)) return;

            // Skip already-colored emphasis text (less aggressive threshold)
            if (s.SkipRichTextColoredSegments)
            {
                float max = Mathf.Max(value.r, Mathf.Max(value.g, value.b));
                float min = Mathf.Min(value.r, Mathf.Min(value.g, value.b));
                if ((max - min) > 0.35f) return;
            }

            var a = value.a;
            ThemeApplier.SetCurrentTextTransform(__instance.transform);
            value = ThemeApplier.TintTextIncoming(value);
            ThemeApplier.ClearCurrentTextTransform();
            value.a = a; // preserve alpha
        }
    }
}
