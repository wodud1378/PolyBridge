namespace PolyBridge.Sandbox
{
    public class MultiTouchGestureDetector : ISandboxGestureDetector
    {
        private readonly int _requiredTouches;
        private readonly ISandboxInput _input;
        private bool _wasTouching;

        internal MultiTouchGestureDetector(int requiredTouches, ISandboxInput input)
        {
            _requiredTouches = requiredTouches;
            _input = input;
        }

        public bool Detect()
        {
            var touching = _input.TouchCount >= _requiredTouches;

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
