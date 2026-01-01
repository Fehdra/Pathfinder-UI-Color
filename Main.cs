// DarkParchmentUI/Main.cs


using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    public static class Main
    {
        internal static UnityModManager.ModEntry ModEntry { get; private set; }
        internal static Logger Logger { get; private set; }
        internal static Settings Settings { get; private set; }

        private static Harmony _harmony;
        private static bool _patched;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Logger = new Logger(modEntry.Logger);

            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnGUI = UMMMenu.OnGUI;
            modEntry.OnToggle = OnToggle;

            // If already enabled at startup, OnToggle may not fire
            if (modEntry.Enabled)
            {
                EnsurePatched(modEntry);
                UIThemeController.Enable();
            }

            return true;
        }


        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                EnsurePatched(modEntry);
                UIThemeController.Enable();
            }
            else
            {
                // Stop coroutines, restore visuals, clear caches
                UIThemeController.Disable();

                // Prevent stacked patches if the mod is toggled repeatedly
                if (_harmony != null)
                {
                    _harmony.UnpatchAll(modEntry.Info.Id);
                    _harmony = null;
                }

                _patched = false;
            }
            return true;
        }

        private static void EnsurePatched(UnityModManager.ModEntry modEntry)
        {
            if (_patched) return;

            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            _patched = true;
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
}
