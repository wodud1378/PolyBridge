using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Generators;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Helpers
{
    internal static class ServiceSourceEmitter
    {
        internal static void EmitInterface(SourceEmitter emitter, ServiceModel model, string implInterfaceName)
        {
            var inheritance = model.HasBridge ? "System.IDisposable" : null;
            emitter.Emit(implInterfaceName, "internal", isInterface: true, inheritance: inheritance, body: builder =>
            {
                foreach (var method in model.Methods)
                    builder.AppendLine($"{method.ReturnType} {method.Name}({method.ParameterDeclarations});");
            });
        }

        internal static void EmitPartialClass(SourceEmitter emitter, ServiceModel model, string implInterfaceName, IPlatformGenerator[] generators)
        {
            var inheritance = model.HasBridge ? "System.IDisposable" : null;
            emitter.Emit(model.ClassName, "public partial", inheritance: inheritance, body: builder =>
            {
                builder.AppendField("private", true, implInterfaceName, "_impl");
                if (model.HasBridge)
                    builder.AppendField("private", true, model.BridgeTypeName, "_nativeBridge");
                builder.AppendLine();

                if (model.HasBridge)
                {
                    builder.AppendLine($"{model.BridgeAccessModifier} {model.BridgeTypeName} Bridge => _nativeBridge;");
                    builder.AppendLine();
                }

                using (builder.StartConstructor("public", model.ClassName))
                {
                    if (model.HasBridge)
                        builder.AppendLine($"_nativeBridge = new {model.BridgeTypeName}();");

                    builder.AppendPreprocessorIf("UNITY_EDITOR");
                    builder.AppendLine($"_impl = new {model.ClassName}EditorImpl(this);");

                    for (var i = 0; i < generators.Length; i++)
                    {
                        var gen = generators[i];
                        var implClassName = $"{model.ClassName}{gen.PlatformSuffix}";
                        builder.AppendPreprocessorElif(gen.PlatformSymbol);
                        if (model.HasBridge)
                        {
                            builder.AppendLine($"var platformImpl = new {implClassName}();");
                            builder.AppendLine("platformImpl.RegisterBridge(_nativeBridge);");
                            builder.AppendLine("_impl = platformImpl;");
                        }
                        else
                        {
                            builder.AppendLine($"_impl = new {implClassName}();");
                        }
                    }

                    builder.AppendPreprocessorEndif();
                }

                foreach (var method in model.Methods)
                {
                    builder.AppendLine();
                    using (builder.StartMethod(method.PartialModifier, method.ReturnType, method.Name, method.IsAsync, method.ParameterDeclarations))
                    {
                        var returnStr = method.HasReturn ? "return " : "";
                        var awaitStr = method.IsAsync ? "await " : "";
                        builder.AppendLine($"{returnStr}{awaitStr}_impl.{method.Name}({method.ParameterNames});");
                    }
                }

                if (model.HasBridge)
                {
                    builder.AppendLine();
                    using (builder.StartMethod("public", "void", "Dispose"))
                    {
                        builder.AppendLine("_impl?.Dispose();");
                        builder.AppendLine("_nativeBridge?.Dispose();");
                    }
                }
            });
        }

        internal static void EmitPlatformImpl(SourceEmitter emitter, ServiceModel model, string implInterfaceName, IPlatformGenerator gen)
        {
            var platformClassName = $"{model.ClassName}{gen.PlatformSuffix}";
            emitter.Emit(platformClassName, "internal", inheritance: implInterfaceName,
                preprocessorGuard: gen.PlatformSymbol, body: builder =>
                {
                    gen.GenerateFields(builder, model);
                    if (model.HasBridge)
                        builder.AppendField("private", false, model.BridgeTypeName, "_nativeBridge");
                    builder.AppendLine();

                    using (builder.BeginScope($"internal {platformClassName}()"))
                    {
                        gen.GenerateConstructorBody(builder, model);
                    }

                    if (model.HasBridge)
                    {
                        builder.AppendLine();
                        using (builder.StartMethod("internal", "void", "RegisterBridge", false, $"{model.BridgeTypeName} bridge"))
                        {
                            builder.AppendLine("_nativeBridge = bridge;");
                            gen.GenerateBridgeRegistration(builder, model);
                        }
                    }

                    foreach (var method in model.Methods)
                    {
                        builder.AppendLine();
                        using (builder.StartMethod("public", method.ReturnType, method.Name, method.IsAsync, method.ParameterDeclarations))
                            gen.GenerateMethodBody(builder, method, model);
                    }

                    if (model.HasBridge)
                    {
                        builder.AppendLine();
                        using (builder.StartMethod("public", "void", "Dispose"))
                            gen.GenerateDisposeBody(builder, model);
                    }

                    gen.GenerateInnerClasses(builder, model);
                });
        }
    }
}
