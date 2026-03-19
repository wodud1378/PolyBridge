using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Generators
{
    internal interface IPlatformGenerator
    {
        string PlatformSymbol { get; }
        string PlatformSuffix { get; }
        void GenerateFields(CodeBuilder builder, ServiceModel model);
        void GenerateConstructorBody(CodeBuilder builder, ServiceModel model);
        void GenerateMethodBody(CodeBuilder builder, MethodModel method, ServiceModel model);
        void GenerateDisposeBody(CodeBuilder builder, ServiceModel model);
        void GenerateInnerClasses(CodeBuilder builder, ServiceModel model);
        void GenerateBridgeRegistration(CodeBuilder builder, ServiceModel model);
    }
}
