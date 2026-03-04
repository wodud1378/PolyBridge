using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PolyBridge.Core.Attributes;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Generators;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator
{
    
    [Generator(LanguageNames.CSharp)]
    public class PolyBridgeGenerator : IIncrementalGenerator
    {
        private static readonly IPlatformGenerator[] Generators =
        {
            new AndroidGenerator(),
            new IOSGenerator()
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var requiredSymbols = context.CompilationProvider.Select((compilation, _) => (
                ServiceAttr: compilation.GetTypeByMetadataName(typeof(NativeServiceAttribute).FullName!),
                MethodAttr: compilation.GetTypeByMetadataName(typeof(NativeMethodAttribute).FullName!),
                TaskType: compilation.GetTypeByMetadataName(typeof(Task).FullName!)
            ));

            var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax
                {
                    AttributeLists: { Count: > 0 }
                } cds && cds.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: (ctx, _) => ctx.Node as ClassDeclarationSyntax
            ).Where(m => m != null);

            var serviceModels = classDeclarations
                .Combine(requiredSymbols)
                .Combine(context.CompilationProvider)
                .Select((pair, _) =>
                {
                    var ((syntax, symbols), compilation) = pair;
                    return GetServiceModel(syntax, symbols.ServiceAttr, symbols.MethodAttr, symbols.TaskType,
                        compilation);
                })
                .Where(m => m != null);

            context.RegisterSourceOutput(serviceModels, GenerateSource);
        }

        private static ServiceModel GetServiceModel(
            ClassDeclarationSyntax syntax,
            INamedTypeSymbol serviceAttrSymbol,
            INamedTypeSymbol methodAttrSymbol,
            INamedTypeSymbol taskSymbol,
            Compilation compilation)
        {
            if (serviceAttrSymbol == null)
                return null;

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(syntax) is not { } classSymbol)
                return null;

            var serviceAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serviceAttrSymbol));

            if (serviceAttr == null)
                return null;

            var classPath = serviceAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
            var methods = new List<MethodModel>();
            var symbolComparer = SymbolEqualityComparer.Default;
            foreach (var methodSymbol in classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                var methodAttr = methodSymbol.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodAttrSymbol));

                if (methodAttr == null)
                    continue;

                var isAsync = symbolComparer.Equals(methodSymbol.ReturnType, taskSymbol) ||
                              (methodSymbol.ReturnType.BaseType != null &&
                               symbolComparer.Equals(methodSymbol.ReturnType.BaseType, taskSymbol));

                string innerReturnType;
                if (isAsync)
                {
                    innerReturnType = methodSymbol.ReturnType is INamedTypeSymbol { IsGenericType: true } genericType
                        ? genericType.TypeArguments[0].ToDisplayString()
                        : "void";
                }
                else
                {
                    innerReturnType = methodSymbol.ReturnType.ToDisplayString();
                }

                var parameters = methodSymbol.Parameters
                    .Select(p => new ParameterModel(p.Type.ToDisplayString(), p.Name))
                    .ToImmutableArray();

                var args = methodAttr.ConstructorArguments;
                var androidName = args.Length > 0 ? args[0].Value?.ToString() ?? methodSymbol.Name : methodSymbol.Name;
                var iosName = args.Length > 1 ? args[1].Value?.ToString() ?? methodSymbol.Name : methodSymbol.Name;
                methods.Add(new MethodModel(
                    methodSymbol.Name,
                    androidName,
                    iosName,
                    methodSymbol.ReturnType.ToDisplayString(),
                    innerReturnType,
                    isAsync,
                    parameters)
                );
            }

            return new ServiceModel(
                classSymbol.Name,
                classSymbol.ContainingNamespace.ToDisplayString(),
                classPath,
                methods.ToImmutableArray()
            );
        }

        private static void GenerateSource(SourceProductionContext context, ServiceModel model)
        {
            var bridgeInterfaceName = $"I{model.ClassName}Bridge";

            GenerateBridgeInterface(context, model, bridgeInterfaceName);
            GeneratePartialClass(context, model, bridgeInterfaceName);

            foreach (var gen in Generators)
                GeneratePlatformClass(context, model, bridgeInterfaceName, gen);
        }

        private static void GenerateBridgeInterface(
            SourceProductionContext context, ServiceModel model, string bridgeInterfaceName)
        {
            var builder = new CodeBuilder();
            builder.AddUsings(new[] { "System.Threading.Tasks" });

            using (builder.StartNameSpace(model.Namespace))
            {
                using (builder.StartInterface("internal", bridgeInterfaceName))
                {
                    foreach (var method in model.Methods)
                    {
                        builder.AppendLine($"{method.ReturnType} {method.Name}({method.ParameterDeclarations});");
                    }
                }
            }

            context.AddSource($"{bridgeInterfaceName}.g.cs",
                SourceText.From(builder.GenerateFullCode(), Encoding.UTF8));
        }

        private static void GeneratePartialClass(
            SourceProductionContext context, ServiceModel model, string bridgeInterfaceName)
        {
            var builder = new CodeBuilder();
            builder.AddUsings(new[] { "System.Threading.Tasks", "PolyBridge.Core" });

            using (builder.StartNameSpace(model.Namespace))
            {
                using (builder.StartClass("public partial", model.ClassName))
                {
                    builder.AppendField("private", true, bridgeInterfaceName, "_impl");
                    builder.AppendLine();

                    using (builder.StartConstructor("public", model.ClassName))
                    {
                        for (var i = 0; i < Generators.Length; i++)
                        {
                            var gen = Generators[i];
                            if (i == 0)
                                builder.AppendPreprocessorIf(gen.PlatformSymbol);
                            else
                                builder.AppendPreprocessorElif(gen.PlatformSymbol);
                            
                            builder.AppendLine($"_impl = new {model.ClassName}{gen.PlatformSuffix}();");
                        }

                        builder.AppendPreprocessorEndif();
                    }

                    foreach (var method in model.Methods)
                    {
                        builder.AppendLine();
                        using (builder.StartMethod("public partial", method.ReturnType, method.Name, method.IsAsync, method.ParameterDeclarations))
                        {
                            var returnStr = method.HasReturn ? "return " : "";
                            var awaitStr = method.IsAsync ? "await " : "";
                            builder.AppendLine($"{returnStr}{awaitStr}_impl.{method.Name}({method.ParameterNames});");
                        }
                    }
                }
            }

            context.AddSource($"{model.ClassName}.g.cs", SourceText.From(builder.GenerateFullCode(), Encoding.UTF8));
        }

        private static void GeneratePlatformClass(
            SourceProductionContext context, ServiceModel model, string bridgeInterfaceName, IPlatformGenerator gen)
        {
            var builder = new CodeBuilder();
            builder.AddUsings(new[] { "UnityEngine", "System.Threading.Tasks", "PolyBridge.Core" });

            var platformClassName = $"{model.ClassName}{gen.PlatformSuffix}";

            using (builder.StartNameSpace(model.Namespace))
            {
                using (builder.StartClass("internal", platformClassName, bridgeInterfaceName))
                {
                    gen.GenerateFields(builder, model.Methods);

                    builder.AppendLine();

                    using (builder.StartConstructor("internal", platformClassName))
                    {
                        gen.GenerateConstructorBody(builder, model.ClassPath);
                    }

                    foreach (var method in model.Methods)
                    {
                        builder.AppendLine();
                        using (builder.StartMethod("public", method.ReturnType, method.Name, method.IsAsync, method.ParameterDeclarations))
                        {
                            gen.GenerateMethodBody(builder, method);
                        }
                    }
                }
            }

            context.AddSource($"{platformClassName}.g.cs", SourceText.From(builder.GenerateFullCode(), Encoding.UTF8));
        }
    }
}