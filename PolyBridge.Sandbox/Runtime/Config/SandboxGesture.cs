using System;
using UnityEngine;

namespace PolyBridge.Sandbox
{
    public interface ISandboxGesture
    {
        string DisplayName { get; }
        ISandboxGestureDetector CreateDetector(ISandboxInput input);
    }

    [Serializable]
    public class KeyboardShortcutGesture : ISandboxGesture
    {
        public KeyCode key = KeyCode.BackQuote;
        public bool requireShift = true;

        public string DisplayName => "Keyboard Shortcut";
        public ISandboxGestureDetector CreateDetector(ISandboxInput input) => new KeyboardGestureDetector(key, requireShift, input);
    }

    [Serializable]
    public class MultiTouchGesture : ISandboxGesture
    {
        public int requiredTouches = 3;

        public string DisplayName => $"{requiredTouches}-Finger Tap";
        public ISandboxGestureDetector CreateDetector(ISandboxInput input) => new MultiTouchGestureDetector(requiredTouches, input);
    }
}
