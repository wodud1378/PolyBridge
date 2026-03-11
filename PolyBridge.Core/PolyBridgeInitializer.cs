#if UNITY_5_3_OR_NEWER
using System.Threading;
using PolyBridge.Core.Runtime;
using UnityEngine;

namespace PolyBridge.Core
{
    internal static class PolyBridgeInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            NativeDispatcher.Initialize(SynchronizationContext.Current);
        }
    }
}
#endif
