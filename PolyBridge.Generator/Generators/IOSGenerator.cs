using System.Collections.Immutable;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal class IOSGenerator : IPlatformGenerator
    {
        public string PlatformSymbol => "UNITY_IOS";
        public string PlatformSuffix => "IOS";

        private static string ExternName(MethodModel method) => $"{method.Name}_Extern";

        public void GenerateFields(CodeBuilder builder, ImmutableArray<MethodModel> methods)
        {
            foreach (var method in methods)
            {
                var externName = ExternName(method);

                if (method.IsAsync)
                {
                    var userParams = method.ParameterDeclarations;
                    var extraParams = "int requestId, PolyBridge.Core.Runtime.IOSBridgeCallback.CallbackDelegate callback";
                    var allParams = string.IsNullOrEmpty(userParams) ? extraParams : $"{userParams}, {extraParams}";
                    builder.AppendLine($"[System.Runtime.InteropServices.DllImport(\"__Internal\", EntryPoint = \"{method.IOSNativeName}\")]");
                    builder.AppendLine($"private static extern void {externName}({allParams});");
                }
                else
                {
                    builder.AppendLine($"[System.Runtime.InteropServices.DllImport(\"__Internal\", EntryPoint = \"{method.IOSNativeName}\")]");
                    builder.AppendLine($"private static extern {method.InnerReturnType} {externName}({method.ParameterDeclarations});");
                }
            }
        }

        public void GenerateConstructorBody(CodeBuilder builder, string classPath)
            => builder.AppendLine("// iOS does not require explicit object instantiation for static externs.");

        public void GenerateMethodBody(CodeBuilder builder, MethodModel method)
        {
            if (method.IsAsync)
                GenerateAsyncBody(builder, method);
            else
                GenerateSyncBody(builder, method);
        }

        private static void GenerateSyncBody(CodeBuilder builder, MethodModel method)
        {
            var nativeCall = $"{ExternName(method)}({method.ParameterNames})";
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
                    setResultExpr = $"result => {{ try {{ {tcsVar}.TrySetResult({conversion}); }} catch (System.Exception ex) {{ {tcsVar}.TrySetException(ex); }} }}";
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
                    setResultExpr = $"result => {{ try {{ {tcsVar}.TrySetResult({conversion}); }} catch (System.Exception ex) {{ {tcsVar}.TrySetException(ex); }} }}";
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
            builder.AppendLine($"var requestId = PolyBridge.Core.Runtime.IOSBridgeCallback.Register(");
            builder.AppendLine($"    {setResultExpr},");
            builder.AppendLine($"    error => {tcsVar}.TrySetException(new System.Exception(error)));");
            builder.AppendLine($"{ExternName(method)}({paramArgs}requestId, PolyBridge.Core.Runtime.IOSBridgeCallback.OnResult);");
            builder.AppendLine(awaitExpr);
        }
    }
}
