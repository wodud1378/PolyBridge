using System.Collections.Immutable;
using System.Linq;

namespace PolyBridge.Generator.Models
{
    internal record MethodModel(
        string Name,
        string AndroidNativeName,
        string IOSNativeName,
        string ReturnType,
        string InnerReturnType,
        bool IsAsync,
        ImmutableArray<ParameterModel> Parameters)
    {
        public string Name { get; } = Name;
        public string AndroidNativeName { get; } = AndroidNativeName;
        public string IOSNativeName { get; } = IOSNativeName;
        public string ReturnType { get; } = ReturnType;
        public string InnerReturnType { get; } = InnerReturnType;
        public bool IsAsync { get; } = IsAsync;
        public ImmutableArray<ParameterModel> Parameters { get; } = Parameters;

        public string ParameterDeclarations => string.Join(", ", Parameters.Select(p => $"{p.Type} {p.Name}"));
        public string ParameterNames => string.Join(", ", Parameters.Select(p => p.Name));
        public bool HasReturn => InnerReturnType != "void";
    }
}
