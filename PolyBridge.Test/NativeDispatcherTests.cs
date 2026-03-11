using System;
using System.Threading;
using PolyBridge.Core.Runtime;
using Xunit;

namespace PolyBridge.Test
{
    public class NativeDispatcherTests
    {
        [Fact]
        public void Initialize_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => NativeDispatcher.Initialize(null));
        }

        [Fact]
        public void Post_ExecutesActionOnContext()
        {
            var mockContext = new MockSynchronizationContext();
            NativeDispatcher.Initialize(mockContext);

            bool executed = false;
            NativeDispatcher.Post(() => executed = true);

            // The mock context captures the callback; execute it
            Assert.True(mockContext.PostCalled);
            mockContext.ExecutePosted();
            Assert.True(executed);
        }

        [Fact]
        public void Post_MultipleActions_AllExecutedViaContext()
        {
            var mockContext = new MockSynchronizationContext();
            NativeDispatcher.Initialize(mockContext);

            int counter = 0;
            NativeDispatcher.Post(() => counter++);
            NativeDispatcher.Post(() => counter++);
            NativeDispatcher.Post(() => counter++);

            mockContext.ExecuteAllPosted();
            Assert.Equal(3, counter);
        }

        private class MockSynchronizationContext : SynchronizationContext
        {
            private readonly System.Collections.Generic.List<(SendOrPostCallback callback, object state)> _posted = new();

            public bool PostCalled => _posted.Count > 0;

            public override void Post(SendOrPostCallback d, object state)
            {
                _posted.Add((d, state));
            }

            public void ExecutePosted()
            {
                if (_posted.Count > 0)
                {
                    var (callback, state) = _posted[0];
                    callback(state);
                }
            }

            public void ExecuteAllPosted()
            {
                foreach (var (callback, state) in _posted)
                    callback(state);
                _posted.Clear();
            }
        }
    }
}
