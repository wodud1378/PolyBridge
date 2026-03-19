using System.Collections.Immutable;
using System.Linq;

namespace PolyBridge.Generator.Models
{
    internal enum BridgeMethodRole
    {
        Event,
        Result,
        Error
    }

    internal record BridgeMethodModel(
        string Name,
        string AccessModifier,
        string EventName,
        BridgeMethodRole Role,
        ImmutableArray<ParameterModel> Parameters)
    {
        public string Name { get; } = Name;
        public string AccessModifier { get; } = AccessModifier;
        public string EventName { get; } = EventName;
        public BridgeMethodRole Role { get; } = Role;
        public ImmutableArray<ParameterModel> Parameters { get; } = Parameters;

        public string PartialModifier => string.IsNullOrEmpty(AccessModifier) ? "partial" : $"{AccessModifier} partial";

        public string ParameterDeclarations => string.Join(", ", Parameters.Select(p => $"{p.Type} {p.Name}"));
        public string ParameterNames => string.Join(", ", Parameters.Select(p => p.Name));

        public string EventDelegateType => Parameters.Length switch
        {
            0 => "System.Action",
            _ => $"System.Action<{string.Join(", ", Parameters.Select(p => p.Type))}>"
        };

        public virtual bool Equals(BridgeMethodModel other)
        {
            if (other is null) return false;
            return Name == other.Name &&
                   AccessModifier == other.AccessModifier &&
                   EventName == other.EventName &&
                   Role == other.Role &&
                   Parameters.SequenceEqual(other.Parameters);
        }

        public override int GetHashCode() => HashHelper.Combine(Name, AccessModifier, EventName, Role, Parameters);
    }

    internal record BridgeModel(
        string ClassName,
        string ClassAccessModifier,
        string Namespace,
        string AndroidInterfacePath,
        string BaseClassName,
        string SourceFilePath,
        bool EmitPhysicalFiles,
        ImmutableArray<BridgeMethodModel> Methods)
    {
        public string ClassName { get; } = ClassName;
        public string ClassAccessModifier { get; } = ClassAccessModifier;
        public string Namespace { get; } = Namespace;
        public string AndroidInterfacePath { get; } = AndroidInterfacePath;
        public string BaseClassName { get; } = BaseClassName;
        public string SourceFilePath { get; } = SourceFilePath;
        public bool EmitPhysicalFiles { get; } = EmitPhysicalFiles;
        public ImmutableArray<BridgeMethodModel> Methods { get; } = Methods;

        public bool HasBaseClass => !string.IsNullOrEmpty(BaseClassName);
        public string ClassModifier => string.IsNullOrEmpty(ClassAccessModifier) ? "partial" : $"{ClassAccessModifier} partial";

        public virtual bool Equals(BridgeModel other)
        {
            if (other is null) return false;
            return ClassName == other.ClassName &&
                   ClassAccessModifier == other.ClassAccessModifier &&
                   Namespace == other.Namespace &&
                   AndroidInterfacePath == other.AndroidInterfacePath &&
                   BaseClassName == other.BaseClassName &&
                   SourceFilePath == other.SourceFilePath &&
                   EmitPhysicalFiles == other.EmitPhysicalFiles &&
                   Methods.SequenceEqual(other.Methods);
        }

        public override int GetHashCode() => HashHelper.Combine(ClassName, ClassAccessModifier, Namespace, AndroidInterfacePath, BaseClassName, SourceFilePath, EmitPhysicalFiles, Methods);
    }
}
