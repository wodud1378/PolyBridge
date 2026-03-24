using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace PolyBridge.Sandbox
{
    public interface ISandboxInput
    {
        bool GetKeyDown(KeyCode key);
        bool GetKey(KeyCode key);
        int TouchCount { get; }
    }

#if ENABLE_INPUT_SYSTEM
    public class InputSystemInput : ISandboxInput
    {
        public bool GetKeyDown(KeyCode key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var control = KeyCodeToControl(keyboard, key);
            return control is { wasPressedThisFrame: true };
        }

        public bool GetKey(KeyCode key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var control = KeyCodeToControl(keyboard, key);
            return control is { isPressed: true };
        }

        public int TouchCount
        {
            get
            {
                var touchscreen = Touchscreen.current;
                if (touchscreen == null) return 0;
                var count = 0;
                foreach (var touch in touchscreen.touches)
                {
                    if (touch.press.isPressed)
                        count++;
                }
                return count;
            }
        }

        private KeyControl KeyCodeToControl(Keyboard keyboard, KeyCode key)
        {
            // A-Z: KeyCode.A(97)~Z(122) → Key.A(15)~Z(40)
            if (key is >= KeyCode.A and <= KeyCode.Z)
                return keyboard[(Key)((int)key - (int)KeyCode.A + (int)Key.A)];

            // 0-9: KeyCode.Alpha0(48)~Alpha9(57) → Key.Digit0(41)~Digit9(50)
            if (key is >= KeyCode.Alpha0 and <= KeyCode.Alpha9)
                return keyboard[(Key)((int)key - (int)KeyCode.Alpha0 + (int)Key.Digit0)];

            // F1-F12: KeyCode.F1(282)~F12(293) → Key.F1(51)~F12(62)
            if (key is >= KeyCode.F1 and <= KeyCode.F12)
                return keyboard[(Key)((int)key - (int)KeyCode.F1 + (int)Key.F1)];

            // 개별 매핑
            return key switch
            {
                KeyCode.BackQuote => keyboard.backquoteKey,
                KeyCode.Escape => keyboard.escapeKey,
                KeyCode.Space => keyboard.spaceKey,
                KeyCode.Tab => keyboard.tabKey,
                KeyCode.Return => keyboard.enterKey,
                KeyCode.Backspace => keyboard.backspaceKey,
                KeyCode.Delete => keyboard.deleteKey,
                KeyCode.LeftShift => keyboard.leftShiftKey,
                KeyCode.RightShift => keyboard.rightShiftKey,
                KeyCode.LeftControl => keyboard.leftCtrlKey,
                KeyCode.RightControl => keyboard.rightCtrlKey,
                KeyCode.LeftAlt => keyboard.leftAltKey,
                KeyCode.RightAlt => keyboard.rightAltKey,
                _ => null
            };
        }
    }
#endif

    public class LegacyInput : ISandboxInput
    {
        public bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);
        public bool GetKey(KeyCode key) => Input.GetKey(key);
        public int TouchCount => Input.touchCount;
    }

    internal static class SandboxInputFactory
    {
        internal static ISandboxInput Create()
        {
#if ENABLE_INPUT_SYSTEM
            return new InputSystemInput();
#else
            return new LegacyInput();
#endif
        }
    }
}
