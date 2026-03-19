using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal static class NativeBridgeGenerator
    {
        internal static void Generate(SourceEmitter emitter, BridgeModel model)
        {
            GeneratePartialClass(emitter, model);
            GenerateAndroidPartial(emitter, model);
        }

        private static void GeneratePartialClass(SourceEmitter emitter, BridgeModel model)
        {
            emitter.Emit(model.ClassName, $"{model.ClassModifier}", inheritance: "System.IDisposable", body: builder =>
            {
                foreach (var method in model.Methods)
                    builder.AppendLine($"public event {method.EventDelegateType} {method.EventName};");

                foreach (var method in model.Methods)
                {
                    builder.AppendLine();
                    using (builder.StartMethod(method.PartialModifier, "void", method.Name, false, method.ParameterDeclarations))
                    {
                        builder.AppendLine($"PolyBridge.Core.Runtime.NativeDispatcher.Post(() => {method.EventName}?.Invoke({method.ParameterNames}));");
                    }
                }

                builder.AppendLine();
                using (builder.StartMethod("public", "void", "Dispose"))
                {
                }
            });
        }

        private static void GenerateAndroidPartial(SourceEmitter emitter, BridgeModel model)
        {
            emitter.Emit(model.ClassName, $"{model.ClassModifier}",
                inheritance: "UnityEngine.AndroidJavaProxy",
                preprocessorGuard: "UNITY_ANDROID",
                fileName: $"{model.ClassName}.Android",
                body: builder =>
                {
                    using (builder.BeginScope($"public {model.ClassName}() : base(\"{model.AndroidInterfacePath}\")"))
                    {
                    }
                });
        }
    }
}
