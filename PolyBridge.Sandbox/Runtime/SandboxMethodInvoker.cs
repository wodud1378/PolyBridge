using System;
using System.Threading.Tasks;

namespace PolyBridge.Sandbox
{
    internal enum SandboxResultStatus
    {
        Success,
        Error,
        Running
    }

    internal class SandboxResult
    {
        public SandboxResultStatus Status { get; }
        public string Body { get; }

        public SandboxResult(SandboxResultStatus status, string body)
        {
            Status = status;
            Body = body;
        }

        public static SandboxResult Running() => new(SandboxResultStatus.Running, "Running...");
        public static SandboxResult Ok(string body) => new(SandboxResultStatus.Success, body);
        public static SandboxResult Fail(string body) => new(SandboxResultStatus.Error, body);
    }

    internal static class SandboxMethodInvoker
    {
        internal static object ConvertParam(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value)) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.TryParse(value, out var i) ? i : 0;
            if (targetType == typeof(float)) return float.TryParse(value, out var f) ? f : 0f;
            if (targetType == typeof(double)) return double.TryParse(value, out var d) ? d : 0.0;
            if (targetType == typeof(bool)) return bool.TryParse(value, out var b) && b;
            if (targetType == typeof(long)) return long.TryParse(value, out var l) ? l : 0L;
            return value;
        }

        internal static async Task<SandboxResult> InvokeAsync(object instance, SandboxMethodInfo methodInfo, string[] paramValues)
        {
            try
            {
                // Build args including CancellationToken slots
                var methodParams = methodInfo.Method.GetParameters();
                var args = new object[methodParams.Length];
                var inputIndex = 0;
                for (var i = 0; i < methodParams.Length; i++)
                {
                    if (methodParams[i].ParameterType == typeof(System.Threading.CancellationToken))
                        args[i] = default(System.Threading.CancellationToken);
                    else
                        args[i] = ConvertParam(paramValues[inputIndex++], methodInfo.Params[inputIndex - 1].Type);
                }

                var result = methodInfo.Method.Invoke(instance, args);

                if (result is Task task)
                {
                    await task;
                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        var taskResult = taskType.GetProperty("Result")?.GetValue(task);
                        return SandboxResult.Ok(FormatResult(taskResult));
                    }
                    return SandboxResult.Ok("(void)");
                }

                return SandboxResult.Ok(FormatResult(result));
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                return SandboxResult.Fail(inner.Message);
            }
        }

        private static string FormatResult(object value)
        {
            if (value == null) return "(null)";
            var str = value.ToString();

            // JSON 감지 — 원문 그대로 반환
            if (str.StartsWith("{") || str.StartsWith("["))
                return str;

            return str;
        }
    }
}
