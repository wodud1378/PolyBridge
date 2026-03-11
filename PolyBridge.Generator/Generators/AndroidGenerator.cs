using System.Collections.Immutable;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal class AndroidGenerator : IPlatformGenerator
    {
        public string PlatformSymbol => "UNITY_ANDROID";
        public string PlatformSuffix => "Android";

        public void GenerateFields(CodeBuilder builder, ImmutableArray<MethodModel> methods)
            => builder.AppendField("private", true, "PolyBridge.Core.Runtime.AndroidBridge", "_bridge");

        public void GenerateConstructorBody(CodeBuilder builder, string classPath)
            => builder.AppendLine($"_bridge = new PolyBridge.Core.Runtime.AndroidBridge(\"{classPath}\");");

        public void GenerateMethodBody(CodeBuilder builder, MethodModel method)
        {
            if (method.IsAsync)
                GenerateAsyncBody(builder, method);
            else
                GenerateSyncBody(builder, method);
        }

        private static void GenerateSyncBody(CodeBuilder builder, MethodModel method)
        {
            var paramArgs = !method.Parameters.IsEmpty ? $", {method.ParameterNames}" : "";
            var callMethod = method.HasReturn ? $"Call<{method.InnerReturnType}>" : "Call";
            var nativeCall = $"_bridge.{callMethod}(\"{method.AndroidNativeName}\"{paramArgs})";
            var returnStr = method.HasReturn ? "return " : "";
            builder.AppendLine($"{returnStr}{nativeCall};");
        }

        private static void GenerateAsyncBody(CodeBuilder builder, MethodModel method)
        {
            var paramArgs = !method.Parameters.IsEmpty ? $"{method.ParameterNames}, " : "";

            string tcsType, tcsVar, setResultExpr, awaitExpr;

            if (method.IsUniTask)
            {
                tcsVar = "utcs";
                if (method.HasReturn)
                {
                    tcsType = $"Cysharp.Threading.Tasks.UniTaskCompletionSource<{method.InnerReturnType}>";
                    var conversion = MethodModel.ResultConversion("result", method.InnerReturnType);
                    setResultExpr = $"result => {tcsVar}.TrySetResult({conversion})";
                    awaitExpr = $"return await {tcsVar}.Task;";
                }
                else
                {
                    tcsType = "Cysharp.Threading.Tasks.UniTaskCompletionSource";
                    setResultExpr = $"_ => {tcsVar}.TrySetResult()";
                    awaitExpr = $"await {tcsVar}.Task;";
                }
            }
            else
            {
                tcsVar = "tcs";
                if (method.HasReturn)
                {
                    tcsType = $"System.Threading.Tasks.TaskCompletionSource<{method.InnerReturnType}>";
                    var conversion = MethodModel.ResultConversion("result", method.InnerReturnType);
                    setResultExpr = $"result => {tcsVar}.TrySetResult({conversion})";
                    awaitExpr = $"return await {tcsVar}.Task;";
                }
                else
                {
                    tcsType = "System.Threading.Tasks.TaskCompletionSource<bool>";
                    setResultExpr = $"_ => {tcsVar}.TrySetResult(true)";
                    awaitExpr = $"await {tcsVar}.Task;";
                }
            }

            builder.AppendLine($"var {tcsVar} = new {tcsType}();");
            builder.AppendLine($"var callback = new PolyBridge.Core.Runtime.AndroidBridgeCallback(");
            builder.AppendLine($"    {setResultExpr},");
            builder.AppendLine($"    error => {tcsVar}.TrySetException(new System.Exception(error)));");
            builder.AppendLine($"_bridge.Call(\"{method.AndroidNativeName}\", {paramArgs}callback);");
            builder.AppendLine(awaitExpr);
        }
    }
}
