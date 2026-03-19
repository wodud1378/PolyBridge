using System.Collections.Immutable;
using System.Linq;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal class IOSGenerator : IPlatformGenerator
    {
        public string PlatformSymbol => "UNITY_IOS";
        public string PlatformSuffix => "IOSImpl";

        private static string ExternName(MethodModel method) => $"{method.Name}_Extern";

        public void GenerateFields(CodeBuilder builder, ServiceModel model)
        {
            foreach (var method in model.Methods)
            {
                var externName = ExternName(method);

                if (method.IsAsync)
                {
                    var nativeExternParams = string.Join(", ", method.NativeParameters.Select(p =>
                        MethodModel.IsPrimitiveType(p.Type) ? $"{p.Type} {p.Name}" : $"string {p.Name}"));
                    var extraParams = "int requestId, PolyBridge.Core.Runtime.IOSBridgeCallback.CallbackDelegate callback";
                    var allParams = string.IsNullOrEmpty(nativeExternParams) ? extraParams : $"{nativeExternParams}, {extraParams}";
                    builder.AppendLine($"[System.Runtime.InteropServices.DllImport(\"__Internal\", EntryPoint = \"{method.IOSNativeName}\")]");
                    builder.AppendLine($"private static extern void {externName}({allParams});");
                }
                else
                {
                    var nativeExternParams = string.Join(", ", method.NativeParameters.Select(p =>
                        MethodModel.IsPrimitiveType(p.Type) ? $"{p.Type} {p.Name}" : $"string {p.Name}"));
                    builder.AppendLine($"[System.Runtime.InteropServices.DllImport(\"__Internal\", EntryPoint = \"{method.IOSNativeName}\")]");
                    builder.AppendLine($"private static extern {method.InnerReturnType} {externName}({nativeExternParams});");
                }
            }
        }

        public void GenerateConstructorBody(CodeBuilder builder, ServiceModel model)
        {
            builder.AppendLine("// iOS does not require explicit object instantiation for static externs.");
        }

        public void GenerateMethodBody(CodeBuilder builder, MethodModel method)
        {
            if (method.IsAsync)
                GenerateAsyncBody(builder, method);
            else
                GenerateSyncBody(builder, method);
        }

        public void GenerateDisposeBody(CodeBuilder builder, ServiceModel model)
        {
            // iOS event bridge cleanup is handled by the event bridge's own Dispose
        }

        public void GenerateInnerClasses(CodeBuilder builder, ServiceModel model)
        {
        }

        public void GenerateEventBridgeRegistration(CodeBuilder builder, ServiceModel model)
        {
            // iOS doesn't need explicit event bridge registration — native side calls methods directly
        }

        private static void GenerateSyncBody(CodeBuilder builder, MethodModel method)
        {
            var nativeCall = $"{ExternName(method)}({method.NativeParameterExpressions})";
            var returnStr = method.HasReturn ? "return " : "";
            builder.AppendLine($"{returnStr}{nativeCall};");
        }

        private static void GenerateAsyncBody(CodeBuilder builder, MethodModel method)
        {
            var nativeParamExprs = method.NativeParameterExpressions;
            var paramArgs = !string.IsNullOrEmpty(nativeParamExprs) ? $"{nativeParamExprs}, " : "";

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

            if (method.HasCancellationToken)
            {
                var ctName = method.CancellationTokenParameterName;
                builder.AppendLine($"var ctr = {ctName}.Register(() => {{ PolyBridge.Core.Runtime.IOSBridgeCallback.Unregister(requestId); {tcsVar}.TrySetCanceled({ctName}); }});");
            }

            builder.AppendLine($"{ExternName(method)}({paramArgs}requestId, PolyBridge.Core.Runtime.IOSBridgeCallback.OnResult);");

            if (method.HasCancellationToken)
            {
                builder.AppendLine($"try {{ {awaitExpr} }}");
                builder.AppendLine("finally { ctr.Dispose(); }");
            }
            else
            {
                builder.AppendLine(awaitExpr);
            }
        }
    }
}
