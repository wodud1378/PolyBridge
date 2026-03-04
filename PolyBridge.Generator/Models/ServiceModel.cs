using System.Collections.Immutable;

namespace PolyBridge.Generator.Models
{
    internal record ServiceModel(string ClassName, string Namespace, string ClassPath, ImmutableArray<MethodModel> Methods)
    {
        public string ClassName { get; } = ClassName;
        public string Namespace { get; } = Namespace;
        public string ClassPath { get; } = ClassPath;
        public ImmutableArray<MethodModel> Methods { get; } = Methods;
    }
}