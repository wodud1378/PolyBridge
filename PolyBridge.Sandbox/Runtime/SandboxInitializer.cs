using UnityEngine;

namespace PolyBridge.Sandbox
{
    internal static class SandboxInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var config = SandboxConfig.Load();
            if (config == null || !config.autoInitialize) return;

            var go = new GameObject("[PolyBridge Sandbox]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SandboxRunner>();
        }
    }
}
