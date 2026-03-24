using UnityEngine;

namespace PolyBridge.Sandbox
{
    public class KeyboardGestureDetector : ISandboxGestureDetector
    {
        private readonly KeyCode _key;
        private readonly bool _requireShift;
        private readonly ISandboxInput _input;

        internal KeyboardGestureDetector(KeyCode key, bool requireShift, ISandboxInput input)
        {
            _key = key;
            _requireShift = requireShift;
            _input = input;
        }

        public bool Detect()
        {
            if (!_input.GetKeyDown(_key)) return false;
            if (_requireShift && !_input.GetKey(KeyCode.LeftShift) && !_input.GetKey(KeyCode.RightShift))
                return false;
            return true;
        }
    }
}
