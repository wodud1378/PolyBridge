#if UNITY_ANDROID
using System;
using UnityEngine;

namespace PolyBridge.Core.Runtime
{
    internal class AndroidBridgeCallback : AndroidJavaProxy
    {
        private readonly Action<string> _onSuccess;
        private readonly Action<string> _onError;

        public AndroidBridgeCallback(Action<string> onSuccess, Action<string> onError)
            : base("com.polybridge.IBridgeCallback")
        {
            _onSuccess = onSuccess;
            _onError = onError;
        }

        // Called from Java (may be on a background thread)
        public void onSuccess(string result)
        {
            NativeDispatcher.Post(() => _onSuccess(result));
        }

        public void onError(string error)
        {
            NativeDispatcher.Post(() => _onError(error));
        }
    }
}
#endif
