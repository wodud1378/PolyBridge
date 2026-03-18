using System.Collections.Immutable;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal static class EditorImplGenerator
    {
        internal static void Generate(SourceEmitter emitter, string className, string implInterfaceName, ImmutableArray<MethodModel> methods)
        {
            var mockClassName = $"{className}EditorImpl";

            emitter.Emit(mockClassName, "internal", inheritance: implInterfaceName,
                preprocessorGuard: "UNITY_EDITOR", body: builder =>
                {
                    builder.AppendField("private", true, className, "_owner");
                    builder.AppendLine();

                    using (builder.BeginScope($"internal {mockClassName}({className} owner)"))
                    {
                        builder.AppendLine("_owner = owner;");
                    }

                    foreach (var method in methods)
                    {
                        builder.AppendLine();
                        GenerateMethodBody(builder, method);
                    }
                });
        }

        private static void GenerateMethodBody(CodeBuilder builder, MethodModel method)
        {
            using (builder.StartMethod("public", method.ReturnType, method.Name, false, method.ParameterDeclarations))
            {
                if (method.MockMethodName != null)
                {
                    var returnStr = method.HasReturn || method.IsAsync ? "return " : "";
                    builder.AppendLine($"{returnStr}_owner.{method.MockMethodName}();");
                }
                else
                {
                    GenerateDefaultFallback(builder, method);
                }
            }
        }

        private static void GenerateDefaultFallback(CodeBuilder builder, MethodModel method)
        {
            if (method.IsAsync)
            {
                if (method.HasReturn)
                {
                    if (method.IsUniTask)
                        builder.AppendLine($"return default;");
                    else
                        builder.AppendLine($"return global::System.Threading.Tasks.Task.FromResult<{method.InnerReturnType}>(default);");
                }
                else
                {
                    if (method.IsUniTask)
                        builder.AppendLine("return default;");
                    else
                        builder.AppendLine("return global::System.Threading.Tasks.Task.CompletedTask;");
                }
            }
            else if (method.HasReturn)
            {
                builder.AppendLine("return default;");
            }
        }
    }
}
