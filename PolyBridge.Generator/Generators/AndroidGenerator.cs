using System.Collections.Immutable;
using System.Linq;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal class AndroidGenerator : IPlatformGenerator
    {
        public string PlatformSymbol => "UNITY_ANDROID";
        public string PlatformSuffix => "AndroidImpl";

        public void GenerateFields(CodeBuilder builder, ServiceModel model)
        {
            builder.AppendField("private", true, "PolyBridge.Core.Runtime.AndroidBridge", "_bridge");
        }

        public void GenerateConstructorBody(CodeBuilder builder, ServiceModel model)
        {
            builder.AppendLine($"_bridge = new PolyBridge.Core.Runtime.AndroidBridge(\"{model.ClassPath}\");");
        }

        public void GenerateMethodBody(CodeBuilder builder, MethodModel method, ServiceModel model)
        {
            if (method.IsAsync)
                GenerateAsyncBody(builder, method, model);
            else
                GenerateSyncBody(builder, method);
        }

        public void GenerateDisposeBody(CodeBuilder builder, ServiceModel model)
        {
            if (model.HasBridge)
                builder.AppendLine("if (_nativeBridge != null) _bridge.Call(\"removeListener\", _nativeBridge);");
        }

        public void GenerateInnerClasses(CodeBuilder builder, ServiceModel model)
        {
        }

        public void GenerateBridgeRegistration(CodeBuilder builder, ServiceModel model)
        {
            builder.AppendLine("_bridge.Call(\"addListener\", _nativeBridge);");
        }

        private static void GenerateSyncBody(CodeBuilder builder, MethodModel method)
        {
            var nativeParamExprs = method.NativeParameterExpressions;
            var paramArgs = !string.IsNullOrEmpty(nativeParamExprs) ? $", {nativeParamExprs}" : "";
            var callMethod = method.HasReturn ? $"Call<{method.InnerReturnType}>" : "Call";
            var nativeCall = $"_bridge.{callMethod}(\"{method.AndroidNativeName}\"{paramArgs})";
            var returnStr = method.HasReturn ? "return " : "";
            builder.AppendLine($"{returnStr}{nativeCall};");
        }

        private static void GenerateAsyncBody(CodeBuilder builder, MethodModel method, ServiceModel model)
        {
            var nativeParamExprs = method.NativeParameterExpressions;
            var paramArgs = !string.IsNullOrEmpty(nativeParamExprs) ? $"{nativeParamExprs}, " : "";

            string tcsType, tcsVar, awaitExpr;

            if (method.IsUniTask)
            {
                tcsVar = "utcs";
                tcsType = method.HasReturn
                    ? $"Cysharp.Threading.Tasks.UniTaskCompletionSource<{method.InnerReturnType}>"
                    : "Cysharp.Threading.Tasks.UniTaskCompletionSource";
                awaitExpr = method.HasReturn ? $"return await {tcsVar}.Task;" : $"await {tcsVar}.Task;";
            }
            else
            {
                tcsVar = "tcs";
                tcsType = method.HasReturn
                    ? $"System.Threading.Tasks.TaskCompletionSource<{method.InnerReturnType}>"
                    : "System.Threading.Tasks.TaskCompletionSource<bool>";
                awaitExpr = method.HasReturn ? $"return await {tcsVar}.Task;" : $"await {tcsVar}.Task;";
            }

            builder.AppendLine($"var {tcsVar} = new {tcsType}();");

            // Create callback bridge instance and subscribe
            if (model?.HasBridge == true)
            {
                builder.AppendLine($"var callback = new {model.BridgeTypeName}();");
            }
            else
            {
                builder.AppendLine("throw new System.NotSupportedException(\"CallbackBridgeType is not specified. Define a [NativeBridge] and set CallbackBridgeType on [NativeService].\");");
                return;
            }

            // Find the result mapping for this specific method
            var resultMapping = model.GetResultMapping(method.Name);
            var resultEvent = resultMapping?.EventName ?? "OnResult";
            var resultParams = resultMapping?.Parameters ?? ImmutableArray<ParameterModel>.Empty;
            var errorMapping = model.GetErrorMapping(method.Name);
            var errorEvent = errorMapping?.EventName ?? "OnError";
            var errorParams = errorMapping?.Parameters ?? ImmutableArray<ParameterModel>.Empty;

            // Build lambda parameter list matching the bridge method signature
            var resultLambdaParams = resultParams.Length switch
            {
                0 => "()",
                1 => resultParams[0].Name,
                _ => $"({string.Join(", ", resultParams.Select(p => p.Name))})"
            };

            if (method.HasReturn)
            {
                var resultValueExpr = resultParams.Length > 0 ? resultParams[0].Name : "null";
                var firstParamType = resultMapping?.FirstParamType ?? "string";

                // Type-aware conversion: same type → direct, string → ResultConversion, else → direct
                string convertedExpr;
                if (firstParamType == method.InnerReturnType)
                    convertedExpr = resultValueExpr;
                else if (firstParamType == "string" || firstParamType == "global::System.String")
                    convertedExpr = MethodModel.ResultConversion(resultValueExpr, method.InnerReturnType);
                else
                    convertedExpr = resultValueExpr;

                builder.AppendLine($"callback.{resultEvent} += {resultLambdaParams} => {{ try {{ {tcsVar}.TrySetResult({convertedExpr}); }} catch (System.Exception ex) {{ {tcsVar}.TrySetException(ex); }} }};");
            }
            else
            {
                if (method.IsUniTask)
                    builder.AppendLine($"callback.{resultEvent} += {resultLambdaParams} => {tcsVar}.TrySetResult();");
                else
                    builder.AppendLine($"callback.{resultEvent} += {resultLambdaParams} => {tcsVar}.TrySetResult(true);");
            }

            // Subscribe to Error event — first parameter as error message
            var errorLambdaParams = errorParams.Length switch
            {
                0 => "()",
                1 => errorParams[0].Name,
                _ => $"({string.Join(", ", errorParams.Select(p => p.Name))})"
            };
            var errorValueExpr = errorParams.Length > 0
                ? (errorParams[0].Type == "string" || errorParams[0].Type == "global::System.String"
                    ? errorParams[0].Name
                    : $"{errorParams[0].Name}.ToString()")
                : "\"Unknown error\"";
            builder.AppendLine($"callback.{errorEvent} += {errorLambdaParams} => {tcsVar}.TrySetException(new System.Exception({errorValueExpr}));");

            if (method.HasCancellationToken)
            {
                var ctName = method.CancellationTokenParameterName;
                builder.AppendLine($"var ctr = {ctName}.Register(() => {tcsVar}.TrySetCanceled({ctName}));");
            }

            builder.AppendLine($"_bridge.Call(\"{method.AndroidNativeName}\", {paramArgs}callback);");

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
