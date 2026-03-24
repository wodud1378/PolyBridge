using System;
using System.Threading;
using System.Threading.Tasks;
using PolyBridge.Core.Serialization;

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

        private SandboxResult(SandboxResultStatus status, string body)
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
        private static object ConvertParam(string value, Type targetType)
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
                    if (methodParams[i].ParameterType == typeof(CancellationToken))
                        args[i] = CancellationToken.None;
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

            var type = value.GetType();

            // Primitive 타입은 ToString
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return value.ToString();

            // struct/class → 등록된 시리얼라이저로 JSON 변환
            try
            {
                return PolyBridgeSerializerRegistry.Serializer.Serialize(value);
            }
            catch
            {
                // 시리얼라이저 실패 시 ToString 폴백
            }

            return value.ToString();
        }
    }
}
