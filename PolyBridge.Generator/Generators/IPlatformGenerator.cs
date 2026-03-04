using System.Collections.Immutable;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal interface IPlatformGenerator
    {
        string PlatformSymbol { get; } // ex) UNITY_ANDROID, UNITY_IOS
        string PlatformSuffix { get; } // ex) Android, IOS
        void GenerateFields(CodeBuilder builder, ImmutableArray<MethodModel> methods);
        void GenerateConstructorBody(CodeBuilder builder, string classPath);
        void GenerateMethodBody(CodeBuilder builder, MethodModel method);
    }
}