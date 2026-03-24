using UnityEngine;

namespace PolyBridge.Sandbox
{
    internal static class SandboxInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            try
            {
                var config = SandboxConfig.Load();
                if (config == null || !config.autoInitialize) return;

                Debug.Log("[PolyBridge Sandbox] Initializing...");
                var go = new GameObject("[PolyBridge Sandbox]");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<SandboxRunner>();
                Debug.Log("[PolyBridge Sandbox] Initialized.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PolyBridge Sandbox] Failed to initialize: {e}");
            }
        }
    }
}
