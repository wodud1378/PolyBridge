using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PolyBridge.Generator.Builders;
using PolyBridge.Generator.Generators;
using PolyBridge.Generator.Helpers;
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

        // ========== Diagnostics ==========

        private static readonly DiagnosticDescriptor NoMethodsWarning = new(
            "PB0001", "No native methods found",
            "[NativeService] class '{0}' contains no [NativeMethod] methods",
            "PolyBridge", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor EmptyClassPathWarning = new(
            "PB0002", "Empty Android class path",
            "[NativeService] class '{0}' has no AndroidClassPath; Android bridge will not function",
            "PolyBridge", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor NotPartialMethodWarning = new(
            "PB0003", "NativeMethod must be partial",
            "[NativeMethod] method '{0}.{1}' must be declared as a partial method without a body",
            "PolyBridge", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor CancellationTokenOnSyncWarning = new(
            "PB0004", "CancellationToken on non-async method",
            "[NativeMethod] method '{0}.{1}' has a CancellationToken parameter but is not async; the token will be ignored",
            "PolyBridge", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor BridgeBaseClassConflict = new(
            "PB0005", "NativeBridge has conflicting base class",
            "[NativeBridge] class '{0}' already has a base class '{1}'; this conflicts with generated AndroidJavaProxy inheritance",
            "PolyBridge", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor MissingBridgeError = new(
            "PB0006", "Async methods require BridgeType",
            "[NativeService] class '{0}' has async methods but no BridgeType specified; define a [NativeBridge] class with [BridgeResult]/[BridgeError] and set BridgeType = typeof(...)",
            "PolyBridge", DiagnosticSeverity.Error, true);

        // ========== Initialization ==========

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax
                {
                    AttributeLists: { Count: > 0 }
                } cds && cds.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: (ctx, _) => ctx.Node as ClassDeclarationSyntax
            ).Where(m => m != null);

            var combined = classDeclarations.Combine(context.CompilationProvider);

            var serviceModels = combined
                .Select((pair, _) => GetServiceModel(pair.Left, pair.Right))
                .Where(m => m != null);
            context.RegisterSourceOutput(serviceModels, GenerateServiceSource);

            var bridgeModels = combined
                .Select((pair, _) => GetBridgeModel(pair.Left, pair.Right))
                .Where(m => m != null);
            context.RegisterSourceOutput(bridgeModels, GenerateBridgeSource);
        }

        // ========== NativeService Pipeline ==========

        private static ServiceModel GetServiceModel(ClassDeclarationSyntax syntax, Compilation compilation)
        {
            var serviceAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.NativeServiceAttribute");
            if (serviceAttrSymbol == null) return null;

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(syntax) is not { } classSymbol) return null;

            var serviceAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serviceAttrSymbol));
            if (serviceAttr == null) return null;

            var classPath = serviceAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

            // Resolve BridgeType
            string bridgeTypeName = null;
            string bridgeAccessModifier = "internal";
            var resultMappings = ImmutableArray<CallbackMapping>.Empty;
            var errorMappings = ImmutableArray<CallbackMapping>.Empty;

            var bridgeResultAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.BridgeResultAttribute");
            var bridgeErrorAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.BridgeErrorAttribute");

            foreach (var named in serviceAttr.NamedArguments)
            {
                if (named.Key != "BridgeType") continue;

                ITypeSymbol bridgeTypeSymbol = null;
                if (named.Value.Kind == TypedConstantKind.Type && named.Value.Value is ITypeSymbol bt)
                    bridgeTypeSymbol = bt;
                else if (named.Value.Value != null)
                    bridgeTypeName = named.Value.Value.ToString();

                if (bridgeTypeSymbol != null)
                {
                    var info = BridgeCallbackResolver.Resolve(bridgeTypeSymbol, bridgeResultAttrSymbol, bridgeErrorAttrSymbol);
                    if (info.HasValue)
                    {
                        bridgeTypeName = info.Value.TypeName;
                        bridgeAccessModifier = info.Value.AccessModifier;
                        resultMappings = info.Value.ResultMappings;
                        errorMappings = info.Value.ErrorMappings;
                    }
                }
            }

            // Resolve methods + mock mappings
            var methodAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.NativeMethodAttribute");
            var taskSymbol = compilation.GetTypeByMetadataName(typeof(Task).FullName!);
            var uniTaskSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask");
            var uniTaskGenericSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask`1");
            var cancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

            var allMethodsWithAttr = classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodAttrSymbol)))
                .ToImmutableArray();

            var mockMapping = ResolveMockMappings(classSymbol, compilation);

            var methods = allMethodsWithAttr
                .Select(m => GetMethodModel(m, methodAttrSymbol, taskSymbol, uniTaskSymbol, uniTaskGenericSymbol, cancellationTokenSymbol))
                .Where(m => m != null)
                .Select(m => mockMapping.TryGetValue(m.Name, out var mockName)
                    ? new MethodModel(m.Name, m.AccessModifier, m.AndroidNativeName, m.IOSNativeName, m.ReturnType, m.InnerReturnType, m.AsyncType, m.AllParameters, m.NativeParameters, m.HasCancellationToken, m.CancellationTokenParameterName, mockName)
                    : m)
                .ToImmutableArray();

            var nonPartialMethodNames = allMethodsWithAttr
                .Where(m => !m.IsPartialDefinition)
                .Select(m => m.Name)
                .ToImmutableArray();

            return new ServiceModel(
                classSymbol.Name,
                classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString(),
                classPath,
                bridgeTypeName, bridgeAccessModifier,
                resultMappings, errorMappings,
                syntax.SyntaxTree.FilePath,
                CompilationHelper.GetEmitPhysicalFiles(compilation),
                methods, nonPartialMethodNames);
        }

        private static void GenerateServiceSource(SourceProductionContext context, ServiceModel model)
        {
            // Diagnostics
            if (model.Methods.IsEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(NoMethodsWarning, Location.None, model.ClassName));
                return;
            }
            if (string.IsNullOrEmpty(model.ClassPath))
                context.ReportDiagnostic(Diagnostic.Create(EmptyClassPathWarning, Location.None, model.ClassName));
            foreach (var name in model.NonPartialMethodNames)
                context.ReportDiagnostic(Diagnostic.Create(NotPartialMethodWarning, Location.None, model.ClassName, name));
            if (model.HasAsyncMethods && !model.HasBridge)
                context.ReportDiagnostic(Diagnostic.Create(MissingBridgeError, Location.None, model.ClassName));
            foreach (var method in model.Methods)
            {
                if (method.HasCancellationToken && !method.IsAsync)
                    context.ReportDiagnostic(Diagnostic.Create(CancellationTokenOnSyncWarning, Location.None, model.ClassName, method.Name));
            }

            // Emit
            var implInterfaceName = $"I{model.ClassName}Impl";
            string outputDir = null;
            if (model.EmitPhysicalFiles && !string.IsNullOrEmpty(model.SourceFilePath))
                outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(model.SourceFilePath)!, "Generated");
            var emitter = new SourceEmitter(context, model.Namespace, outputDir);

            ServiceSourceEmitter.EmitInterface(emitter, model, implInterfaceName);
            ServiceSourceEmitter.EmitPartialClass(emitter, model, implInterfaceName, Generators);
            EditorImplGenerator.Generate(emitter, model.ClassName, implInterfaceName, model.Methods, model.HasBridge);

            foreach (var gen in Generators)
                ServiceSourceEmitter.EmitPlatformImpl(emitter, model, implInterfaceName, gen);
        }

        // ========== NativeBridge Pipeline ==========

        private static BridgeModel GetBridgeModel(ClassDeclarationSyntax syntax, Compilation compilation)
        {
            var bridgeAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.NativeBridgeAttribute");
            if (bridgeAttrSymbol == null) return null;

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(syntax) is not { } classSymbol) return null;

            var bridgeAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeAttrSymbol));
            if (bridgeAttr == null) return null;

            var androidInterfacePath = bridgeAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
            var bridgeResultAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.BridgeResultAttribute");
            var bridgeErrorAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.BridgeErrorAttribute");

            var methods = classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.IsPartialDefinition && m.ReturnsVoid)
                .Select(m =>
                {
                    var methodName = m.Name;
                    var eventName = char.ToUpperInvariant(methodName[0]) + methodName.Substring(1);

                    var role = BridgeMethodRole.Event;
                    if (bridgeResultAttrSymbol != null && m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeResultAttrSymbol)))
                        role = BridgeMethodRole.Result;
                    else if (bridgeErrorAttrSymbol != null && m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeErrorAttrSymbol)))
                        role = BridgeMethodRole.Error;

                    var parameters = m.Parameters
                        .Select(p => new ParameterModel(p.Type.ToDisplayString(FqFormat), p.Name))
                        .ToImmutableArray();

                    return new BridgeMethodModel(methodName, CompilationHelper.GetExplicitAccessModifier(m), eventName, role, parameters);
                })
                .ToImmutableArray();

            var baseClassName = classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object
                ? classSymbol.BaseType.ToDisplayString(FqFormat)
                : null;

            return new BridgeModel(
                classSymbol.Name,
                CompilationHelper.GetClassAccessModifier(syntax),
                classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString(),
                androidInterfacePath, baseClassName,
                syntax.SyntaxTree.FilePath,
                CompilationHelper.GetEmitPhysicalFiles(compilation),
                methods);
        }

        private static void GenerateBridgeSource(SourceProductionContext context, BridgeModel model)
        {
            if (model.Methods.IsEmpty) return;

            if (model.HasBaseClass)
                context.ReportDiagnostic(Diagnostic.Create(BridgeBaseClassConflict, Location.None, model.ClassName, model.BaseClassName));

            string outputDir = null;
            if (model.EmitPhysicalFiles && !string.IsNullOrEmpty(model.SourceFilePath))
                outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(model.SourceFilePath)!, "Generated");
            var emitter = new SourceEmitter(context, model.Namespace, outputDir);

            NativeBridgeGenerator.Generate(emitter, model);
        }

        // ========== Shared Helpers ==========

        private static System.Collections.Generic.Dictionary<string, string> ResolveMockMappings(INamedTypeSymbol classSymbol, Compilation compilation)
        {
            var mockImplAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.MockImplAttribute");
            var mockReturnAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.MockReturnAttribute");

            var mapping = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                foreach (var attr in member.GetAttributes())
                {
                    if ((mockImplAttrSymbol != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mockImplAttrSymbol)) ||
                        (mockReturnAttrSymbol != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mockReturnAttrSymbol)))
                    {
                        var targetName = attr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                        if (targetName != null)
                            mapping[targetName] = member.Name;
                    }
                }
            }
            return mapping;
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

            var args = methodAttr.ConstructorArguments;
            string NativeName(int i) => i < args.Length
                ? args[i].Value?.ToString() ?? methodSymbol.Name
                : args.Length > 0
                    ? args[0].Value?.ToString() ?? methodSymbol.Name
                    : methodSymbol.Name;

            return new MethodModel(
                methodSymbol.Name,
                CompilationHelper.GetExplicitAccessModifier(methodSymbol),
                NativeName(0), NativeName(1),
                returnType.ToDisplayString(FqFormat), innerReturnType, asyncType,
                allParameters, nativeParametersBuilder.ToImmutable(),
                hasCancellationToken, cancellationTokenName);
        }
    }
}
