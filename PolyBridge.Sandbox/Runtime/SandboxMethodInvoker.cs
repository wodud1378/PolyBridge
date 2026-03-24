using System;
using System.Threading;
using System.Threading.Tasks;
using PolyBridge.Core.Serialization;
using UnityEngine;

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
        internal static async Task<SandboxResult> InvokeAsync(object instance, SandboxMethodInfo methodInfo, string[] paramValues)
        {
            try
            {
                Debug.Log($"[PolyBridge Sandbox] Invoking {methodInfo.Method.DeclaringType?.Name}.{methodInfo.Method.Name}");
                var args = BuildArgs(methodInfo, paramValues);
                var result = methodInfo.Method.Invoke(instance, args);
                Debug.Log($"[PolyBridge Sandbox] Method returned: {result?.GetType().Name ?? "null"} (ReturnType: {methodInfo.Method.ReturnType.Name})");
                return await ResolveResult(result, methodInfo.Method.ReturnType);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Debug.LogError($"[PolyBridge Sandbox] Invoke failed: {inner}");
                return SandboxResult.Fail(inner.Message);
            }
        }

        private static object[] BuildArgs(SandboxMethodInfo methodInfo, string[] paramValues)
        {
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

            return args;
        }

        private static async Task<SandboxResult> ResolveResult(object result, Type returnType)
        {
            // Sync
            if (result == null)
                return SandboxResult.Ok("(null)");

            // Task
            if (result is Task task)
                return await ResolveTask(task, returnType);

            // UniTask — AsTask()로 변환
            var resultType = result.GetType();
            var asTaskMethod = resultType.GetMethod("AsTask");
            if (asTaskMethod != null && typeof(Task).IsAssignableFrom(asTaskMethod.ReturnType))
            {
                var task2 = (Task)asTaskMethod.Invoke(result, null);
                return await ResolveTask(task2, asTaskMethod.ReturnType);
            }

            // Sync with return value
            return SandboxResult.Ok(FormatValue(result));
        }

        private static async Task<SandboxResult> ResolveTask(Task task, Type returnType)
        {
            await task;

            if (!returnType.IsGenericType)
                return SandboxResult.Ok("(void)");

            var innerType = returnType.GetGenericArguments()[0];
            var genericTaskType = typeof(Task<>).MakeGenericType(innerType);
            var resultProp = genericTaskType.GetProperty("Result");

            if (resultProp == null)
            {
                Debug.LogWarning($"[PolyBridge Sandbox] No Result property on {genericTaskType.Name}");
                return SandboxResult.Ok("(null)");
            }

            var taskResult = resultProp.GetValue(task);
            Debug.Log($"[PolyBridge Sandbox] Task result: {taskResult?.GetType().Name ?? "null"}");

            if (taskResult == null)
                return SandboxResult.Ok("(null)");

            return SandboxResult.Ok(FormatValue(taskResult));
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "(null)";

            var type = value.GetType();

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return value.ToString();

            try
            {
                return PolyBridgeSerializerRegistry.Serializer.Serialize(value);
            }
            catch
            {
                return value.ToString();
            }
        }

        private static object ConvertParam(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.TryParse(value, out var i) ? i : 0;
            if (targetType == typeof(float)) return float.TryParse(value, out var f) ? f : 0f;
            if (targetType == typeof(double)) return double.TryParse(value, out var d) ? d : 0.0;
            if (targetType == typeof(bool)) return bool.TryParse(value, out var b) && b;
            if (targetType == typeof(long)) return long.TryParse(value, out var l) ? l : 0L;
            return value;
        }
    }
}
