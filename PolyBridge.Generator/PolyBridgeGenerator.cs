using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PolyBridge.Core.Attributes;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Generators;
using PolyBridge.Generator.Models;

namespace PolyBridge.Generator
{
    // TODO :
    // 1. 플랫폼 별 비동기 처리 (스레드 안정성 확보)
    // 2. 이너 리턴 정상 동작하도록 수정
    [Generator(LanguageNames.CSharp)]
    public class PolyBridgeGenerator : IIncrementalGenerator
    {
        private static readonly IPlatformGenerator[] Generators =
        {
            new AndroidGenerator(),
            new IOSGenerator()
        };

        private static readonly SymbolDisplayFormat FqFormat = SymbolDisplayFormat.FullyQualifiedFormat;

        private static readonly DiagnosticDescriptor NoMethodsWarning = new(
            id: "PB0001",
            title: "No native methods found",
            messageFormat: "[NativeService] class '{0}' contains no [NativeMethod] methods",
            category: "PolyBridge",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EmptyClassPathWarning = new(
            id: "PB0002",
            title: "Empty Android class path",
            messageFormat: "[NativeService] class '{0}' has no AndroidClassPath; Android bridge will not function",
            category: "PolyBridge",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax
                {
                    AttributeLists: { Count: > 0 }
                } cds && cds.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: (ctx, _) => ctx.Node as ClassDeclarationSyntax
            ).Where(m => m != null);

            var serviceModels = classDeclarations
                .Combine(context.CompilationProvider)
                .Select((pair, _) =>
                {
                    var (syntax, compilation) = pair;
                    return GetServiceModel(syntax, compilation);
                })
                .Where(m => m != null);

            context.RegisterSourceOutput(serviceModels, GenerateSource);
        }

        private static ServiceModel GetServiceModel(ClassDeclarationSyntax syntax, Compilation compilation)
        {
            var serviceAttrSymbol = compilation.GetTypeByMetadataName(typeof(NativeServiceAttribute).FullName!);
            if (serviceAttrSymbol == null) return null;

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(syntax) is not { } classSymbol) return null;

            var serviceAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serviceAttrSymbol));
            if (serviceAttr == null) return null;

            var methodAttrSymbol = compilation.GetTypeByMetadataName(typeof(NativeMethodAttribute).FullName!);
            var taskSymbol = compilation.GetTypeByMetadataName(typeof(Task).FullName!);
            var uniTaskSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask");
            var uniTaskGenericSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask`1");

            var classPath = serviceAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
            var methods = classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Select(m => GetMethodModel(m, methodAttrSymbol, taskSymbol, uniTaskSymbol, uniTaskGenericSymbol))
                .Where(m => m != null)
                .ToImmutableArray();

            return new ServiceModel(
                classSymbol.Name,
                classSymbol.ContainingNamespace.ToDisplayString(),
                classPath,
                methods);
        }

        private static MethodModel GetMethodModel(
            IMethodSymbol methodSymbol,
            INamedTypeSymbol methodAttrSymbol,
            INamedTypeSymbol taskSymbol,
            INamedTypeSymbol uniTaskSymbol,
            INamedTypeSymbol uniTaskGenericSymbol)
        {
            var methodAttr = methodSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodAttrSymbol));
            if (methodAttr == null) return null;

            var returnType = methodSymbol.ReturnType;
            var comparer = SymbolEqualityComparer.Default;

            var isTask = comparer.Equals(returnType, taskSymbol) ||
                         (returnType.BaseType != null && comparer.Equals(returnType.BaseType, taskSymbol));

            var isUniTask = (uniTaskSymbol != null && comparer.Equals(returnType, uniTaskSymbol)) ||
                            (uniTaskGenericSymbol != null &&
                             returnType is INamedTypeSymbol { IsGenericType: true } uniTaskGeneric &&
                             comparer.Equals(uniTaskGeneric.OriginalDefinition, uniTaskGenericSymbol));

            IAsyncType asyncType = isUniTask ? new UniTaskType()
                                 : isTask ? new TaskType()
                                 : null;

            var innerReturnType = asyncType != null
                ? returnType is INamedTypeSymbol { IsGenericType: true } genericType
                    ? genericType.TypeArguments[0].ToDisplayString(FqFormat)
                    : "void"
                : returnType.ToDisplayString(FqFormat);

            var parameters = methodSymbol.Parameters
                .Select(p => new ParameterModel(p.Type.ToDisplayString(FqFormat), p.Name))
                .ToImmutableArray();

            var args = methodAttr.ConstructorArguments;
            string NativeName(int i) => i < args.Length ? args[i].Value?.ToString() ?? methodSymbol.Name : methodSymbol.Name;

            return new MethodModel(
                methodSymbol.Name,
                NativeName(0),
                NativeName(1),
                returnType.ToDisplayString(FqFormat),
                innerReturnType,
                asyncType,
                parameters);
        }

        private static void GenerateSource(SourceProductionContext context, ServiceModel model)
        {
            if (model.Methods.IsEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(NoMethodsWarning, Location.None, model.ClassName));
                return;
            }

            if (string.IsNullOrEmpty(model.ClassPath))
                context.ReportDiagnostic(Diagnostic.Create(EmptyClassPathWarning, Location.None, model.ClassName));

            var bridgeInterfaceName = $"I{model.ClassName}Bridge";
            var emitter = new SourceEmitter(context, model.Namespace);

            emitter.Emit(bridgeInterfaceName, "internal", isInterface: true, body: builder =>
                {
                    foreach (var method in model.Methods)
                        builder.AppendLine($"{method.ReturnType} {method.Name}({method.ParameterDeclarations});");
                });

            emitter.Emit(model.ClassName, "public partial", body: builder =>
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
                });

            foreach (var gen in Generators)
            {
                var platformClassName = $"{model.ClassName}{gen.PlatformSuffix}";
                emitter.Emit(platformClassName, "internal", inheritance: bridgeInterfaceName, body: builder =>
                    {
                        gen.GenerateFields(builder, model.Methods);
                        builder.AppendLine();

                        using (builder.StartConstructor("internal", platformClassName))
                            gen.GenerateConstructorBody(builder, model.ClassPath);

                        foreach (var method in model.Methods)
                        {
                            builder.AppendLine();
                            using (builder.StartMethod("public", method.ReturnType, method.Name, method.IsAsync, method.ParameterDeclarations))
                                gen.GenerateMethodBody(builder, method);
                        }
                    });
            }
        }
    }
}
