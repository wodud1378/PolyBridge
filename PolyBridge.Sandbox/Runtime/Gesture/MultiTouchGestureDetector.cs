using UnityEngine;

namespace PolyBridge.Sandbox
{
    public class MultiTouchGestureDetector : ISandboxGestureDetector
    {
        private readonly int _requiredTouches;
        private bool _wasTouching;

        internal MultiTouchGestureDetector(int requiredTouches)
        {
            _requiredTouches = requiredTouches;
        }

        public bool Detect()
        {
            var touching = Input.touchCount >= _requiredTouches;

            if (_wasTouching && !touching)
            {
                _wasTouching = false;
                return true;
            }

            _wasTouching = touching;
            return false;
        }
    }
}
