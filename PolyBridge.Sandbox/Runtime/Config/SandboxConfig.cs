using System.Collections.Generic;
using UnityEngine;

namespace PolyBridge.Sandbox
{
    [CreateAssetMenu(menuName = "PolyBridge/Sandbox Config", fileName = "SandboxConfig")]
    public class SandboxConfig : ScriptableObject
    {
        public const string ResourcesPathKey = "PolyBridge_SandboxConfig_ResourcesPath";
        public const string DefaultResourcesPath = "SandboxConfig";

        public bool autoInitialize = true;

        [SerializeReference]
        public List<ISandboxGesture> gestures = new()
        {
            new KeyboardShortcutGesture(),
            new MultiTouchGesture { requiredTouches = 3 }
        };

        public static SandboxConfig Load()
        {
#if UNITY_EDITOR
            var path = UnityEditor.EditorPrefs.GetString(ResourcesPathKey, DefaultResourcesPath);
#else
            var path = DefaultResourcesPath;
#endif
            var config = Resources.Load<SandboxConfig>(path);
            if (config == null)
                config = Resources.Load<SandboxConfig>(DefaultResourcesPath);
            return config;
        }
    }
}
