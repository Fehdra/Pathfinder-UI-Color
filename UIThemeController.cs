// DarkParchmentUI/UIThemeController.cs
// C# 7.3 compatible

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkParchmentUI
{
    internal static class UIThemeController
    {
        private static bool _enabled;

        public static void Enable()
        {
            if (_enabled) return;
            _enabled = true;

            Runner.Ensure();
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Initial burst (enable mid-session or first load)
            Runner.Instance.Run(DelayedApply());
        }

        public static void Disable()
        {
            if (!_enabled) return;
            _enabled = false;

            SceneManager.sceneLoaded -= OnSceneLoaded;

            ThemeApplier.RestoreAll();
            ThemeApplier.ClearAllTracking();

            Runner.DestroyRunner();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_enabled) return;

            // UI often rebuilds after scene load
            Runner.Instance.Run(DelayedApply());
        }

        private static IEnumerator DelayedApply()
        {
            // Try a few times to catch UI that builds late (main menu, HUD, etc.)
            for (int attempt = 0; attempt < 8; attempt++)
            {
                yield return new WaitForSeconds(0.25f);
                if (!_enabled) yield break;

                try
                {
                    ThemeApplier.ApplyToAllCanvases();
                    ThemeApplier.ApplyTextToAll();
                }
                catch (System.Exception e)
                {
                    Main.Logger.Error(e);
                    yield break;
                }
            }
        }
    }
}
