using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkParchmentUI
{
    internal static class UIThemeController
    {
        private static bool _enabled;
        private static int _seq;

        public static void Enable()
        {
            if (_enabled) return;
            _enabled = true;

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Initial pass for current scene
            QueueFullApply(0.35f);
            QueueFullApply(1.00f);
        }

        public static void Disable()
        {
            if (!_enabled) return;
            _enabled = false;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Two passes: early + late (late catches UI that spawns after load)
            QueueFullApply(0.35f);
            QueueFullApply(1.00f);
        }

        private static void QueueFullApply(float delay)
        {
            Runner.Ensure();
            _seq++;
            int seq = _seq;

            Runner.Instance.Run(DelayThen(delay, () =>
            {
                if (!_enabled) return;
                if (seq != _seq) return;

                ThemeApplier.ReapplyAll();
                ThemeApplier.ApplyHudVisibilityNow();

                if (Main.Settings != null && Main.Settings.EnableTextTint)
                    ThemeApplier.ApplyTextToAll();
            }));
        }

        private static IEnumerator DelayThen(float delay, System.Action a)
        {
            yield return new WaitForSeconds(delay);
            a?.Invoke();
        }
    }
}
