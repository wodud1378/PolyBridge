namespace PolyBridge.Sandbox
{
    internal enum SandboxLogLevel
    {
        Info,
        Warning,
        Error,
        Native
    }

    internal class SandboxLogEntry
    {
        public SandboxLogLevel Level { get; }
        public string Message { get; }
        public string Timestamp { get; }

        public SandboxLogEntry(SandboxLogLevel level, string message, string timestamp)
        {
            Level = level;
            Message = message;
            Timestamp = timestamp;
        }
    }
}
