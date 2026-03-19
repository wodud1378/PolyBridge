using System.Collections.Immutable;
using System.Linq;

namespace PolyBridge.Generator.Models
{
    internal record CallbackMapping(
        string TargetMethodName,
        string EventName,
        ImmutableArray<ParameterModel> Parameters)
    {
        public string TargetMethodName { get; } = TargetMethodName;
        public string EventName { get; } = EventName;
        public ImmutableArray<ParameterModel> Parameters { get; } = Parameters;

        public string FirstParamType => Parameters.Length > 0 ? Parameters[0].Type : "string";

        public virtual bool Equals(CallbackMapping other)
        {
            if (other is null) return false;
            return TargetMethodName == other.TargetMethodName &&
                   EventName == other.EventName &&
                   Parameters.SequenceEqual(other.Parameters);
        }

        public override int GetHashCode() => HashHelper.Combine(TargetMethodName, EventName, Parameters);
    }

    internal record ServiceModel(
        string ClassName,
        string Namespace,
        string ClassPath,
        string BridgeTypeName,
        string BridgeAccessModifier,
        ImmutableArray<CallbackMapping> CallbackResultMappings,
        ImmutableArray<CallbackMapping> CallbackErrorMappings,
        string SourceFilePath,
        bool EmitPhysicalFiles,
        ImmutableArray<MethodModel> Methods,
        ImmutableArray<string> NonPartialMethodNames)
    {
        public string ClassName { get; } = ClassName;
        public string Namespace { get; } = Namespace;
        public string ClassPath { get; } = ClassPath;
        public string BridgeTypeName { get; } = BridgeTypeName;
        public string BridgeAccessModifier { get; } = BridgeAccessModifier;
        public ImmutableArray<CallbackMapping> CallbackResultMappings { get; } = CallbackResultMappings;
        public ImmutableArray<CallbackMapping> CallbackErrorMappings { get; } = CallbackErrorMappings;
        public string SourceFilePath { get; } = SourceFilePath;
        public bool EmitPhysicalFiles { get; } = EmitPhysicalFiles;
        public ImmutableArray<MethodModel> Methods { get; } = Methods;
        public ImmutableArray<string> NonPartialMethodNames { get; } = NonPartialMethodNames;

        public bool HasBridge => !string.IsNullOrEmpty(BridgeTypeName);
        public bool HasAsyncMethods => Methods.Any(m => m.IsAsync);

        public CallbackMapping GetResultMapping(string methodName)
        {
            return CallbackResultMappings.FirstOrDefault(m => m.TargetMethodName == methodName);
        }

        public CallbackMapping GetErrorMapping(string methodName)
        {
            return CallbackErrorMappings.FirstOrDefault(m => m.TargetMethodName == methodName);
        }

        public virtual bool Equals(ServiceModel other)
        {
            if (other is null) return false;
            return ClassName == other.ClassName &&
                   Namespace == other.Namespace &&
                   ClassPath == other.ClassPath &&
                   BridgeTypeName == other.BridgeTypeName &&
                   BridgeAccessModifier == other.BridgeAccessModifier &&
                   CallbackResultMappings.SequenceEqual(other.CallbackResultMappings) &&
                   CallbackErrorMappings.SequenceEqual(other.CallbackErrorMappings) &&
                   SourceFilePath == other.SourceFilePath &&
                   EmitPhysicalFiles == other.EmitPhysicalFiles &&
                   Methods.SequenceEqual(other.Methods) &&
                   NonPartialMethodNames.SequenceEqual(other.NonPartialMethodNames);
        }

        public override int GetHashCode() => HashHelper.Combine(ClassName, Namespace, ClassPath, BridgeTypeName, BridgeAccessModifier, CallbackResultMappings, CallbackErrorMappings, SourceFilePath, EmitPhysicalFiles, Methods, NonPartialMethodNames);
    }
}
