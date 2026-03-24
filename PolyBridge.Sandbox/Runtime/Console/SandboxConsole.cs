using System;
using System.Collections.Generic;
using UnityEngine;

namespace PolyBridge.Sandbox
{
    internal class SandboxConsole : IDisposable
    {
        private readonly List<SandboxLogEntry> _logs = new();
        private readonly int _maxEntries;

        public event Action<SandboxLogEntry> OnLogAdded;

        public IReadOnlyList<SandboxLogEntry> Logs => _logs;

        public SandboxConsole(int maxEntries = 500)
        {
            _maxEntries = maxEntries;
            Application.logMessageReceived += HandleUnityLog;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= HandleUnityLog;
        }

        public void Clear()
        {
            _logs.Clear();
        }

        public void AddLog(SandboxLogLevel level, string message)
        {
            var entry = new SandboxLogEntry(level, message, DateTime.Now.ToString("HH:mm:ss"));
            _logs.Add(entry);

            if (_logs.Count > _maxEntries)
                _logs.RemoveAt(0);

            OnLogAdded?.Invoke(entry);
        }

        private void HandleUnityLog(string message, string stackTrace, LogType type)
        {
            var level = type switch
            {
                LogType.Error => SandboxLogLevel.Error,
                LogType.Exception => SandboxLogLevel.Error,
                LogType.Warning => SandboxLogLevel.Warning,
                _ => SandboxLogLevel.Info
            };

            AddLog(level, message);
        }
    }
}
