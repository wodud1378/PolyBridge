using UnityEngine;

namespace PolyBridge.Sandbox
{
    public class KeyboardGestureDetector : ISandboxGestureDetector
    {
        private readonly KeyCode _key;
        private readonly bool _requireShift;

        internal KeyboardGestureDetector(KeyCode key, bool requireShift)
        {
            _key = key;
            _requireShift = requireShift;
        }

        public bool Detect()
        {
            if (!Input.GetKeyDown(_key)) return false;
            if (_requireShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                return false;
            return true;
        }
    }
}
