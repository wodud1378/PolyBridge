using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    public class SandboxRunner : MonoBehaviour
    {
        private static SandboxRunner _instance;

        private readonly List<ISandboxGestureDetector> _detectors = new();
        private PolyBridgeSandbox _sandbox;
        private bool _isOpen;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            try
            {
                var config = SandboxConfig.Load();
                if (config == null)
                {
                    Debug.LogWarning("[PolyBridge Sandbox] SandboxConfig not found.");
                    return;
                }

                var input = SandboxInputFactory.Create();
                foreach (var gesture in config.gestures)
                {
                    if (gesture != null)
                        _detectors.Add(gesture.CreateDetector(input));
                }

                Debug.Log($"[PolyBridge Sandbox] Runner started with {_detectors.Count} gesture detector(s).");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PolyBridge Sandbox] Runner start failed: {e}");
            }
        }

        private void Update()
        {
            foreach (var detector in _detectors)
            {
                if (detector.Detect())
                {
                    Toggle();
                    break;
                }
            }
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        private void Open()
        {
            try
            {
                if (_sandbox == null)
                {
                    var config = SandboxConfig.Load();
                    var uiDoc = gameObject.AddComponent<UIDocument>();
                    if (config != null && config.panelSettings != null)
                        uiDoc.panelSettings = config.panelSettings;

                    _sandbox = gameObject.AddComponent<PolyBridgeSandbox>();
                    Debug.Log("[PolyBridge Sandbox] Opened (first time).");
                }
                else
                {
                    var uiDoc = GetComponent<UIDocument>();
                    if (uiDoc != null) uiDoc.enabled = true;
                    _sandbox.enabled = true;
                    Debug.Log("[PolyBridge Sandbox] Opened.");
                }

                _isOpen = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PolyBridge Sandbox] Failed to open: {e}");
            }
        }

        private void Close()
        {
            if (_sandbox != null)
                _sandbox.enabled = false;

            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc != null) uiDoc.enabled = false;

            _isOpen = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // === Static API ===

        private static SandboxRunner GetOrCreate()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("[PolyBridge Sandbox]");
            DontDestroyOnLoad(go);
            return go.AddComponent<SandboxRunner>();
        }

        public static void ShowSandbox()
        {
            var runner = GetOrCreate();
            if (!runner._isOpen) runner.Open();
        }

        public static void HideSandbox()
        {
            if (_instance != null && _instance._isOpen)
                _instance.Close();
        }

        public static void ToggleSandbox()
        {
            GetOrCreate().Toggle();
        }
    }
}
