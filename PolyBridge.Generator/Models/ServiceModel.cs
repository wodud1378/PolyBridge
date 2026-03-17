using System.Collections.Immutable;
using System.Linq;

namespace PolyBridge.Generator.Models
{
    internal record ServiceModel(
        string ClassName,
        string Namespace,
        string ClassPath,
        string SourceFilePath,
        bool EmitPhysicalFiles,
        ImmutableArray<MethodModel> Methods,
        ImmutableArray<string> NonPartialMethodNames)
    {
        public string ClassName { get; } = ClassName;
        public string Namespace { get; } = Namespace;
        public string ClassPath { get; } = ClassPath;
        public string SourceFilePath { get; } = SourceFilePath;
        public bool EmitPhysicalFiles { get; } = EmitPhysicalFiles;
        public ImmutableArray<MethodModel> Methods { get; } = Methods;
        public ImmutableArray<string> NonPartialMethodNames { get; } = NonPartialMethodNames;

        public virtual bool Equals(ServiceModel other)
        {
            if (other is null) return false;
            return ClassName == other.ClassName &&
                   Namespace == other.Namespace &&
                   ClassPath == other.ClassPath &&
                   SourceFilePath == other.SourceFilePath &&
                   EmitPhysicalFiles == other.EmitPhysicalFiles &&
                   Methods.SequenceEqual(other.Methods) &&
                   NonPartialMethodNames.SequenceEqual(other.NonPartialMethodNames);
        }

        public override int GetHashCode() => HashHelper.Combine(ClassName, Namespace, ClassPath, SourceFilePath, EmitPhysicalFiles, Methods, NonPartialMethodNames);
    }
}
