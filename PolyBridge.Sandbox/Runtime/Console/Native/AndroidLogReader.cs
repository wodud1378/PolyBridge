#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

namespace PolyBridge.Sandbox
{
    internal class AndroidLogReader : INativeLogReader
    {
        private readonly AndroidJavaObject _runtime;
        private readonly int _pid;
        private string _lastTimestamp = "";

        public AndroidLogReader()
        {
            _runtime = new AndroidJavaClass("java.lang.Runtime")
                .CallStatic<AndroidJavaObject>("getRuntime");
            _pid = new AndroidJavaClass("android.os.Process")
                .CallStatic<int>("myPid");
        }

        public void ReadNewLogs(SandboxConsole console)
        {
            try
            {
                using var process = _runtime.Call<AndroidJavaObject>("exec", $"logcat -d -v time --pid={_pid} -t 50");

                using var reader = new AndroidJavaObject("java.io.BufferedReader",
                    new AndroidJavaObject("java.io.InputStreamReader",
                        process.Call<AndroidJavaObject>("getInputStream")));

                string line;
                while ((line = reader.Call<string>("readLine")) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (string.Compare(line, _lastTimestamp, System.StringComparison.Ordinal) <= 0) continue;

                    _lastTimestamp = line.Length > 18 ? line.Substring(0, 18) : line;
                    console.AddLog(SandboxLogLevel.Native, line);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Sandbox] Native log read failed: {e.Message}");
            }
        }
    }
}
#endif
