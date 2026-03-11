using System;
using System.Threading;

namespace PolyBridge.Core.Runtime
{
    public static class NativeDispatcher
    {
        private static SynchronizationContext _mainContext;

        public static void Initialize(SynchronizationContext context)
        {
            _mainContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public static void Post(Action action)
        {
            _mainContext.Post(_ => action(), null);
        }
    }
}
