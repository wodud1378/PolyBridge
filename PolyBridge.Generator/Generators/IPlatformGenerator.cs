using System.Collections.Immutable;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal interface IPlatformGenerator
    {
        string PlatformSymbol { get; } // UNITY_ANDROID, UNITY_IOS 등
        string PlatformSuffix { get; } // Android, IOS 등
        void GenerateFields(CodeBuilder builder, ImmutableArray<MethodModel> methods);
        void GenerateConstructorBody(CodeBuilder builder, string classPath);
        void GenerateMethodBody(CodeBuilder builder, MethodModel method);
    }
}