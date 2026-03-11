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
        IAsyncType AsyncType,
        ImmutableArray<ParameterModel> Parameters)
    {
        public string Name { get; } = Name;
        public string AndroidNativeName { get; } = AndroidNativeName;
        public string IOSNativeName { get; } = IOSNativeName;
        public string ReturnType { get; } = ReturnType;
        public string InnerReturnType { get; } = InnerReturnType;
        public IAsyncType AsyncType { get; } = AsyncType;
        public ImmutableArray<ParameterModel> Parameters { get; } = Parameters;

        public string ParameterDeclarations => string.Join(", ", Parameters.Select(p => $"{p.Type} {p.Name}"));
        public string ParameterNames => string.Join(", ", Parameters.Select(p => p.Name));
        public bool HasReturn => InnerReturnType != "void";
        public bool IsAsync => AsyncType != null;
        public bool IsUniTask => AsyncType is UniTaskType;

        public static string ResultConversion(string resultVar, string targetType)
        {
            switch (targetType)
            {
                case "string": return resultVar;
                case "int": return $"int.Parse({resultVar})";
                case "bool": return $"bool.Parse({resultVar})";
                case "float": return $"float.Parse({resultVar})";
                case "double": return $"double.Parse({resultVar})";
                case "long": return $"long.Parse({resultVar})";
                default: return $"UnityEngine.JsonUtility.FromJson<{targetType}>({resultVar})";
            }
        }

        public virtual bool Equals(MethodModel other)
        {
            if (other is null) return false;
            return Name == other.Name &&
                   AndroidNativeName == other.AndroidNativeName &&
                   IOSNativeName == other.IOSNativeName &&
                   ReturnType == other.ReturnType &&
                   InnerReturnType == other.InnerReturnType &&
                   Equals(AsyncType, other.AsyncType) &&
                   Parameters.SequenceEqual(other.Parameters);
        }

        public override int GetHashCode() => HashHelper.Combine(Name, AndroidNativeName, IOSNativeName, ReturnType, InnerReturnType, AsyncType, Parameters);
    }
}
