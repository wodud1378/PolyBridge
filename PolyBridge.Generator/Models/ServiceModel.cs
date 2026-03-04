using System.Collections.Immutable;
using System.Linq;

namespace PolyBridge.Generator.Models
{
    internal record ServiceModel(string ClassName, string Namespace, string ClassPath, ImmutableArray<MethodModel> Methods)
    {
        public string ClassName { get; } = ClassName;
        public string Namespace { get; } = Namespace;
        public string ClassPath { get; } = ClassPath;
        public ImmutableArray<MethodModel> Methods { get; } = Methods;

        public virtual bool Equals(ServiceModel other)
        {
            if (other is null) return false;
            return ClassName == other.ClassName &&
                   Namespace == other.Namespace &&
                   ClassPath == other.ClassPath &&
                   Methods.SequenceEqual(other.Methods);
        }

        public override int GetHashCode() => HashHelper.Combine(ClassName, Namespace, ClassPath, Methods);
    }
}
