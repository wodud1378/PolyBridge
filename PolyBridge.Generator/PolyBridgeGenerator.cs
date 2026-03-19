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

        private static readonly DiagnosticDescriptor BridgeBaseClassConflict = new(
            id: "PB0005",
            title: "NativeBridge has conflicting base class",
            messageFormat: "[NativeBridge] class '{0}' already has a base class '{1}'; this conflicts with generated AndroidJavaProxy inheritance",
            category: "PolyBridge",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MissingCallbackBridgeWarning = new(
            id: "PB0006",
            title: "Async methods without CallbackBridgeType",
            messageFormat: "[NativeService] class '{0}' has async methods but no CallbackBridgeType specified; async methods will not function on Android",
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

            var combined = classDeclarations.Combine(context.CompilationProvider);

            // Pipeline 1: NativeService
            var serviceModels = combined
                .Select((pair, _) => GetServiceModel(pair.Left, pair.Right))
                .Where(m => m != null);
            context.RegisterSourceOutput(serviceModels, GenerateServiceSource);

            // Pipeline 2: NativeBridge
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

            var methodAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.NativeMethodAttribute");
            var taskSymbol = compilation.GetTypeByMetadataName(typeof(Task).FullName!);
            var uniTaskSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask");
            var uniTaskGenericSymbol = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask`1");
            var cancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

            var classPath = serviceAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

            string callbackBridgeTypeName = null;
            var callbackResultMappingsBuilder = ImmutableArray.CreateBuilder<CallbackMapping>();
            var callbackErrorMappingsBuilder = ImmutableArray.CreateBuilder<CallbackMapping>();
            string eventBridgeTypeName = null;
            string eventBridgeAccessModifier = "internal";

            var bridgeResultAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.BridgeResultAttribute");
            var bridgeErrorAttrSymbol = compilation.GetTypeByMetadataName("PolyBridge.Core.Attributes.BridgeErrorAttribute");

            foreach (var named in serviceAttr.NamedArguments)
            {
                if (named.Key == "CallbackBridgeType")
                {
                    ITypeSymbol cbTypeSymbol = null;
                    if (named.Value.Kind == TypedConstantKind.Type && named.Value.Value is ITypeSymbol ct)
                    {
                        cbTypeSymbol = ct;
                        callbackBridgeTypeName = ct.ToDisplayString(FqFormat);
                    }
                    else if (named.Value.Value != null)
                    {
                        callbackBridgeTypeName = named.Value.Value.ToString();
                    }

                    if (cbTypeSymbol != null)
                    {
                        foreach (var member in cbTypeSymbol.GetMembers().OfType<IMethodSymbol>())
                        {
                            var methodName = member.Name;
                            var eventName = char.ToUpperInvariant(methodName[0]) + methodName.Substring(1);
                            var memberParams = member.Parameters
                                .Select(p => new ParameterModel(p.Type.ToDisplayString(FqFormat), p.Name))
                                .ToImmutableArray();

                            // AllowMultiple: one bridge method can have multiple BridgeResult/BridgeError attributes
                            if (bridgeResultAttrSymbol != null)
                            {
                                foreach (var resultAttr in member.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeResultAttrSymbol)))
                                {
                                    var targetMethodName = resultAttr.ConstructorArguments.Length > 0
                                        ? resultAttr.ConstructorArguments[0].Value?.ToString()
                                        : null;
                                    if (targetMethodName != null)
                                        callbackResultMappingsBuilder.Add(new CallbackMapping(targetMethodName, eventName, memberParams));
                                }
                            }

                            if (bridgeErrorAttrSymbol != null)
                            {
                                foreach (var errorAttr in member.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeErrorAttrSymbol)))
                                {
                                    var targetMethodName = errorAttr.ConstructorArguments.Length > 0
                                        ? errorAttr.ConstructorArguments[0].Value?.ToString()
                                        : null;
                                    if (targetMethodName != null)
                                        callbackErrorMappingsBuilder.Add(new CallbackMapping(targetMethodName, eventName, memberParams));
                                }
                            }
                        }
                    }
                }
                else if (named.Key == "EventBridgeType")
                {
                    if (named.Value.Kind == TypedConstantKind.Type && named.Value.Value is ITypeSymbol eventBridgeType)
                    {
                        eventBridgeTypeName = eventBridgeType.ToDisplayString(FqFormat);
                        eventBridgeAccessModifier = eventBridgeType.DeclaredAccessibility switch
                        {
                            Accessibility.Public => "public",
                            Accessibility.Internal => "internal",
                            Accessibility.Private => "private",
                            _ => "internal"
                        };
                    }
                    else if (named.Value.Value != null)
                    {
                        eventBridgeTypeName = named.Value.Value.ToString();
                    }
                }
            }

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
                    ? new MethodModel(m.Name, m.AccessModifier, m.AndroidNativeName, m.IOSNativeName, m.ReturnType, m.InnerReturnType, m.AsyncType, m.AllParameters, m.NativeParameters, m.HasCancellationToken, m.CancellationTokenParameterName, mockName)
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
                callbackBridgeTypeName,
                callbackResultMappingsBuilder.ToImmutable(),
                callbackErrorMappingsBuilder.ToImmutable(),
                eventBridgeTypeName,
                eventBridgeAccessModifier,
                syntax.SyntaxTree.FilePath,
                emitPhysicalFiles,
                methods,
                nonPartialMethodNames);
        }

        private static void GenerateServiceSource(SourceProductionContext context, ServiceModel model)
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

            // PB0006: async methods without CallbackBridgeType
            if (model.HasAsyncMethods && !model.HasCallbackBridge)
                context.ReportDiagnostic(Diagnostic.Create(MissingCallbackBridgeWarning, Location.None, model.ClassName));

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

            var implInheritance = model.HasDisposable ? "System.IDisposable" : null;
            emitter.Emit(implInterfaceName, "internal", isInterface: true, inheritance: implInheritance, body: builder =>
                {
                    foreach (var method in model.Methods)
                        builder.AppendLine($"{method.ReturnType} {method.Name}({method.ParameterDeclarations});");
                });

            var partialClassInheritance = model.HasDisposable ? "System.IDisposable" : null;
            emitter.Emit(model.ClassName, "public partial", inheritance: partialClassInheritance, body: builder =>
                {
                    builder.AppendField("private", true, implInterfaceName, "_impl");
                    if (model.HasEventBridge)
                        builder.AppendField("private", true, model.EventBridgeTypeName, "_eventBridge");
                    builder.AppendLine();

                    if (model.HasEventBridge)
                    {
                        builder.AppendLine($"{model.EventBridgeAccessModifier} {model.EventBridgeTypeName} EventBridge => _eventBridge;");
                        builder.AppendLine();
                    }

                    // Constructor
                    using (builder.StartConstructor("public", model.ClassName))
                    {
                        if (model.HasEventBridge)
                            builder.AppendLine($"_eventBridge = new {model.EventBridgeTypeName}();");

                        builder.AppendPreprocessorIf("UNITY_EDITOR");
                        builder.AppendLine($"_impl = new {model.ClassName}EditorImpl(this);");

                        for (var i = 0; i < Generators.Length; i++)
                        {
                            var gen = Generators[i];
                            var implClassName = $"{model.ClassName}{gen.PlatformSuffix}";
                            builder.AppendPreprocessorElif(gen.PlatformSymbol);
                            if (model.HasEventBridge)
                            {
                                builder.AppendLine($"var platformImpl = new {implClassName}();");
                                builder.AppendLine("platformImpl.RegisterEventBridge(_eventBridge);");
                                builder.AppendLine("_impl = platformImpl;");
                            }
                            else
                            {
                                builder.AppendLine($"_impl = new {implClassName}();");
                            }
                        }

                        builder.AppendPreprocessorEndif();
                    }

                    // NativeMethod implementations
                    foreach (var method in model.Methods)
                    {
                        builder.AppendLine();
                        using (builder.StartMethod(method.PartialModifier, method.ReturnType, method.Name, method.IsAsync, method.ParameterDeclarations))
                        {
                            var returnStr = method.HasReturn ? "return " : "";
                            var awaitStr = method.IsAsync ? "await " : "";
                            builder.AppendLine($"{returnStr}{awaitStr}_impl.{method.Name}({method.ParameterNames});");
                        }
                    }

                    // Dispose
                    if (model.HasDisposable)
                    {
                        builder.AppendLine();
                        using (builder.StartMethod("public", "void", "Dispose"))
                        {
                            builder.AppendLine("_impl?.Dispose();");
                            if (model.HasEventBridge)
                                builder.AppendLine("_eventBridge?.Dispose();");
                        }
                    }
                });

            EditorImplGenerator.Generate(emitter, model.ClassName, implInterfaceName, model.Methods, model.HasDisposable);

            foreach (var gen in Generators)
            {
                if (gen is AndroidGenerator androidGen)
                    androidGen.SetCurrentModel(model);

                var platformClassName = $"{model.ClassName}{gen.PlatformSuffix}";
                emitter.Emit(platformClassName, "internal", inheritance: implInterfaceName,
                    preprocessorGuard: gen.PlatformSymbol, body: builder =>
                    {
                        gen.GenerateFields(builder, model);
                        if (model.HasEventBridge)
                            builder.AppendField("private", false, model.EventBridgeTypeName, "_eventBridge");
                        builder.AppendLine();

                        using (builder.BeginScope($"internal {platformClassName}()"))
                        {
                            gen.GenerateConstructorBody(builder, model);
                        }

                        if (model.HasEventBridge)
                        {
                            builder.AppendLine();
                            using (builder.StartMethod("internal", "void", "RegisterEventBridge", false, $"{model.EventBridgeTypeName} eventBridge"))
                            {
                                builder.AppendLine("_eventBridge = eventBridge;");
                                gen.GenerateEventBridgeRegistration(builder, model);
                            }
                        }

                        foreach (var method in model.Methods)
                        {
                            builder.AppendLine();
                            using (builder.StartMethod("public", method.ReturnType, method.Name, method.IsAsync, method.ParameterDeclarations))
                                gen.GenerateMethodBody(builder, method);
                        }

                        if (model.HasDisposable)
                        {
                            builder.AppendLine();
                            using (builder.StartMethod("public", "void", "Dispose"))
                                gen.GenerateDisposeBody(builder, model);
                        }

                        gen.GenerateInnerClasses(builder, model);
                    });
            }
        }

        // ========== NativeEventBridge Pipeline ==========

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

                    return new BridgeMethodModel(methodName, GetExplicitAccessModifier(m), eventName, role, parameters);
                })
                .ToImmutableArray();

            // Detect class access modifier
            var classAccessModifier = "";
            foreach (var modifier in syntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword)) { classAccessModifier = "public"; break; }
                if (modifier.IsKind(SyntaxKind.InternalKeyword)) { classAccessModifier = "internal"; break; }
                if (modifier.IsKind(SyntaxKind.PrivateKeyword)) { classAccessModifier = "private"; break; }
            }

            // Detect base class (not object)
            var baseClassName = classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object
                ? classSymbol.BaseType.ToDisplayString(FqFormat)
                : null;

            return new BridgeModel(
                classSymbol.Name,
                classAccessModifier,
                classSymbol.ContainingNamespace.IsGlobalNamespace
                    ? null
                    : classSymbol.ContainingNamespace.ToDisplayString(),
                androidInterfacePath,
                baseClassName,
                syntax.SyntaxTree.FilePath,
                emitPhysicalFiles,
                methods);
        }

        private static void GenerateBridgeSource(SourceProductionContext context, BridgeModel model)
        {
            if (model.Methods.IsEmpty) return;

            // PB0005: base class conflict
            if (model.HasBaseClass)
                context.ReportDiagnostic(Diagnostic.Create(BridgeBaseClassConflict, Location.None, model.ClassName, model.BaseClassName));

            string outputDir = null;
            if (model.EmitPhysicalFiles && !string.IsNullOrEmpty(model.SourceFilePath))
                outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(model.SourceFilePath)!, "Generated");
            var emitter = new SourceEmitter(context, model.Namespace, outputDir);

            NativeBridgeGenerator.Generate(emitter, model);
        }

        // ========== Shared Helpers ==========

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

            var nativeParameters = nativeParametersBuilder.ToImmutable();

            var args = methodAttr.ConstructorArguments;
            string NativeName(int i) => i < args.Length
                ? args[i].Value?.ToString() ?? methodSymbol.Name
                : args.Length > 0
                    ? args[0].Value?.ToString() ?? methodSymbol.Name
                    : methodSymbol.Name;

            return new MethodModel(
                methodSymbol.Name,
                GetExplicitAccessModifier(methodSymbol),
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

        private static string GetExplicitAccessModifier(IMethodSymbol methodSymbol)
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
    }
}
