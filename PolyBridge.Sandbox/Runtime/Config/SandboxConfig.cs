using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    public class SandboxConfig : ScriptableObject
    {
        public bool autoInitialize = true;
        public PanelSettings panelSettings;

        [SerializeReference]
        public List<ISandboxGesture> gestures = new()
        {
            new KeyboardShortcutGesture(),
            new MultiTouchGesture { requiredTouches = 3 }
        };

        private static SandboxConfig _loaded;

        private void OnEnable()
        {
            _loaded = this;
        }

        public static SandboxConfig Load()
        {
            return _loaded;
        }
    }
}
