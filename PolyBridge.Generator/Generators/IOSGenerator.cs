using System.Collections.Immutable;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal class IOSGenerator : IPlatformGenerator
    {
        public string PlatformSymbol => "UNITY_IOS";
        public string PlatformSuffix => "IOS";

        public void GenerateFields(CodeBuilder builder, ImmutableArray<MethodModel> methods)
        {
            builder.AddUsing("System.Runtime.InteropServices");

            foreach (var method in methods)
            {
                builder.AppendLine("[DllImport(\"__Internal\")]");
                builder.AppendLine($"private static extern {method.InnerReturnType} {method.IOSNativeName}({method.ParameterDeclarations});");
            }
        }

        public void GenerateConstructorBody(CodeBuilder builder, string classPath)
            => builder.AppendLine("// iOS does not require explicit object instantiation for static externs.");

        public void GenerateMethodBody(CodeBuilder builder, MethodModel method)
        {
            var nativeCall = $"{method.IOSNativeName}({method.ParameterNames})";
            var returnStr = method.HasReturn ? "return " : "";

            builder.AppendLine(method.IsAsync
                ? $"{returnStr}await Task.Run(() => {nativeCall});"
                : $"{returnStr}{nativeCall};");
        }
    }
}