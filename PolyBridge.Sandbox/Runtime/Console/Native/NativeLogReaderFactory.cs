namespace PolyBridge.Sandbox
{
    internal static class NativeLogReaderFactory
    {
        internal static INativeLogReader Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidLogReader();
#else
            return null;
#endif
        }
    }
}
