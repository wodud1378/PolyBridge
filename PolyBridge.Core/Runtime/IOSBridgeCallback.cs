#if UNITY_IOS
using System;
using System.Collections.Concurrent;
using System.Threading;
using AOT;

namespace PolyBridge.Core.Runtime
{
    internal static class IOSBridgeCallback
    {
        public delegate void CallbackDelegate(int requestId, string result, string error);

        private static int _nextId;
        private static readonly ConcurrentDictionary<int, (Action<string> onSuccess, Action<string> onError)> Pending
            = new ConcurrentDictionary<int, (Action<string>, Action<string>)>();

        public static int Register(Action<string> onSuccess, Action<string> onError)
        {
            var id = Interlocked.Increment(ref _nextId);
            Pending[id] = (onSuccess, onError);
            return id;
        }

        [MonoPInvokeCallback(typeof(CallbackDelegate))]
        public static void OnResult(int requestId, string result, string error)
        {
            if (!Pending.TryRemove(requestId, out var callbacks))
                return;

            NativeDispatcher.Post(() =>
            {
                if (error != null)
                    callbacks.onError(error);
                else
                    callbacks.onSuccess(result);
            });
        }
    }
}
#endif
