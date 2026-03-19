using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PolyBridge.Generator.Helpers
{
    internal static class CompilationHelper
    {
        private static readonly SymbolDisplayFormat FqFormat = SymbolDisplayFormat.FullyQualifiedFormat;

        internal static bool GetEmitPhysicalFiles(Compilation compilation)
        {
            var configAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.PolyBridgeConfigurationAttribute");
            if (configAttrSymbol == null) return false;

            var configAttr = compilation.Assembly.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, configAttrSymbol));
            if (configAttr == null) return false;

            foreach (var named in configAttr.NamedArguments)
            {
                if (named.Key == "EmitPhysicalFiles")
                    return (bool)named.Value.Value!;
            }

            return false;
        }

        internal static string GetExplicitAccessModifier(IMethodSymbol methodSymbol)
        {
            var syntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
            if (syntax == null) return "";

            bool hasExplicit = syntax.Modifiers.Any(m =>
                m.IsKind(SyntaxKind.PublicKeyword) ||
                m.IsKind(SyntaxKind.PrivateKeyword) ||
                m.IsKind(SyntaxKind.InternalKeyword) ||
                m.IsKind(SyntaxKind.ProtectedKeyword));

            if (!hasExplicit) return "";

            switch (methodSymbol.DeclaredAccessibility)
            {
                case Accessibility.Public: return "public";
                case Accessibility.Internal: return "internal";
                case Accessibility.Private: return "private";
                case Accessibility.Protected: return "protected";
                case Accessibility.ProtectedOrInternal: return "protected internal";
                case Accessibility.ProtectedAndInternal: return "private protected";
                default: return "";
            }
        }

        internal static string GetClassAccessModifier(ClassDeclarationSyntax syntax)
        {
            foreach (var modifier in syntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword)) return "public";
                if (modifier.IsKind(SyntaxKind.InternalKeyword)) return "internal";
                if (modifier.IsKind(SyntaxKind.PrivateKeyword)) return "private";
            }
            return "";
        }

        internal static string GetAccessibilityString(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                _ => "internal"
            };
        }

        internal static string ToFqName(this ITypeSymbol symbol) => symbol.ToDisplayString(FqFormat);
        internal static string ToFqName(this IParameterSymbol symbol) => symbol.Type.ToDisplayString(FqFormat);
    }
}
