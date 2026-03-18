using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        private static readonly DiagnosticDescriptor NotPartialMethodWarning = new(
            id: "PB0003",
            title: "NativeMethod must be partial",
            messageFormat: "[NativeMethod] method '{0}.{1}' must be declared as a partial method without a body",
            category: "PolyBridge",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor CancellationTokenOnSyncWarning = new(
            id: "PB0004",
            title: "CancellationToken on non-async method",
            messageFormat: "[NativeMethod] method '{0}.{1}' has a CancellationToken parameter but is not async; the token will be ignored",
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
            var serviceAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.NativeServiceAttribute");
            if (serviceAttrSymbol == null) return null;

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(syntax) is not { } classSymbol) return null;

            var serviceAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serviceAttrSymbol));
            if (serviceAttr == null) return null;

            var methodAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.NativeMethodAttribute");
            var taskSymbol = compilation.GetTypeByMetadataName(typeof(Task).FullName!);
            var uniTaskSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask");
            var uniTaskGenericSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask`1");
            var cancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

            var classPath = serviceAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

            var configAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.PolyBridgeConfigurationAttribute");
            var emitPhysicalFiles = false;
            if (configAttrSymbol != null)
            {
                var configAttr = compilation.Assembly.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, configAttrSymbol));
                if (configAttr != null)
                {
                    foreach (var named in configAttr.NamedArguments)
                    {
                        if (named.Key == "EmitPhysicalFiles")
                            emitPhysicalFiles = (bool)named.Value.Value!;
                    }
                }
            }

            var mockImplAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.MockImplAttribute");
            var mockReturnValueAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.MockReturnAttribute");

            var allMethodsWithAttr = classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodAttrSymbol)))
                .ToImmutableArray();

            // Build mock method mapping: targetMethodName → mockMethodName
            var mockMapping = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                foreach (var attr in member.GetAttributes())
                {
                    if ((mockImplAttrSymbol != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mockImplAttrSymbol)) ||
                        (mockReturnValueAttrSymbol != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mockReturnValueAttrSymbol)))
                    {
                        var targetName = attr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                        if (targetName != null)
                            mockMapping[targetName] = member.Name;
                    }
                }
            }

            var methods = allMethodsWithAttr
                .Select(m => GetMethodModel(m, methodAttrSymbol, taskSymbol, uniTaskSymbol, uniTaskGenericSymbol, cancellationTokenSymbol))
                .Where(m => m != null)
                .Select(m => mockMapping.TryGetValue(m.Name, out var mockName)
                    ? new MethodModel(m.Name, m.AndroidNativeName, m.IOSNativeName, m.ReturnType, m.InnerReturnType, m.AsyncType, m.AllParameters, m.NativeParameters, m.HasCancellationToken, m.CancellationTokenParameterName, mockName)
                    : m)
                .ToImmutableArray();

            var nonPartialMethodNames = allMethodsWithAttr
                .Where(m => !m.IsPartialDefinition)
                .Select(m => m.Name)
                .ToImmutableArray();

            return new ServiceModel(
                classSymbol.Name,
                classSymbol.ContainingNamespace.IsGlobalNamespace
                    ? null
                    : classSymbol.ContainingNamespace.ToDisplayString(),
                classPath,
                syntax.SyntaxTree.FilePath,
                emitPhysicalFiles,
                methods,
                nonPartialMethodNames);
        }

        private static MethodModel GetMethodModel(
            IMethodSymbol methodSymbol,
            INamedTypeSymbol methodAttrSymbol,
            INamedTypeSymbol taskSymbol,
            INamedTypeSymbol uniTaskSymbol,
            INamedTypeSymbol uniTaskGenericSymbol,
            INamedTypeSymbol cancellationTokenSymbol)
        {
            var methodAttr = methodSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodAttrSymbol));
            if (methodAttr == null) return null;
            if (!methodSymbol.IsPartialDefinition) return null;

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

            var allParameters = methodSymbol.Parameters
                .Select(p => new ParameterModel(p.Type.ToDisplayString(FqFormat), p.Name))
                .ToImmutableArray();

            // Separate CancellationToken from native parameters
            var hasCancellationToken = false;
            string cancellationTokenName = null;
            var nativeParametersBuilder = ImmutableArray.CreateBuilder<ParameterModel>();

            foreach (var p in methodSymbol.Parameters)
            {
                if (cancellationTokenSymbol != null && comparer.Equals(p.Type, cancellationTokenSymbol))
                {
                    hasCancellationToken = true;
                    cancellationTokenName = p.Name;
                }
                else
                {
                    nativeParametersBuilder.Add(new ParameterModel(p.Type.ToDisplayString(FqFormat), p.Name));
                }
            }

            var nativeParameters = nativeParametersBuilder.ToImmutable();

            var args = methodAttr.ConstructorArguments;
            string NativeName(int i) => i < args.Length
                ? args[i].Value?.ToString() ?? methodSymbol.Name
                : args.Length > 0
                    ? args[0].Value?.ToString() ?? methodSymbol.Name
                    : methodSymbol.Name;

            return new MethodModel(
                methodSymbol.Name,
                NativeName(0),
                NativeName(1),
                returnType.ToDisplayString(FqFormat),
                innerReturnType,
                asyncType,
                allParameters,
                nativeParameters,
                hasCancellationToken,
                cancellationTokenName);
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

            foreach (var name in model.NonPartialMethodNames)
                context.ReportDiagnostic(Diagnostic.Create(NotPartialMethodWarning, Location.None, model.ClassName, name));

            // PB0004: CancellationToken on non-async methods
            foreach (var method in model.Methods)
            {
                if (method.HasCancellationToken && !method.IsAsync)
                    context.ReportDiagnostic(Diagnostic.Create(CancellationTokenOnSyncWarning, Location.None, model.ClassName, method.Name));
            }

            var implInterfaceName = $"I{model.ClassName}Impl";
            string outputDir = null;
            if (model.EmitPhysicalFiles && !string.IsNullOrEmpty(model.SourceFilePath))
                outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(model.SourceFilePath)!, "Generated");
            var emitter = new SourceEmitter(context, model.Namespace, outputDir);

            emitter.Emit(implInterfaceName, "internal", isInterface: true, body: builder =>
                {
                    foreach (var method in model.Methods)
                        builder.AppendLine($"{method.ReturnType} {method.Name}({method.ParameterDeclarations});");
                });

            emitter.Emit(model.ClassName, "public partial", body: builder =>
                {
                    builder.AppendField("private", true, implInterfaceName, "_impl");
                    builder.AppendLine();

                    using (builder.StartConstructor("public", model.ClassName))
                    {
                        builder.AppendPreprocessorIf("UNITY_EDITOR");
                        builder.AppendLine($"_impl = new {model.ClassName}EditorImpl(this);");

                        for (var i = 0; i < Generators.Length; i++)
                        {
                            var gen = Generators[i];
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

            EditorImplGenerator.Generate(emitter, model.ClassName, implInterfaceName, model.Methods);

            foreach (var gen in Generators)
            {
                var platformClassName = $"{model.ClassName}{gen.PlatformSuffix}";
                emitter.Emit(platformClassName, "internal", inheritance: implInterfaceName,
                    preprocessorGuard: gen.PlatformSymbol, body: builder =>
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
