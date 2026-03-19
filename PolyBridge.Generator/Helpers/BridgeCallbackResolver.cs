using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator.Helpers
{
    internal static class BridgeCallbackResolver
    {
        internal struct BridgeInfo
        {
            public string TypeName;
            public string AccessModifier;
            public ImmutableArray<CallbackMapping> ResultMappings;
            public ImmutableArray<CallbackMapping> ErrorMappings;
        }

        internal static BridgeInfo? Resolve(
            ITypeSymbol bridgeTypeSymbol,
            INamedTypeSymbol bridgeResultAttrSymbol,
            INamedTypeSymbol bridgeErrorAttrSymbol)
        {
            if (bridgeTypeSymbol == null) return null;

            var resultMappings = ImmutableArray.CreateBuilder<CallbackMapping>();
            var errorMappings = ImmutableArray.CreateBuilder<CallbackMapping>();

            foreach (var member in bridgeTypeSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                var methodName = member.Name;
                var eventName = char.ToUpperInvariant(methodName[0]) + methodName.Substring(1);
                var memberParams = member.Parameters
                    .Select(p => new ParameterModel(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), p.Name))
                    .ToImmutableArray();

                if (bridgeResultAttrSymbol != null)
                {
                    foreach (var attr in member.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeResultAttrSymbol)))
                    {
                        var targetMethodName = attr.ConstructorArguments.Length > 0
                            ? attr.ConstructorArguments[0].Value?.ToString()
                            : null;
                        if (targetMethodName != null)
                            resultMappings.Add(new CallbackMapping(targetMethodName, eventName, memberParams));
                    }
                }

                if (bridgeErrorAttrSymbol != null)
                {
                    foreach (var attr in member.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeErrorAttrSymbol)))
                    {
                        var targetMethodName = attr.ConstructorArguments.Length > 0
                            ? attr.ConstructorArguments[0].Value?.ToString()
                            : null;
                        if (targetMethodName != null)
                            errorMappings.Add(new CallbackMapping(targetMethodName, eventName, memberParams));
                    }
                }
            }

            return new BridgeInfo
            {
                TypeName = bridgeTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                AccessModifier = CompilationHelper.GetAccessibilityString(bridgeTypeSymbol.DeclaredAccessibility),
                ResultMappings = resultMappings.ToImmutable(),
                ErrorMappings = errorMappings.ToImmutable()
            };
        }
    }
}
