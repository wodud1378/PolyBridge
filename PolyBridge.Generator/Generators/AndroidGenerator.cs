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
            => builder.AppendField("private", true, "UnityEngine.AndroidJavaObject", "_nativeObject");

        public void GenerateConstructorBody(CodeBuilder builder, string classPath)
            => builder.AppendLine($"_nativeObject = new UnityEngine.AndroidJavaObject(\"{classPath}\");");

        public void GenerateMethodBody(CodeBuilder builder, MethodModel method)
        {
            var paramArgs = !method.Parameters.IsEmpty ? $", {method.ParameterNames}" : "";
            var callMethod = method.HasReturn ? $"Call<{method.InnerReturnType}>" : "Call";
            var nativeCall = $"_nativeObject.{callMethod}(\"{method.AndroidNativeName}\"{paramArgs})";
            builder.AppendLine(method.FormatCall(nativeCall));
        }
    }
}
