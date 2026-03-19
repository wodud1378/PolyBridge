using System;
using System.Threading.Tasks;

namespace PolyBridge.Sandbox
{
    internal static class SandboxMethodInvoker
    {
        internal static object ConvertParam(string value, Type targetType)
        {
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.TryParse(value, out var i) ? i : 0;
            if (targetType == typeof(float)) return float.TryParse(value, out var f) ? f : 0f;
            if (targetType == typeof(double)) return double.TryParse(value, out var d) ? d : 0.0;
            if (targetType == typeof(bool)) return bool.TryParse(value, out var b) && b;
            if (targetType == typeof(long)) return long.TryParse(value, out var l) ? l : 0L;
            return value;
        }

        internal static async Task<string> InvokeAsync(object instance, SandboxMethodInfo methodInfo, string[] paramValues)
        {
            try
            {
                var args = new object[methodInfo.Params.Count];
                for (var i = 0; i < methodInfo.Params.Count; i++)
                    args[i] = ConvertParam(paramValues[i], methodInfo.Params[i].Type);

                var result = methodInfo.Method.Invoke(instance, args);

                if (result is Task task)
                {
                    await task;

                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        var resultProp = taskType.GetProperty("Result");
                        var taskResult = resultProp?.GetValue(task);
                        return taskResult?.ToString() ?? "(null)";
                    }

                    return "(completed)";
                }

                return result?.ToString() ?? "(void)";
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                return $"ERROR: {inner.Message}";
            }
        }
    }
}
