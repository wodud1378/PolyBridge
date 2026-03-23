using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    public class SandboxRunner : MonoBehaviour
    {
        private readonly List<ISandboxGestureDetector> _detectors = new();
        private PolyBridgeSandbox _sandbox;
        private bool _isOpen;

        private void Start()
        {
            var config = SandboxConfig.Load();
            if (config == null) return;

            foreach (var gesture in config.gestures)
            {
                if (gesture != null)
                    _detectors.Add(gesture.CreateDetector());
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
            if (_isOpen)
                Close();
            else
                Open();
        }

        private void Open()
        {
            if (_sandbox == null)
            {
                var panelSettings = Resources.Load<PanelSettings>("SandboxPanelSettings");

                var uiDoc = gameObject.AddComponent<UIDocument>();
                if (panelSettings != null)
                    uiDoc.panelSettings = panelSettings;

                _sandbox = gameObject.AddComponent<PolyBridgeSandbox>();
            }
            else
            {
                _sandbox.enabled = true;
                var uiDoc = GetComponent<UIDocument>();
                if (uiDoc != null) uiDoc.enabled = true;
            }

            _isOpen = true;
        }

        private void Close()
        {
            if (_sandbox != null)
                _sandbox.enabled = false;

            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc != null) uiDoc.enabled = false;

            _isOpen = false;
        }
    }
}
