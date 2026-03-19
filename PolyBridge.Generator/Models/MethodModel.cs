using System.Collections.Immutable;
using System.Linq;

namespace PolyBridge.Generator.Models
{
    internal record MethodModel(
        string Name,
        string AccessModifier,
        string AndroidNativeName,
        string IOSNativeName,
        string ReturnType,
        string InnerReturnType,
        IAsyncType AsyncType,
        ImmutableArray<ParameterModel> AllParameters,
        ImmutableArray<ParameterModel> NativeParameters,
        bool HasCancellationToken,
        string CancellationTokenParameterName,
        string MockMethodName = null)
    {
        public string Name { get; } = Name;
        public string AccessModifier { get; } = AccessModifier;
        public string AndroidNativeName { get; } = AndroidNativeName;
        public string IOSNativeName { get; } = IOSNativeName;
        public string ReturnType { get; } = ReturnType;
        public string InnerReturnType { get; } = InnerReturnType;
        public IAsyncType AsyncType { get; } = AsyncType;
        public ImmutableArray<ParameterModel> AllParameters { get; } = AllParameters;
        public ImmutableArray<ParameterModel> NativeParameters { get; } = NativeParameters;
        public bool HasCancellationToken { get; } = HasCancellationToken;
        public string CancellationTokenParameterName { get; } = CancellationTokenParameterName;
        public string MockMethodName { get; } = MockMethodName;

        public string PartialModifier => string.IsNullOrEmpty(AccessModifier) ? "partial" : $"{AccessModifier} partial";

        public string ParameterDeclarations => string.Join(", ", AllParameters.Select(p => $"{p.Type} {p.Name}"));
        public string ParameterNames => string.Join(", ", AllParameters.Select(p => p.Name));
        public string NativeParameterNames => string.Join(", ", NativeParameters.Select(p => p.Name));
        public string NativeParameterExpressions => string.Join(", ", NativeParameters.Select(p => ParameterConversion(p.Name, p.Type)));
        public bool HasReturn => InnerReturnType != "void";
        public bool IsAsync => AsyncType != null;
        public bool IsUniTask => AsyncType is UniTaskType;

        private static readonly string[] PrimitiveTypes =
        {
            "string", "global::System.String",
            "int", "global::System.Int32",
            "bool", "global::System.Boolean",
            "float", "global::System.Single",
            "double", "global::System.Double",
            "long", "global::System.Int64"
        };

        public static string ResultConversion(string resultVar, string targetType)
        {
            switch (targetType)
            {
                case "string":
                case "global::System.String":
                    return resultVar;
                case "int":
                case "global::System.Int32":
                    return $"int.Parse({resultVar})";
                case "bool":
                case "global::System.Boolean":
                    return $"bool.Parse({resultVar})";
                case "float":
                case "global::System.Single":
                    return $"float.Parse({resultVar})";
                case "double":
                case "global::System.Double":
                    return $"double.Parse({resultVar})";
                case "long":
                case "global::System.Int64":
                    return $"long.Parse({resultVar})";
                default:
                    return $"PolyBridge.Core.Serialization.PolyBridgeSerializerRegistry.Serializer.Deserialize<{targetType}>({resultVar})";
            }
        }

        public static string ParameterConversion(string paramName, string paramType)
        {
            if (IsPrimitiveType(paramType))
                return paramName;
            return $"PolyBridge.Core.Serialization.PolyBridgeSerializerRegistry.Serializer.Serialize({paramName})";
        }

        public static bool IsPrimitiveType(string type)
        {
            foreach (var t in PrimitiveTypes)
            {
                if (t == type) return true;
            }
            return false;
        }

        public virtual bool Equals(MethodModel other)
        {
            if (other is null) return false;
            return Name == other.Name &&
                   AccessModifier == other.AccessModifier &&
                   AndroidNativeName == other.AndroidNativeName &&
                   IOSNativeName == other.IOSNativeName &&
                   ReturnType == other.ReturnType &&
                   InnerReturnType == other.InnerReturnType &&
                   Equals(AsyncType, other.AsyncType) &&
                   AllParameters.SequenceEqual(other.AllParameters) &&
                   NativeParameters.SequenceEqual(other.NativeParameters) &&
                   HasCancellationToken == other.HasCancellationToken &&
                   CancellationTokenParameterName == other.CancellationTokenParameterName &&
                   MockMethodName == other.MockMethodName;
        }

        public override int GetHashCode() => HashHelper.Combine(Name, AccessModifier, AndroidNativeName, IOSNativeName, ReturnType, InnerReturnType, AsyncType, AllParameters, NativeParameters, HasCancellationToken, CancellationTokenParameterName, MockMethodName);
    }
}
