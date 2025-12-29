// DarkParchmentUI/Runner.cs
// C# 7.3 compatible

using System.Collections;
using UnityEngine;

namespace DarkParchmentUI
{
    internal sealed class Runner : MonoBehaviour
    {
        internal static Runner Instance { get; private set; }

        internal static void Ensure()
        {
            if (Instance != null) return;

            var go = new GameObject("DarkParchmentUI.Runner");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            Instance = go.AddComponent<Runner>();
        }

        internal static void DestroyRunner()
        {
            if (Instance == null) return;
            try { Destroy(Instance.gameObject); }
            catch { /* ignore */ }
            Instance = null;
        }

        internal void Run(IEnumerator routine)
        {
            if (routine == null) return;
            StartCoroutine(routine);
        }
    }
}
