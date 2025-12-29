// DarkParchmentUI/UIElementPatches.cs
// C# 7.3 compatible

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DarkParchmentUI
{
    [HarmonyPatch]
    internal static class UIElementPatches
    {
        // Catch UI that is shown by changing color/alpha instead of enabling/disabling.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Graphic), "set_color")]
        private static void Graphic_set_color_Prefix(Graphic __instance, ref Color value)
        {
            if (__instance == null) return;
            if (ThemeApplier.SuppressPatches) return;

            var s = Main.Settings;
            if (s == null || !s.Enabled) return;

            // Only tint background graphics, not Text (Text is also a Graphic)
            if (!(__instance is Image) && !(__instance is RawImage))
                return;

            // Respect exclude tokens (chat/log etc.)
            if (ThemeApplier.IsExcludedForPatch(__instance.transform))
                return;

            // Preserve original alpha so things don't "disappear"
            var a = value.a;
            value = ThemeApplier.TintIncoming(value);
            value.a = a;
        }
    }
}
